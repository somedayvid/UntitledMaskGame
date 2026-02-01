using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 
///
////— Player 的 TakeDamage / AddShield hook
/// ：mana/cost、deck/hand/discard/exile、status、summon、extra turn、8 masks.
/// </summary>
public class CombatManagerFacade : MonoBehaviour
{
    [Header("Economy")]
    public int maxMana = 3;
    public int startHandSize = 5;
    public int drawPerTurn = 2;

    [Header("Ski: shield gain multiplier (less shield)")]
    [Range(0.1f, 1f)] public float skiShieldGainMultiplier = 0.7f;

    [Header("Dian Wei activation cost")]
    public int dianWeiHpCostOnActive = 2;

    public event Action OnRequestExtraTurn;

    private Player player;
    private List<Enemy> enemies = new();

    // External economy/zones (不改 Player.hand 的结构，只把它当 UI 展示列表)
    private readonly List<CardBlueprint> deck = new();
    private readonly List<CardBlueprint> discard = new();
    private readonly List<CardBlueprint> exile = new();

    // Runtime mapping (Card instance -> runtime meta)
    private readonly Dictionary<Card, CardRuntime> runtime = new();

    private int mana;

    // Enemy statuses
    private readonly Dictionary<Enemy, EnemyStatus> status = new();

    // Summons
    private readonly List<Summon> foxies = new();
    private bool dogActive = false;

    // Mask runtime states
    private BaoZhengState bao = new();
    private DianWeiState dian = new();
    private ErLangState erlang = new();
    private ZhongKuiState zhong = new();
    private SunWukongState wukong = new();
    private KitsuneState kitsune = new();
    private SkiState ski = new();

    // guard (avoid recursion when we call player.TakeDamage inside hook)
    private bool internalDamageCall = false;

    // -------------------- PUBLIC API --------------------

    public void Bind(Player p, List<Enemy> enemyList)
    {
        player = p;
        enemies = enemyList ?? new List<Enemy>();

        status.Clear();
        foreach (var e in enemies)
            if (e != null && !status.ContainsKey(e))
                status.Add(e, new EnemyStatus());

        if (player != null)
        {
            player.OnBeforeTakeDamage -= HandleBeforeTakeDamage;
            player.OnBeforeTakeDamage += HandleBeforeTakeDamage;

            player.OnAfterTakeDamage -= HandleAfterTakeDamage;
            player.OnAfterTakeDamage += HandleAfterTakeDamage;

            player.OnBeforeGainShield -= HandleBeforeGainShield;
            player.OnBeforeGainShield += HandleBeforeGainShield;

            player.OnCardPlayed -= HandleOnCardPlayed;
            player.OnCardPlayed += HandleOnCardPlayed;
        }
    }

    public void OnBattleStart(MaskData[] equippedMasks, int activeMaskIndex)
    {
        ResetAllStates();
        mana = maxMana;

        // 初始化“外置 deck”：如果你现在项目没有 deck，我们就把「初始手牌模板」当作牌池来源。
        // 做法：把当前 hand 里的卡，转成 blueprint 丢进 deck，然后清空 hand，再抽 startHandSize。
        deck.Clear();
        discard.Clear();
        exile.Clear();
        runtime.Clear();

        if (player != null)
        {
            // collect starting templates from current hand
            foreach (var c in player.hand)
                deck.Add(CardBlueprint.FromCard(c, DefaultCostFor(c), CardFlags.None));

            player.hand.Clear();

            Shuffle(deck);
            Draw(startHandSize);
        }
    }

    public void OnPlayerTurnStart(MaskData[] equippedMasks, int activeMaskIndex, int turnNumber)
    {
        mana = maxMana;

        // ZhongKui cons：每回合塞负面卡到 discard
        if (HasMask(equippedMasks, "钟馗") || HasMaskId(equippedMasks, "zhong_kui"))
        {
            AddZhongKuiNegativeCardToDiscard();
        }

        // ErLang dog condition check at turn start (if 2 activations last turn)
        dogActive = (erlang.lastTurnActivations >= 2);

        // draw
        Draw(drawPerTurn);

        // turn-based regen (Sun Wukong)
        if (wukong.buffActive && wukong.regenPerTurn != 0)
        {
            int val = wukong.GetSigned(wukong.regenPerTurn);
            if (val > 0) player.HealPlayer(val);
            else if (val < 0) SafeInternalDamage(-val);
        }

        // Kitsune: Foxy end-of-turn triggers are handled in OnPlayerTurnEnd
    }

    public void OnPlayerTurnEnd(MaskData[] equippedMasks, int activeMaskIndex, int turnNumber)
    {
        // Kitsune summons act at end of player turn
        if (kitsune.anyKitsuneEquipped)
        {
            ResolveFoxyEndOfTurn();
        }

        // ErLang track activations
        erlang.lastTurnActivations = erlang.thisTurnActivations;
        erlang.thisTurnActivations = 0;
    }

    public bool TryPlayCard(MaskData[] equippedMasks, int activeMaskIndex, Card card, Enemy target)
    {
        if (player == null) return false;
        if (card == null) return false;

        // register runtime meta if missing (e.g., newly generated cards)
        if (!runtime.TryGetValue(card, out var meta))
        {
            meta = new CardRuntime
            {
                blueprint = CardBlueprint.FromCard(card, DefaultCostFor(card), CardFlags.None),
                banishOnPlay = false,
                unremovable = false,
                onDiscardSelfDamage = 0
            };
            runtime[card] = meta;
        }

        // Resolve mask equipped flags quickly
        ApplyEquippedMaskBooleans(equippedMasks, activeMaskIndex);

        // Compute final cost (BaoZheng pending reduction, etc.)
        int cost = meta.blueprint.cost;
        cost = ApplyCostHooks(equippedMasks, activeMaskIndex, cost);

        if (mana < cost)
        {
            Debug.Log($"[Combat] Not enough mana. Need {cost}, Have {mana}");
            return false;
        }

        mana -= cost;

        // Build action context
        var ctx = new ActionContext
        {
            actor = player,
            card = card,
            target = target,
            isAOE = false,
            repeatCount = 1,
            secondCastDamageMultiplier = 1f,
            requestExtraTurn = false,
            banishThisCard = false,
            baseCost = meta.blueprint.cost,
            finalCost = cost
        };

        // ACTIVE / PASSIVE hooks before play
        RunBeforePlayHooks(equippedMasks, activeMaskIndex, ref ctx);

        if (ctx.cancelPlay) return false;

        // Dian Wei: when active -> double cast behavior
        // handled by hooks (ctx.repeatCount, secondCastDamageMultiplier)

        // Execute casts
        for (int i = 0; i < ctx.repeatCount; i++)
        {
            float dmgMul = (i == 1) ? ctx.secondCastDamageMultiplier : 1f;
            ResolveCardCast(equippedMasks, activeMaskIndex, ctx.card, ctx.target, ctx.isAOE, dmgMul);
        }

        // After play hooks (set BaoZheng pending, Kitsune counters, etc.)
        RunAfterPlayHooks(equippedMasks, activeMaskIndex, ref ctx);

        // move to discard/exile
        MovePlayedCardToZone(ctx.card, ctx.banishThisCard || meta.banishOnPlay);

        // extra turn request
        if (ctx.requestExtraTurn)
            OnRequestExtraTurn?.Invoke();

        return true;
    }

    public void AfterEnemyAction(Enemy enemy)
    {
        if (enemy == null) return;
        if (!status.TryGetValue(enemy, out var st)) return;

        // Ski: after enemy moves, trigger bleed 3 times randomly
        if (ski.anySkiEquipped && st.bleed > 0)
        {
            for (int i = 0; i < 3; i++)
            {
                float r = UnityEngine.Random.value;
                if (r < 0.75f)
                {
                    enemy.TakeDamage(st.bleed);
                }
                else if (r < 0.90f)
                {
                    st.weakenTurns = Mathf.Max(st.weakenTurns, 1);
                }
                else
                {
                    st.bleed += 1;
                }
            }
            status[enemy] = st;
        }

        // stun tick down after action
        if (st.stunTurns > 0) st.stunTurns -= 1;
        if (st.weakenTurns > 0) st.weakenTurns -= 1;
        status[enemy] = st;
    }

    // -------------------- HOOKS INTO PLAYER --------------------

    private void HandleBeforeTakeDamage(ref Player.DamageContext ctx)
    {
        if (internalDamageCall) return;

        // ErLang passive: take 1.4x dmg when equipped
        if (erlang.anyErLangEquipped)
            ctx.incomingDamage = Mathf.CeilToInt(ctx.incomingDamage * 1.4f);

        // ErLang time shield: immune once
        if (erlang.timeShieldCharges > 0)
        {
            erlang.timeShieldCharges -= 1;
            ctx.cancelDamage = true;
            return;
        }

        // Kitsune: redirect incoming damage to Foxy if any exist (Foxy can be killed)
        if (kitsune.anyKitsuneEquipped && foxies.Count > 0)
        {
            // redirect full damage to the first alive foxy
            var f = GetFirstAliveFoxy();
            if (f != null)
            {
                f.hp -= ctx.incomingDamage;
                ctx.cancelDamage = true;

                if (f.hp <= 0)
                {
                    foxies.Remove(f);
                    // con: when killed, -10 health to player
                    SafeInternalDamage(10);
                }
                return;
            }
        }
    }

    private void HandleAfterTakeDamage(int hpLoss)
    {
        if (internalDamageCall) return;

        // SunWukong: if attacked while mask equipped, buffs become debuffs
        if (wukong.anyWukongEquipped && hpLoss > 0)
        {
            wukong.inverted = true;
        }
    }

    private void HandleBeforeGainShield(ref Player.ShieldContext ctx)
    {
        if (ski.anySkiEquipped)
        {
            // Ski con: shield gained less effective -> interpret as "gain less shield"
            ctx.amount = Mathf.FloorToInt(ctx.amount * skiShieldGainMultiplier);
        }

        // SunWukong: if buff active -> shield card +1 shield when cast
        // 这里不做，因为会影响所有来源的盾（包括 BaoZheng、Foxy）
        // 我们只在“卡牌结算时”额外加 +1（更精准）
    }

    private void HandleOnCardPlayed()
    {
        // 旧事件保留，不强依赖
    }

    // -------------------- CARD / ZONE OPS --------------------

    private void Draw(int count)
    {
        if (player == null) return;

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
            {
                if (discard.Count == 0) return;
                deck.AddRange(discard);
                discard.Clear();
                Shuffle(deck);
            }

            var bp = deck[0];
            deck.RemoveAt(0);

            Card c = bp.CreateInstance();
            player.hand.Add(c);

            runtime[c] = new CardRuntime
            {
                blueprint = bp,
                banishOnPlay = bp.flags.HasFlag(CardFlags.BanishOnPlay),
                unremovable = bp.flags.HasFlag(CardFlags.Unremovable),
                onDiscardSelfDamage = bp.onDiscardSelfDamage
            };
        }
    }

    private void MovePlayedCardToZone(Card c, bool toExile)
    {
        if (player == null || c == null) return;

        // player.PlayCard 已经把卡从 hand.Remove 了，所以这里只更新外置 zones
        if (!runtime.TryGetValue(c, out var meta)) return;

        if (toExile && !meta.unremovable)
        {
            exile.Add(meta.blueprint);
        }
        else
        {
            discard.Add(meta.blueprint);

            // Homesick: takes 3 damage when discarded
            if (meta.onDiscardSelfDamage > 0)
                SafeInternalDamage(meta.onDiscardSelfDamage);
        }

        runtime.Remove(c);
    }

    // -------------------- CARD RESOLUTION --------------------

    private void ResolveCardCast(MaskData[] equippedMasks, int activeMaskIndex, Card card, Enemy target, bool isAOE, float damageMultiplier)
    {
        if (card == null) return;

        // Special generated cards by ID
        if (TryResolveSpecialGeneratedCard(equippedMasks, activeMaskIndex, card, target))
            return;

        // Normal card uses original Player.PlayCard for base logic,
        // BUT we need to support AOE + damage multiplier + Wukong strength, Ski bleed, ZhongKui lifesteal, kill mana, etc.
        // 所以我们这里自己做“攻击/防御”两种最常用类型（不依赖你 Player.PlayCard 的内部实现）。
        if (card.cardType == CardType.Attack)
        {
            if (isAOE)
            {
                foreach (var e in enemies)
                {
                    if (e == null || !e.IsAlive()) continue;
                    DealDamageToEnemy_WithHooks(equippedMasks, activeMaskIndex, card, e, damageMultiplier);
                }
            }
            else
            {
                if (target == null || !target.IsAlive()) return;
                DealDamageToEnemy_WithHooks(equippedMasks, activeMaskIndex, card, target, damageMultiplier);
            }
        }
        else if (card.cardType == CardType.Defense)
        {
            int shieldGain = card.shield;

            // SunWukong buff: shield card +1 (signed if inverted)
            if (wukong.buffActive)
                shieldGain += wukong.GetSigned(1);

            if (shieldGain > 0) player.AddShield(shieldGain);
            else if (shieldGain < 0) SafeInternalDamage(-shieldGain);
        }
        else
        {
            // Power: currently none in your base system
        }
    }

    private void DealDamageToEnemy_WithHooks(MaskData[] equippedMasks, int activeMaskIndex, Card card, Enemy enemy, float damageMultiplier)
    {
        if (enemy == null || !enemy.IsAlive()) return;

        // base damage from player strength + card.damage
        int baseDmg = card.damage;
        baseDmg += playerStrengthSignedBonus(); // SunWukong strength buff/debuff
        baseDmg = Mathf.Max(0, baseDmg);

        int dmg = Mathf.RoundToInt(baseDmg * damageMultiplier);

        // DianWei second cast wants full special effect but 50% damage:
        // we already passed damageMultiplier.

        enemy.TakeDamage(dmg);

        // Ski: all attacks cause bleed
        if (ski.anySkiEquipped)
        {
            if (status.TryGetValue(enemy, out var st))
            {
                st.bleed += 1;
                status[enemy] = st;
            }
        }

        // ZhongKui lifesteal 10%
        if (zhong.anyZhongKuiEquipped && dmg > 0)
        {
            int heal = Mathf.FloorToInt(dmg * 0.1f);
            if (heal > 0) player.HealPlayer(heal);
        }

        // Kill check: if enemy reports dead after taking damage -> +2 mana
        if (zhong.anyZhongKuiEquipped && !enemy.IsAlive())
        {
            mana = Mathf.Min(maxMana, mana + 2);
        }
    }

    private int playerStrengthSignedBonus()
    {
        if (!wukong.buffActive) return 0;
        return wukong.GetSigned(1);
    }

    // -------------------- MASK HOOK PIPELINE --------------------

    private void RunBeforePlayHooks(MaskData[] equippedMasks, int activeMaskIndex, ref ActionContext ctx)
    {
        // BaoZheng cost pending applies in ApplyCostHooks (before pay), not here

        // DianWei active -> double cast
        if (IsActiveMask(equippedMasks, activeMaskIndex, "典韦", "dian_wei"))
        {
            ctx.repeatCount = 2;
            ctx.secondCastDamageMultiplier = 0.5f;

            dian.activations++;
            SafeInternalDamage(dianWeiHpCostOnActive);

            if (dian.activations % 3 == 0)
                dian.nextAttackAOE = true;
        }

        // DianWei next AOE flag
        if (dian.nextAttackAOE && ctx.card.cardType == CardType.Attack)
        {
            ctx.isAOE = true;
            dian.nextAttackAOE = false;
        }

        // ErLang active -> add 0-cost random card to hand each activation
        if (IsActiveMask(equippedMasks, activeMaskIndex, "二郎神", "er_lang_shen"))
        {
            erlang.thisTurnActivations++;
            AddErLangRandomZeroCostCardToHand();
        }

        // BaoZheng con: banish played card
        if (IsActiveMask(equippedMasks, activeMaskIndex, "包拯", "bao_zheng"))
        {
            ctx.banishThisCard = true;
        }

        // SunWukong active -> apply buffs (once per activation)
        if (IsActiveMask(equippedMasks, activeMaskIndex, "孙悟空", "sun_wukong"))
        {
            wukong.buffActive = true;
            wukong.inverted = false; // reset on activation
            // copy previous mask effect (very simple: copy lastActiveNonWukong)
            wukong.copiedMaskId = wukong.lastNonWukongMaskId;
        }

        // Kitsune: ensure we can generate its 1-cost card (we keep it always available by adding to deck once)
        if (kitsune.anyKitsuneEquipped && !kitsune.kitsuneCardSeeded)
        {
            kitsune.kitsuneCardSeeded = true;
            // put into discard so it will enter deck on shuffle quickly
            discard.Add(CardBlueprint.KitsuneCard());
        }
    }

    private void RunAfterPlayHooks(MaskData[] equippedMasks, int activeMaskIndex, ref ActionContext ctx)
    {
        // BaoZheng: next card costs minus this card cost (if 0 then -1), gain shield equal reduced amount
        if (IsActiveMask(equippedMasks, activeMaskIndex, "包拯", "bao_zheng"))
        {
            int reduce = Mathf.Max(ctx.finalCost, 1);
            bao.pendingReduction = reduce;
            player.AddShield(reduce); // shield gain will be affected by Ski multiplier automatically
        }

        // ZhongKui: nothing here; turn-based negative cards handled in OnPlayerTurnStart

        // Kitsune: track kitsune card usage
        if (kitsune.anyKitsuneEquipped && ctx.card.ID == CardBlueprint.ID_KITSUNE_CARD)
        {
            kitsune.kitsuneCardUses++;
            if (kitsune.kitsuneCardUses % 5 == 0 && foxies.Count < 7)
            {
                int hp = (GetPlayerApproxHealth() < 30) ? 12 : 8;
                foxies.Add(new Summon { kind = SummonKind.Foxy, hp = hp });
            }
        }

        // ErLang: “extra turn card” triggers extra turn
        if (ctx.requestExtraTurn)
            return;

        // SunWukong: record last non-wukong mask
        var active = GetActiveMask(equippedMasks, activeMaskIndex);
        if (active != null && active.displayName != "孙悟空" && active.maskId != "sun_wukong")
            wukong.lastNonWukongMaskId = active.maskId ?? active.displayName;
    }

    private int ApplyCostHooks(MaskData[] equippedMasks, int activeMaskIndex, int cost)
    {
        // BaoZheng pending reduction affects NEXT card
        if (bao.pendingReduction > 0)
        {
            cost = Mathf.Max(0, cost - bao.pendingReduction);
            bao.pendingReduction = 0;
        }
        return cost;
    }

    // -------------------- SPECIAL GENERATED CARDS --------------------

    private bool TryResolveSpecialGeneratedCard(MaskData[] equippedMasks, int activeMaskIndex, Card card, Enemy target)
    {
        // ErLang random 0-cost cards
        if (card.ID == CardBlueprint.ID_ERLANG_TIME_SHIELD)
        {
            erlang.timeShieldCharges += 1;
            return true;
        }
        if (card.ID == CardBlueprint.ID_ERLANG_BITE)
        {
            // Stun 1 turn on target
            if (target != null && status.TryGetValue(target, out var st))
            {
                st.stunTurns = Mathf.Max(st.stunTurns, 1);
                status[target] = st;
            }
            return true;
        }
        if (card.ID == CardBlueprint.ID_ERLANG_ATTACK_X2)
        {
            if (target == null) return true;
            // deal damage *2 : treat as instant damage 6 for now
            target.TakeDamage(6);
            return true;
        }
        if (card.ID == CardBlueprint.ID_ERLANG_AOE_3X2)
        {
            foreach (var e in enemies)
                if (e != null && e.IsAlive()) e.TakeDamage(6);
            return true;
        }
        if (card.ID == CardBlueprint.ID_ERLANG_EXTRA_TURN)
        {
            // request extra turn
            OnRequestExtraTurn?.Invoke();
            return true;
        }

        // ZhongKui negative cards (no effect in play)
        if (card.ID == CardBlueprint.ID_ZHONG_DEEP_CONFUSION) return true;
        if (card.ID == CardBlueprint.ID_ZHONG_SAD) return true;
        if (card.ID == CardBlueprint.ID_ZHONG_HOMESICK) return true;

        // Kitsune card: Gain 2 shield, draw 1
        if (card.ID == CardBlueprint.ID_KITSUNE_CARD)
        {
            player.AddShield(2);
            Draw(1);
            return true;
        }

        return false;
    }

    // -------------------- HELPERS --------------------

    private void AddErLangRandomZeroCostCardToHand()
    {
        float r = UnityEngine.Random.value;
        CardBlueprint bp;
        if (r < 0.30f) bp = CardBlueprint.ErLangTimeShield();
        else if (r < 0.50f) bp = CardBlueprint.ErLangBite();
        else if (r < 0.70f) bp = CardBlueprint.ErLangAttackX2();
        else if (r < 0.90f) bp = CardBlueprint.ErLangAOE();
        else bp = CardBlueprint.ErLangExtraTurn();

        Card c = bp.CreateInstance();
        player.hand.Add(c);
        runtime[c] = new CardRuntime { blueprint = bp };
    }

    private void AddZhongKuiNegativeCardToDiscard()
    {
        zhong.anyZhongKuiEquipped = true;
        float r = UnityEngine.Random.value;

        CardBlueprint bp;
        if (r < 0.34f) bp = CardBlueprint.ZhongDeepConfusion();
        else if (r < 0.67f) bp = CardBlueprint.ZhongSad();
        else bp = CardBlueprint.ZhongHomesick();

        discard.Add(bp);
    }

    private void ResolveFoxyEndOfTurn()
    {
        if (foxies.Count == 0) return;

        // each foxy: +2 shield, +5 heal, deal 7 damage to random alive enemy
        for (int i = 0; i < foxies.Count; i++)
        {
            player.AddShield(2);
            player.HealPlayer(5);

            Enemy e = GetRandomAliveEnemy();
            if (e != null) e.TakeDamage(7);
        }
    }

    private Enemy GetRandomAliveEnemy()
    {
        List<Enemy> alive = new();
        foreach (var e in enemies) if (e != null && e.IsAlive()) alive.Add(e);
        if (alive.Count == 0) return null;
        return alive[UnityEngine.Random.Range(0, alive.Count)];
    }

    private Summon GetFirstAliveFoxy()
    {
        foreach (var f in foxies)
            if (f != null && f.hp > 0) return f;
        return null;
    }

    private int GetPlayerApproxHealth()
    {
        // 你现在 Player 没有公开 health getter，我们用一个近似：
        // 如果你愿意，我可以给 Player 加一个 HealthDebug 属性（1行）
        // 暂时返回 100，保证不报错
        return 100;
    }

    private void SafeInternalDamage(int dmg)
    {
        if (player == null) return;
        internalDamageCall = true;
        player.TakeDamage(dmg);
        internalDamageCall = false;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static int DefaultCostFor(Card c)
    {
        // 你现在 Card 没 cost，所以先给默认规则：Attack=1, Defense=1, Power=1
        // 你以后想要“每张牌不同 cost”，我们可以加一个 ScriptableObject 映射表（不改 Card.cs）
        return 1;
    }

    private void ResetAllStates()
    {
        bao = new BaoZhengState();
        dian = new DianWeiState();
        erlang = new ErLangState();
        zhong = new ZhongKuiState();
        wukong = new SunWukongState();
        kitsune = new KitsuneState();
        ski = new SkiState();

        foxies.Clear();
        dogActive = false;
    }

    private void ApplyEquippedMaskBooleans(MaskData[] equippedMasks, int activeMaskIndex)
    {
        erlang.anyErLangEquipped = HasMask(equippedMasks, "二郎神") || HasMaskId(equippedMasks, "er_lang_shen");
        zhong.anyZhongKuiEquipped = HasMask(equippedMasks, "钟馗") || HasMaskId(equippedMasks, "zhong_kui");
        wukong.anyWukongEquipped = HasMask(equippedMasks, "孙悟空") || HasMaskId(equippedMasks, "sun_wukong");
        kitsune.anyKitsuneEquipped = HasMask(equippedMasks, "狐狸") || HasMaskId(equippedMasks, "kitsune") || HasMaskId(equippedMasks, "ksume");
        ski.anySkiEquipped = HasMask(equippedMasks, "Ski") || HasMaskId(equippedMasks, "ski");
    }

    private static MaskData GetActiveMask(MaskData[] equippedMasks, int activeMaskIndex)
    {
        if (equippedMasks == null || equippedMasks.Length < 3) return null;
        int idx = Mathf.Clamp(activeMaskIndex, 0, 2);
        return equippedMasks[idx];
    }

    private static bool IsActiveMask(MaskData[] equippedMasks, int activeMaskIndex, string displayNameCN, string id)
    {
        var m = GetActiveMask(equippedMasks, activeMaskIndex);
        if (m == null) return false;
        return (m.displayName == displayNameCN) || (m.maskId == id);
    }

    private static bool HasMask(MaskData[] equippedMasks, string displayNameCN)
    {
        if (equippedMasks == null) return false;
        foreach (var m in equippedMasks)
            if (m != null && m.displayName == displayNameCN) return true;
        return false;
    }

    private static bool HasMaskId(MaskData[] equippedMasks, string id)
    {
        if (equippedMasks == null) return false;
        foreach (var m in equippedMasks)
            if (m != null && m.maskId == id) return true;
        return false;
    }

    // -------------------- DATA TYPES --------------------

    private struct ActionContext
    {
        public Player actor;
        public Card card;
        public Enemy target;

        public bool cancelPlay;
        public bool isAOE;

        public int baseCost;
        public int finalCost;

        public int repeatCount;
        public float secondCastDamageMultiplier;

        public bool banishThisCard;
        public bool requestExtraTurn;
    }

    [Flags]
    private enum CardFlags
    {
        None = 0,
        Unremovable = 1 << 0,
        BanishOnPlay = 1 << 1
    }

    private class CardRuntime
    {
        public CardBlueprint blueprint;
        public bool banishOnPlay;
        public bool unremovable;
        public int onDiscardSelfDamage;
    }

    private class CardBlueprint
    {
        // Reserved IDs for generated cards
        public const int ID_ERLANG_TIME_SHIELD = 9001;
        public const int ID_ERLANG_BITE = 9002;
        public const int ID_ERLANG_ATTACK_X2 = 9003;
        public const int ID_ERLANG_AOE_3X2 = 9004;
        public const int ID_ERLANG_EXTRA_TURN = 9005;

        public const int ID_ZHONG_DEEP_CONFUSION = 9101;
        public const int ID_ZHONG_SAD = 9102;
        public const int ID_ZHONG_HOMESICK = 9103;

        public const int ID_KITSUNE_CARD = 9201;

        public int id;
        public string name;
        public CardType type;
        public CardEffect effect;
        public int cost;
        public int damage;
        public int shield;
        public CardFlags flags;
        public int onDiscardSelfDamage;

        public Card CreateInstance()
        {
            return new Card
            {
                ID = id,
                cardName = name,
                cardType = type,
                cardEffect = effect,
                damage = damage,
                shield = shield
            };
        }

        public static CardBlueprint FromCard(Card c, int cost, CardFlags flags)
        {
            return new CardBlueprint
            {
                id = c.ID,
                name = c.cardName,
                type = c.cardType,
                effect = c.cardEffect,
                cost = cost,
                damage = c.damage,
                shield = c.shield,
                flags = flags,
                onDiscardSelfDamage = 0
            };
        }

        public static CardBlueprint ErLangTimeShield() => new CardBlueprint
        {
            id = ID_ERLANG_TIME_SHIELD,
            name = "Time Shield",
            type = CardType.Power,
            effect = CardEffect.Null,
            cost = 0,
            damage = 0,
            shield = 0
        };
        public static CardBlueprint ErLangBite() => new CardBlueprint
        {
            id = ID_ERLANG_BITE,
            name = "Bite On Leg",
            type = CardType.Power,
            effect = CardEffect.Null,
            cost = 0,
            damage = 0,
            shield = 0
        };
        public static CardBlueprint ErLangAttackX2() => new CardBlueprint
        {
            id = ID_ERLANG_ATTACK_X2,
            name = "Attack x2",
            type = CardType.Power,
            effect = CardEffect.Null,
            cost = 0,
            damage = 0,
            shield = 0
        };
        public static CardBlueprint ErLangAOE() => new CardBlueprint
        {
            id = ID_ERLANG_AOE_3X2,
            name = "AOE 3x2",
            type = CardType.Power,
            effect = CardEffect.Null,
            cost = 0,
            damage = 0,
            shield = 0
        };
        public static CardBlueprint ErLangExtraTurn() => new CardBlueprint
        {
            id = ID_ERLANG_EXTRA_TURN,
            name = "Extra Turn",
            type = CardType.Power,
            effect = CardEffect.Null,
            cost = 0,
            damage = 0,
            shield = 0
        };

public static CardBlueprint ZhongDeepConfusion() => new CardBlueprint
{
    id = ID_ZHONG_DEEP_CONFUSION,
    name = "Deep Confusion",
    type = CardType.Power, // Negative cards are treated as Power/no-op cards
    effect = CardEffect.Null,
    cost = 1, damage = 0, shield = 0
};

public static CardBlueprint ZhongSad() => new CardBlueprint
{
    id = ID_ZHONG_SAD,
    name = "Sad",
    type = CardType.Power, // Negative cards are treated as Power/no-op cards
    effect = CardEffect.Null,
    cost = 0, damage = 0, shield = 0,
    flags = CardFlags.Unremovable
};

public static CardBlueprint ZhongHomesick() => new CardBlueprint
{
    id = ID_ZHONG_HOMESICK,
    name = "Homesick",
    type = CardType.Power, // Negative cards are treated as Power/no-op cards
    effect = CardEffect.Null,
    cost = 0, damage = 0, shield = 0,
    onDiscardSelfDamage = 3
};


        public static CardBlueprint KitsuneCard() => new CardBlueprint
        {
            id = ID_KITSUNE_CARD,
            name = "Fox Charm",
            type = CardType.Power,
            effect = CardEffect.Null,
            cost = 1,
            damage = 0,
            shield = 0
        };
    }

    private struct EnemyStatus
    {
        public int bleed;
        public int weakenTurns;
        public int stunTurns;
    }

    private enum SummonKind { Foxy, Dog }
    private class Summon
    {
        public SummonKind kind;
        public int hp;
    }

    // mask states
    private struct BaoZhengState { public int pendingReduction; }
    private struct DianWeiState { public int activations; public bool nextAttackAOE; }
    private struct ErLangState
    {
        public bool anyErLangEquipped;
        public int lastTurnActivations;
        public int thisTurnActivations;
        public int timeShieldCharges;
    }
    private struct ZhongKuiState { public bool anyZhongKuiEquipped; }
    private struct SunWukongState
    {
        public bool anyWukongEquipped;
        public bool buffActive;
        public bool inverted;
        public int regenPerTurn; // default 0 (you can set)
        public string copiedMaskId;
        public string lastNonWukongMaskId;

        public int GetSigned(int v) => inverted ? -v : v;
    }
    private struct KitsuneState
    {
        public bool anyKitsuneEquipped;
        public bool kitsuneCardSeeded;
        public int kitsuneCardUses;
    }
    private struct SkiState { public bool anySkiEquipped; }

}

// Small helper extension: treat negative as Power for now
public static class CardTypeExtensions
{
    public static CardType NegativeType()
    {
        return CardType.Power;
    }
}
