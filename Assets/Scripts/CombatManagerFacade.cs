using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CombatManagerFacade
/// - Hooks into Player.TakeDamage / Player.AddShield
/// - Owns economy (mana/cost), zones (deck/hand/discard/exile), statuses, summons, extra turn, masks.
/// NOTE: This is a jam script. No refactor. Only added Debug.Log for manual testing.
/// </summary>
public class CombatManagerFacade : MonoBehaviour
{
    // -------------------- DEBUG --------------------
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;
    private static CombatManagerFacade instance;
    private void DLog(string msg)
    {
        if (!enableDebugLogs) return;
        Debug.Log(msg);
    }

    // -------------------- ECONOMY --------------------
    [Header("Economy")]
    public int manaCap = 5;
    public int manaRefillPerTurn = 3;
    public int startHandSize = 5;
    public int drawPerTurn = 2;

    public int Mana => mana;
    public int ManaCap => manaCap;

    public event Action<int, int> OnManaChanged;

    [SerializeField] private PlayerHandController handController;
    public void addMana(int amt)
    {
        mana += amt;
    }
    public static CombatManagerFacade GetInstance()
    {
        return instance;
    }
    private void SetMana(int value)
    {
        int newVal = Mathf.Clamp(value, 0, manaCap);
        if (newVal == mana) return;
        mana = newVal;
        DLog($"[ECON] Mana changed => {mana}/{manaCap}");
        OnManaChanged?.Invoke(mana, manaCap);
    }

    [Header("Ski: shield gain multiplier (less shield)")]
    [Range(0.1f, 1f)] public float skiShieldGainMultiplier = 0.7f;

    [Header("Dian Wei activation cost")]
    public int dianWeiHpCostOnActive = 2;

    public event Action OnRequestExtraTurn;

    private const string MASK_LOG = "[MASK]";

    // -------------------- REFERENCES --------------------
    private Player player;
    private List<Enemy> enemies = new List<Enemy>();

    // External zones (we do NOT change Player.hand structure; hand is UI list)
    private readonly List<CardBlueprint> deck = new List<CardBlueprint>();
    private readonly List<CardBlueprint> discard = new List<CardBlueprint>();
    private readonly List<CardBlueprint> exile = new List<CardBlueprint>();

    // Runtime mapping (Card instance -> runtime meta)
    private readonly Dictionary<Card, CardRuntime> runtime = new Dictionary<Card, CardRuntime>();

    private int mana;

    // Enemy statuses
    private readonly Dictionary<Enemy, EnemyStatus> status = new Dictionary<Enemy, EnemyStatus>();

    // Summons
    private readonly List<Summon> foxies = new List<Summon>();
    private bool dogActive = false;

    // Mask runtime states
    private BaoZhengState bao = new BaoZhengState();
    private DianWeiState dian = new DianWeiState();
    private ErLangState erlang = new ErLangState();
    private ZhongKuiState zhong = new ZhongKuiState();
    private SunWukongState wukong = new SunWukongState();
    private KitsuneState kitsune = new KitsuneState();
    private SkiState ski = new SkiState();
    private GuanYuState guan = new GuanYuState();

    private struct GuanYuState
    {
        public bool anyGuanYuEquipped;
    }


    // Guard (avoid recursion when we call player.TakeDamage inside hook)
    private bool internalDamageCall = false;

    // -------------------- PUBLIC API --------------------
    private void Awake() => instance = this;
    public void Bind(Player p, List<Enemy> enemyList)
    {
        player = p;
        enemies = enemyList ?? new List<Enemy>();

        DLog($"[BIND] Player={(player != null ? "OK" : "NULL")} | Enemies={enemies.Count}");

        status.Clear();
        foreach (var e in enemies)
        {
            if (e != null && !status.ContainsKey(e))
                status.Add(e, new EnemyStatus());
        }
        DLog($"[BIND] Status table initialized for enemies => {status.Count}");

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

            DLog("[BIND] Player hooks bound: BeforeTakeDamage / AfterTakeDamage / BeforeGainShield / OnCardPlayed");
        }
    }

    public void OnBattleStart(MaskData[] equippedMasks, int activeMaskIndex)
    {
        DLog($"[BATTLE] Start | ActiveMaskIndex={activeMaskIndex}");

        ResetAllStates();
        SetMana(manaRefillPerTurn);

        deck.Clear();
        discard.Clear();
        exile.Clear();
        runtime.Clear();

        if (player != null)
        {
            // Collect starting templates from current hand
            DLog($"[BATTLE] Seeding deck from starting hand => handCount={player.hand.Count}");
            foreach (var c in player.hand)
            {
                deck.Add(CardBlueprint.FromCard(c, DefaultCostFor(c), CardFlags.None));
                DLog($"[BATTLE] Seed => {c.cardName} (ID={c.ID}) cost={DefaultCostFor(c)}");
            }

            player.hand.Clear();

            Shuffle(deck);
            DLog($"[BATTLE] Deck shuffled => deckCount={deck.Count}");

            Draw(startHandSize);
            DLog($"[BATTLE] Initial draw => startHandSize={startHandSize} | handCount={player.hand.Count}");
        }

        // Log equipped masks once at battle start
        if (equippedMasks != null)
        {
            for (int i = 0; i < equippedMasks.Length; i++)
            {
                var m = equippedMasks[i];
                if (m == null) DLog($"[BATTLE] EquippedMask[{i}] = NULL");
                else DLog($"[BATTLE] EquippedMask[{i}] = {m.displayName} (id={m.maskId})");
            }
        }
    }

    public void OnPlayerTurnStart(MaskData[] equippedMasks, int activeMaskIndex, int turnNumber)
    {
        DLog($"[TURN] PlayerTurnStart | Turn={turnNumber} | ActiveMaskIndex={activeMaskIndex}");

        int refill = manaRefillPerTurn + (kitsune.anyKitsuneEquipped ? 1 : 0);
        SetMana(refill);
        if (kitsune.anyKitsuneEquipped) DLog($"{MASK_LOG}[Kitsune] TurnStart => manaRefill +1 => {refill}/{manaCap}");


        // ZhongKui cons: each turn add a negative card to discard
        if (HasMask(equippedMasks, "钟馗") || HasMaskId(equippedMasks, "zhong_kui"))
        {
            DLog($"{MASK_LOG}[ZhongKui] TurnStart => adding negative card to discard");
            AddZhongKuiNegativeCardToDiscard();
        }

        // ErLang dog condition check at turn start (if 2 activations last turn)
        dogActive = (erlang.lastTurnActivations >= 2);
        if (dogActive) DLog($"{MASK_LOG}[ErLang] DogActive TRUE (lastTurnActivations={erlang.lastTurnActivations})");
        else DLog($"{MASK_LOG}[ErLang] DogActive FALSE (lastTurnActivations={erlang.lastTurnActivations})");

        // Draw
        int beforeHand = (player != null) ? player.hand.Count : 0;
        Draw(drawPerTurn);
        int afterHand = (player != null) ? player.hand.Count : 0;
        DLog($"[DRAW] Drew {drawPerTurn} | hand {beforeHand} -> {afterHand} | deck={deck.Count} discard={discard.Count}");

        // Turn-based regen (Sun Wukong)
        if (wukong.buffActive && wukong.regenPerTurn != 0)
        {
            int val = wukong.GetSigned(wukong.regenPerTurn);
            DLog($"{MASK_LOG}[SunWukong] TurnRegen => regenPerTurn={wukong.regenPerTurn} signed={val}");
            if (val > 0) player.HealPlayer(val);
            else if (val < 0) SafeInternalDamage(-val);
        }
    }

    public void OnPlayerTurnEnd(MaskData[] equippedMasks, int activeMaskIndex, int turnNumber)
    {
        DLog($"[TURN] PlayerTurnEnd | Turn={turnNumber} | ActiveMaskIndex={activeMaskIndex}");

        // Kitsune summons act at end of player turn
        // Kitsune: simple end-of-turn damage (no foxies)
        if (kitsune.anyKitsuneEquipped)
        {
            int dmg = 3; 
            DLog($"{MASK_LOG}[Kitsune] TurnEnd => deal {dmg} damage to a random alive enemy");
            Enemy e = GetRandomAliveEnemy();
            if (e != null) e.TakeDamage(dmg);
            else DLog($"{MASK_LOG}[Kitsune] No alive enemy to take damage");
        }


        // ErLang track activations
        DLog($"{MASK_LOG}[ErLang] TurnEnd => thisTurnActivations={erlang.thisTurnActivations} (stored to lastTurn)");
        erlang.lastTurnActivations = erlang.thisTurnActivations;
        erlang.thisTurnActivations = 0;
    }

    public bool TryPlayCard(MaskData[] equippedMasks, int activeMaskIndex, Card card, Enemy target)
    {
        if (player == null)
        {
            DLog("[PLAY] Failed: player is NULL");
            return false;
        }
        if (card == null)
        {
            DLog("[PLAY] Failed: card is NULL");
            return false;
        }

        var active = GetActiveMask(equippedMasks, activeMaskIndex);
        DLog($"[PLAY] TryPlayCard => {card.cardName} (ID={card.ID}, type={card.cardType}, chi={card.chi}) | ActiveMask={(active != null ? active.displayName : "NULL")}");

        // Register runtime meta if missing (e.g., newly generated cards)
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
            DLog($"[RUNTIME] Registered new runtime meta => cost={meta.blueprint.cost}");
        }

        // Resolve mask equipped flags quickly
        ApplyEquippedMaskBooleans(equippedMasks, activeMaskIndex);

        // Compute final cost (BaoZheng pending reduction, etc.)
        int cost = meta.blueprint.cost;
        int costBefore = cost;
        cost = ApplyCostHooks(equippedMasks, activeMaskIndex, cost);
        if (cost != costBefore) DLog($"{MASK_LOG}[BaoZheng] CostHook => {costBefore} -> {cost}");

        if (mana < cost)
        {
            DLog($"[ECON] Not enough mana. Need={cost}, Have={mana}");
            return false;
        }

        SetMana(mana - cost);

        // Build action context
        var ctx = new ActionContext
        {
            actor = player,
            card = card,
            target = target,
            cancelPlay = false,
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

        if (ctx.cancelPlay)
        {
            DLog("[PLAY] Cancelled by hook (ctx.cancelPlay = true)");
            return false;
        }

        DLog($"[PLAY] Resolve casts => repeatCount={ctx.repeatCount} isAOE={ctx.isAOE} costBase={ctx.baseCost} costFinal={ctx.finalCost}");

        // Execute casts
        for (int i = 0; i < ctx.repeatCount; i++)
        {
            float dmgMul = (i == 1) ? ctx.secondCastDamageMultiplier : 1f;
            DLog($"[CAST] #{i + 1}/{ctx.repeatCount} dmgMul={dmgMul}");
            ResolveCardCast(equippedMasks, activeMaskIndex, ctx.card, ctx.target, ctx.isAOE, dmgMul);
        }

        // After play hooks (set BaoZheng pending, Kitsune counters, etc.)
        RunAfterPlayHooks(equippedMasks, activeMaskIndex, ref ctx);

        // Move to discard/exile
        bool banish = ctx.banishThisCard || meta.banishOnPlay;
        DLog($"[ZONE] MovePlayedCard => banish={banish}");
        MovePlayedCardToZone(ctx.card, banish);

        // Extra turn request
        if (ctx.requestExtraTurn)
        {
            DLog("[TURN] Extra turn requested by ctx.requestExtraTurn");
            OnRequestExtraTurn?.Invoke();
        }

        DLog($"[PLAY] Success => {card.cardName} (ID={card.ID})");
        return true;
    }

    public void AfterEnemyAction(Enemy enemy)
    {
        if (enemy == null) return;
        if (!status.TryGetValue(enemy, out var st)) return;

        DLog($"[ENEMY] AfterEnemyAction | bleed={st.bleed} weakenTurns={st.weakenTurns} stunTurns={st.stunTurns}");

        // Ski: after enemy moves, trigger bleed 3 times randomly
        if (ski.anySkiEquipped && st.bleed > 0)
        {
            DLog($"{MASK_LOG}[Ski] AfterEnemyAction => rolling 3 times | bleed={st.bleed}");
            for (int i = 0; i < 3; i++)
            {
                float r = UnityEngine.Random.value;
                if (r < 0.75f)
                {
                    DLog($"{MASK_LOG}[Ski] Roll#{i + 1}: Damage enemy for bleed={st.bleed}");
                    enemy.TakeDamage(st.bleed);
                }
                else if (r < 0.90f)
                {
                    st.weakenTurns = Mathf.Max(st.weakenTurns, 1);
                    DLog($"{MASK_LOG}[Ski] Roll#{i + 1}: Apply weakenTurns => {st.weakenTurns}");
                }
                else
                {
                    st.bleed += 1;
                    DLog($"{MASK_LOG}[Ski] Roll#{i + 1}: Increase bleed => {st.bleed}");
                }
            }
            status[enemy] = st;
        }

        // Tick down after action
        if (st.stunTurns > 0) st.stunTurns -= 1;
        if (st.weakenTurns > 0) st.weakenTurns -= 1;
        status[enemy] = st;

        DLog($"[ENEMY] AfterEnemyAction END | bleed={st.bleed} weakenTurns={st.weakenTurns} stunTurns={st.stunTurns}");
    }

    // -------------------- HOOKS INTO PLAYER --------------------

    private void HandleBeforeTakeDamage(ref Player.DamageContext ctx)
    {
        if (internalDamageCall) return;

        DLog($"[HOOK][BeforeTakeDamage] incoming={ctx.incomingDamage} cancel={ctx.cancelDamage} shieldEff={ctx.shieldEfficiency}");

        // ErLang passive: take 1.4x dmg when equipped
        if (erlang.anyErLangEquipped)
        {
            int before = ctx.incomingDamage;
            ctx.incomingDamage = Mathf.CeilToInt(ctx.incomingDamage * 1.4f);
            DLog($"{MASK_LOG}[ErLang] Passive dmg amp => {before} -> {ctx.incomingDamage}");
        }

        // ErLang time shield: immune once
        if (erlang.timeShieldCharges > 0)
        {
            erlang.timeShieldCharges -= 1;
            ctx.cancelDamage = true;
            DLog($"{MASK_LOG}[ErLang] TimeShield consumed => cancelDamage TRUE | remaining={erlang.timeShieldCharges}");
            return;
        }

    }

    private void HandleAfterTakeDamage(int hpLoss)
    {
        if (internalDamageCall) return;

        DLog($"[HOOK][AfterTakeDamage] hpLoss={hpLoss}");

        // SunWukong: if attacked while mask equipped, buffs become debuffs
        if (wukong.anyWukongEquipped && hpLoss > 0)
        {
            wukong.inverted = true;
            DLog($"{MASK_LOG}[SunWukong] Got hit => inverted TRUE (buffs become debuffs)");
        }
    }

    private void HandleBeforeGainShield(ref Player.ShieldContext ctx)
    {
        DLog($"[HOOK][BeforeGainShield] amount={ctx.amount}");

        if (ski.anySkiEquipped)
        {
            int before = ctx.amount;
            ctx.amount = Mathf.FloorToInt(ctx.amount * skiShieldGainMultiplier);
            DLog($"{MASK_LOG}[Ski] Shield multiplier => {before} -> {ctx.amount} (x{skiShieldGainMultiplier})");
        }
    }

    private void HandleOnCardPlayed()
    {
        DLog("[HOOK][OnCardPlayed] Player fired OnCardPlayed");
    }

    // -------------------- CARD / ZONE OPS --------------------

    private void Draw(int count)
    {
        if (player == null) return;

        for (int i = 0; i < count; i++)
        {
            if (deck.Count == 0)
            {
                if (discard.Count == 0)
                {
                    DLog("[DRAW] Stop: deck empty and discard empty");
                    return;
                }
                deck.AddRange(discard);
                discard.Clear();
                Shuffle(deck);
                DLog($"[DRAW] Reshuffle discard->deck | deck={deck.Count}");
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

            DLog($"[DRAW] + {c.cardName} (ID={c.ID}) cost={bp.cost} | hand={player.hand.Count} deck={deck.Count}");
        }
    }

    private void MovePlayedCardToZone(Card c, bool toExile)
    {
        if (player == null || c == null) return;

        if (!runtime.TryGetValue(c, out var meta))
        {
            DLog("[ZONE] MovePlayedCardToZone failed: no runtime meta found");
            return;
        }

        if (toExile && !meta.unremovable)
        {
            exile.Add(meta.blueprint);
            DLog($"[ZONE] -> EXILE : {meta.blueprint.name} (ID={meta.blueprint.id}) | exile={exile.Count}");
        }
        else
        {
            discard.Add(meta.blueprint);
            DLog($"[ZONE] -> DISCARD : {meta.blueprint.name} (ID={meta.blueprint.id}) | discard={discard.Count}");

            // Homesick: takes 3 damage when discarded
            if (meta.onDiscardSelfDamage > 0)
            {
                DLog($"[ZONE] onDiscardSelfDamage => {meta.onDiscardSelfDamage}");
                SafeInternalDamage(meta.onDiscardSelfDamage);
            }
        }

        runtime.Remove(c);
    }

    // -------------------- CARD RESOLUTION --------------------

    private void ResolveCardCast(MaskData[] equippedMasks, int activeMaskIndex, Card card, Enemy target, bool isAOE, float damageMultiplier)
    {
        if (card == null) return;

        DLog($"[RESOLVE] CardCast => {card.cardName} (ID={card.ID}, type={card.cardType}) AOE={isAOE} dmgMul={damageMultiplier}");

        // Special generated cards by ID
        if (TryResolveSpecialGeneratedCard(equippedMasks, activeMaskIndex, card, target))
        {
            DLog("[RESOLVE] Special generated card resolved (no normal resolution)");
            return;
        }

        // Normal card: we do Attack/Defense ourselves (jam-simple)
        if (card.cardType == CardType.Attack)
        {
            if (isAOE)
            {
                DLog("[RESOLVE] Attack AOE");
                foreach (var e in enemies)
                {
                    if (e == null || !e.IsAlive()) continue;
                    DealDamageToEnemy_WithHooks(equippedMasks, activeMaskIndex, card, e, damageMultiplier);
                }
            }
            else
            {
                if (target == null || !target.IsAlive())
                {
                    DLog("[RESOLVE] Attack cancelled: target invalid");
                    return;
                }
                DealDamageToEnemy_WithHooks(equippedMasks, activeMaskIndex, card, target, damageMultiplier);
            }
        }
        else if (card.cardType == CardType.Defense)
        {
            int shieldGain = card.shield;

            // SunWukong buff: shield card +1 (signed if inverted)
            if (wukong.buffActive)
            {
                int before = shieldGain;
                shieldGain += wukong.GetSigned(1);
                DLog($"{MASK_LOG}[SunWukong] Defense shield bonus => {before} -> {shieldGain}");
            }

            // Guan Yu: all defense cards +1 shield (passive)
            if (guan.anyGuanYuEquipped)
            {
                int before = shieldGain;
                shieldGain += 1;
                DLog($"{MASK_LOG}[GuanYu] Defense shield +1 => {before} -> {shieldGain}");
            }


            if (shieldGain > 0) player.AddShield(shieldGain);
            else if (shieldGain < 0) SafeInternalDamage(-shieldGain);
        }

        else
        {
            DLog("[RESOLVE] Power/Other => no default effect here");
        }
    }

    private void DealDamageToEnemy_WithHooks(MaskData[] equippedMasks, int activeMaskIndex, Card card, Enemy enemy, float damageMultiplier)
    {
        if (enemy == null || !enemy.IsAlive()) return;

        // base damage from card.damage + SunWukong strength signed bonus
        int baseDmg = card.damage;
        baseDmg += playerStrengthSignedBonus();

        // Guan Yu: all attack cards +3 damage (passive)
        if (guan.anyGuanYuEquipped)
        {
            baseDmg += 3;
            DLog($"{MASK_LOG}[GuanYu] Attack damage +3 => baseDmg now {baseDmg}");
        }

        baseDmg = Mathf.Max(0, baseDmg);


        int dmg = Mathf.RoundToInt(baseDmg * damageMultiplier);

        DLog($"[DMG] DealDamage => base={baseDmg} mul={damageMultiplier} final={dmg}");

        enemy.TakeDamage(dmg);

        // Ski: all attacks cause bleed
        if (ski.anySkiEquipped)
        {
            
            if (status.TryGetValue(enemy, out var st))
            {
                if(UnityEngine.Random.value <0.5f)
                {
                    st.bleed += 1;
                    status[enemy] = st;
                    DLog($"{MASK_LOG}[Ski] Apply bleed +1 => bleed={st.bleed}");

                }
            }
        }

        // ZhongKui lifesteal 10%
        if (IsActiveMask(equippedMasks, activeMaskIndex, "钟馗", "zhong_kui") && dmg > 0)
        {
            int heal = Mathf.FloorToInt(dmg * 0.1f);
            if (heal > 0)
            {
                DLog($"{MASK_LOG}[ZhongKui] Lifesteal => heal={heal}");
                player.HealPlayer(heal);
            }
        }

        // Kill check: if enemy dead after taking damage => +2 mana
        if (zhong.anyZhongKuiEquipped && !enemy.IsAlive())
        {
            DLog($"{MASK_LOG}[ZhongKui] Kill => +2 mana");
            SetMana(mana + 2);
        }
    }

    private int playerStrengthSignedBonus()
    {
        if (!wukong.buffActive) return 0;
        int val = wukong.GetSigned(1);
        DLog($"{MASK_LOG}[SunWukong] Strength signed bonus => {val}");
        return val;
    }

    // -------------------- MASK HOOK PIPELINE --------------------
    private bool IsActiveWukong(MaskData[] equippedMasks, int activeMaskIndex)
    {
        return IsActiveMask(equippedMasks, activeMaskIndex, "孙悟空", "sun_wukong");
    }

    private bool TryGetWukongCopiedMaskId(out string copiedId)
    {
        copiedId = wukong.copiedMaskId;
        return wukong.buffActive && !string.IsNullOrEmpty(copiedId);
    }

    private void ApplyCopiedMask_BeforePlay(string copiedMaskId, ref ActionContext ctx)
    {
        // copiedMaskId comes from lastNonWukongMaskId (usually maskId like "er_lang_shen")
        // We treat it as "the mask whose ACTIVE behavior should be copied this play".

        if (string.IsNullOrEmpty(copiedMaskId)) return;

        // DianWei copy: double cast + hp cost + every 3rd => next AOE
        if (copiedMaskId == "dian_wei")
        {
            DLog($"{MASK_LOG}[SunWukong->Copy] Copied DianWei ACTIVE");

            ctx.repeatCount = 2;
            ctx.secondCastDamageMultiplier = 0.5f;

            dian.activations++;
            SafeInternalDamage(dianWeiHpCostOnActive);

            if (dian.activations % 3 == 0)
            {
                dian.nextAttackAOE = true;
                DLog($"{MASK_LOG}[SunWukong->Copy] DianWei every 3rd => nextAttackAOE TRUE");
            }
            return;
        }

        // ErLang copy: add random 0-cost card to hand
        if (copiedMaskId == "er_lang_shen")
        {
            erlang.thisTurnActivations++;
            DLog($"{MASK_LOG}[SunWukong->Copy] Copied ErLang ACTIVE => add random 0-cost card | thisTurnActivations={erlang.thisTurnActivations}");

            AddErLangRandomZeroCostCardToHand();
            return;
        }

        // BaoZheng copy: (Step0) no banish; only AFTER effect is meaningful
        if (copiedMaskId == "bao_zheng")
        {
            DLog($"{MASK_LOG}[SunWukong->Copy] Copied BaoZheng ACTIVE (banish disabled in Step0)");
            // nothing to do before play (no exile)
            return;
        }

        // ZhongKui / Kitsune / Ski etc — you can add here later if needed.
        DLog($"{MASK_LOG}[SunWukong->Copy] No copied ACTIVE implemented for copiedMaskId={copiedMaskId}");
    }


    private void RunBeforePlayHooks(MaskData[] equippedMasks, int activeMaskIndex, ref ActionContext ctx)
    {
        // DianWei active -> double cast
        if (IsActiveMask(equippedMasks, activeMaskIndex, "典韦", "dian_wei"))
        {
            DLog($"{MASK_LOG}[DianWei] ACTIVE => double cast + HP cost ({dianWeiHpCostOnActive})");

            ctx.repeatCount = 2;
            ctx.secondCastDamageMultiplier = 0.5f;

            dian.activations++;
            SafeInternalDamage(dianWeiHpCostOnActive);

            if (dian.activations % 3 == 0)
            {
                dian.nextAttackAOE = true;
                DLog($"{MASK_LOG}[DianWei] Every 3rd activation => nextAttackAOE TRUE");
            }
        }

        // DianWei next AOE flag
        if (dian.nextAttackAOE && ctx.card.cardType == CardType.Attack)
        {
            ctx.isAOE = true;
            dian.nextAttackAOE = false;
            DLog($"{MASK_LOG}[DianWei] Consumed nextAttackAOE => this attack becomes AOE");
        }


        // SunWukong active -> apply buffs (once per activation)
        if (IsActiveMask(equippedMasks, activeMaskIndex, "孙悟空", "sun_wukong"))
        {
            wukong.buffActive = true;
            wukong.inverted = false;
            wukong.copiedMaskId = wukong.lastNonWukongMaskId;

            DLog($"{MASK_LOG}[SunWukong] ACTIVE => buffActive TRUE, inverted FALSE, copiedMaskId={wukong.copiedMaskId}");
        }

        // SunWukong COPY: also run the copied mask's ACTIVE hook
        if (IsActiveWukong(equippedMasks, activeMaskIndex))
        {
            if (TryGetWukongCopiedMaskId(out var copied))
            {
                ApplyCopiedMask_BeforePlay(copied, ref ctx);
            }
        }

        // ErLang active -> add 0-cost random card to hand each activation
        if (IsActiveMask(equippedMasks, activeMaskIndex, "二郎神", "er_lang_shen"))
        {
            erlang.thisTurnActivations++;
            DLog($"{MASK_LOG}[ErLang] ACTIVE => add random 0-cost card | thisTurnActivations={erlang.thisTurnActivations}");
            AddErLangRandomZeroCostCardToHand();
        }

        // BaoZheng con (banish) disabled for now
        if (IsActiveMask(equippedMasks, activeMaskIndex, "包拯", "bao_zheng"))
        {
            // do nothing (no exile)
            DLog($"{MASK_LOG}[BaoZheng] ACTIVE => (banish disabled)");
        }


        //// Kitsune: seed its 1-cost card once
        //if (kitsune.anyKitsuneEquipped && !kitsune.kitsuneCardSeeded)
        //{
        //    kitsune.kitsuneCardSeeded = true;
        //    discard.Add(CardBlueprint.KitsuneCard());
        //    DLog($"{MASK_LOG}[Kitsune] Seeded Kitsune card into discard (so it appears soon)");
        //}
    }

    private void RunAfterPlayHooks(MaskData[] equippedMasks, int activeMaskIndex, ref ActionContext ctx)
    {
        // BaoZheng: next card costs minus this card cost (if 0 then -1), gain shield equal reduced amount
        if (IsActiveMask(equippedMasks, activeMaskIndex, "包拯", "bao_zheng"))
        {
            int reduce = Mathf.Max(ctx.finalCost, 1);
            bao.pendingReduction = reduce;

            DLog($"{MASK_LOG}[BaoZheng] AFTER => pendingReduction={reduce} (applies to NEXT card), gain shield={reduce}");
            player.AddShield(reduce);
        }

        // SunWukong: record last non-wukong active mask
        var active = GetActiveMask(equippedMasks, activeMaskIndex);
        if (active != null && active.displayName != "孙悟空" && active.maskId != "sun_wukong")
        {
            wukong.lastNonWukongMaskId = active.maskId ?? active.displayName;
            DLog($"{MASK_LOG}[SunWukong] Record lastNonWukongMaskId => {wukong.lastNonWukongMaskId}");
        }

        // SunWukong COPY: if copied mask is BaoZheng, apply BaoZheng AFTER too
        if (IsActiveWukong(equippedMasks, activeMaskIndex))
        {
            if (TryGetWukongCopiedMaskId(out var copied) && copied == "bao_zheng")
            {
                int reduce = Mathf.Max(ctx.finalCost, 1);
                bao.pendingReduction = reduce;

                DLog($"{MASK_LOG}[SunWukong->Copy] BaoZheng AFTER => pendingReduction={reduce} (applies to NEXT card), gain shield={reduce}");
                player.AddShield(reduce);
            }
        }
    }

    private int ApplyCostHooks(MaskData[] equippedMasks, int activeMaskIndex, int cost)
    {
        // BaoZheng pending reduction affects NEXT card
        if (bao.pendingReduction > 0)
        {
            int before = cost;
            cost = Mathf.Max(0, cost - bao.pendingReduction);
            DLog($"{MASK_LOG}[BaoZheng] Apply pendingReduction => {before} - {bao.pendingReduction} = {cost}");
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
            DLog($"{MASK_LOG}[ErLang] SpecialCard TimeShield => charges={erlang.timeShieldCharges}");
            return true;
        }
        if (card.ID == CardBlueprint.ID_ERLANG_BITE)
        {
            DLog($"{MASK_LOG}[ErLang] SpecialCard Bite => (stun logic depends on status table)");
            if (target != null && status.TryGetValue(target, out var st))
            {
                st.stunTurns = Mathf.Max(st.stunTurns, 1);
                status[target] = st;
                DLog($"{MASK_LOG}[ErLang] Applied stunTurns=1 to target");
            }
            return true;
        }
        if (card.ID == CardBlueprint.ID_ERLANG_ATTACK_X2)
        {
            DLog($"{MASK_LOG}[ErLang] SpecialCard AttackX2 => instant 6 dmg");
            if (target == null) return true;
            target.TakeDamage(6);
            return true;
        }
        if (card.ID == CardBlueprint.ID_ERLANG_AOE_3X2)
        {
            DLog($"{MASK_LOG}[ErLang] SpecialCard AOE => instant 6 dmg to all alive enemies");
            foreach (var e in enemies)
                if (e != null && e.IsAlive()) e.TakeDamage(6);
            return true;
        }
        if (card.ID == CardBlueprint.ID_ERLANG_EXTRA_TURN)
        {
            DLog($"{MASK_LOG}[ErLang] SpecialCard ExtraTurn => request extra turn");
            OnRequestExtraTurn?.Invoke();
            return true;
        }

        // ZhongKui negative cards (no effect in play)
        if (card.ID == CardBlueprint.ID_ZHONG_DEEP_CONFUSION)
        {
            DLog($"{MASK_LOG}[ZhongKui] NegativeCard DeepConfusion played => no-op");
            return true;
        }
        if (card.ID == CardBlueprint.ID_ZHONG_SAD)
        {
            DLog($"{MASK_LOG}[ZhongKui] NegativeCard Sad played => no-op");
            return true;
        }
        if (card.ID == CardBlueprint.ID_ZHONG_HOMESICK)
        {
            DLog($"{MASK_LOG}[ZhongKui] NegativeCard Homesick played => no-op (damage happens on discard)");
            return true;
        }

        // Kitsune card: Gain 2 shield, draw 1
        if (card.ID == CardBlueprint.ID_KITSUNE_CARD)
        {
            DLog($"{MASK_LOG}[Kitsune] SpecialCard FoxCharm => +2 shield, draw 1");
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

        // 关键：用 HandController 才会真的进“你的手牌系统”
        if (handController != null)
        {
            bool ok = handController.AddCardFromPrefab(c);
            if (!ok)
            {
                DLog($"{MASK_LOG}[ErLang] AddCardFromPrefab FAILED (hand full?) => {c.cardName} (ID={c.ID})");
                return;
            }
        }
        else
        {
            // 保底：如果你忘了拖引用，至少别直接悄悄丢失
            DLog($"{MASK_LOG}[ErLang] handController is NULL, fallback to player.hand (UI won't show!)");
            if (player != null) player.hand.Add(c);
        }

        // runtime meta（保证 0 费，且能被 MovePlayedCardToZone 处理）
        runtime[c] = new CardRuntime
        {
            blueprint = bp,
            banishOnPlay = false,
            unremovable = false,
            onDiscardSelfDamage = bp.onDiscardSelfDamage
        };

        DLog($"{MASK_LOG}[ErLang] Added random 0-cost card => {c.cardName} (ID={c.ID})");
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
        DLog($"{MASK_LOG}[ZhongKui] Added negative to discard => {bp.name} (ID={bp.id}) | discard={discard.Count}");
    }

    private void ResolveFoxyEndOfTurn()
    {
        if (foxies.Count == 0)
        {
            DLog($"{MASK_LOG}[Kitsune] No foxies to resolve");
            return;
        }

        DLog($"{MASK_LOG}[Kitsune] ResolveFoxyEndOfTurn => foxiesCount={foxies.Count}");

        // each foxy: +2 shield, +5 heal, deal 7 damage to random alive enemy
        for (int i = 0; i < foxies.Count; i++)
        {
            DLog($"{MASK_LOG}[Kitsune] Foxy#{i + 1} => +2 shield, +5 heal, random enemy -7");
            player.AddShield(2);
            player.HealPlayer(5);

            Enemy e = GetRandomAliveEnemy();
            if (e != null) e.TakeDamage(7);
            else DLog($"{MASK_LOG}[Kitsune] No alive enemy for Foxy damage");
        }
    }

    private Enemy GetRandomAliveEnemy()
    {
        List<Enemy> alive = new List<Enemy>();
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
        // You DO have Player.Health now, but leaving your original behavior (jam-safe).
        // If you want accurate, replace with: return player != null ? player.Health : 100;
        return 100;
    }

    private void SafeInternalDamage(int dmg)
    {
        if (player == null) return;
        internalDamageCall = true;
        DLog($"[INTERNAL] Deal internal damage => {dmg}");
        player.TakeDamage(dmg);
        internalDamageCall = false;
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, list.Count);
            T tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    private static int DefaultCostFor(Card c)
    {
        if (c == null) return 1;
        return Mathf.Max(0, c.chi);
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
        guan = new GuanYuState();

        foxies.Clear();
        dogActive = false;

        DLog("[RESET] All mask states reset. Summons cleared.");
    }

    private void ApplyEquippedMaskBooleans(MaskData[] equippedMasks, int activeMaskIndex)
    {
        for (int i = 0; i < 3; i++)
        {
            var m = (equippedMasks != null && i < equippedMasks.Length) ? equippedMasks[i] : null;
            DLog($"[MASK][DATA] slot{i}: name={(m ? m.displayName : "NULL")} | id={(m ? m.maskId : "NULL")}");
        }

        erlang.anyErLangEquipped = HasMask(equippedMasks, "二郎神") || HasMaskId(equippedMasks, "er_lang_shen");
        zhong.anyZhongKuiEquipped = HasMask(equippedMasks, "钟馗") || HasMaskId(equippedMasks, "zhong_kui");
        wukong.anyWukongEquipped = HasMask(equippedMasks, "孙悟空") || HasMaskId(equippedMasks, "sun_wukong");
        kitsune.anyKitsuneEquipped = HasMask(equippedMasks, "狐狸") || HasMaskId(equippedMasks, "kitsune") || HasMaskId(equippedMasks, "ksume");
        ski.anySkiEquipped = HasMask(equippedMasks, "Ski") || HasMaskId(equippedMasks, "ski");
        guan.anyGuanYuEquipped =HasMask(equippedMasks, "关羽") || HasMaskId(equippedMasks, "guan_yu");


        DLog($"{MASK_LOG} Equipped flags => ErLang={erlang.anyErLangEquipped} ZhongKui={zhong.anyZhongKuiEquipped} SunWukong={wukong.anyWukongEquipped} Kitsune={kitsune.anyKitsuneEquipped} Ski={ski.anySkiEquipped} GuanYu={guan.anyGuanYuEquipped}");

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
            type = CardType.Power,
            effect = CardEffect.Null,
            cost = 1,
            damage = 0,
            shield = 0
        };

        public static CardBlueprint ZhongSad() => new CardBlueprint
        {
            id = ID_ZHONG_SAD,
            name = "Sad",
            type = CardType.Power,
            effect = CardEffect.Null,
            cost = 0,
            damage = 0,
            shield = 0,
            flags = CardFlags.Unremovable
        };

        public static CardBlueprint ZhongHomesick() => new CardBlueprint
        {
            id = ID_ZHONG_HOMESICK,
            name = "Homesick",
            type = CardType.Power,
            effect = CardEffect.Null,
            cost = 0,
            damage = 0,
            shield = 0,
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

    // Mask states
    private struct BaoZhengState { public int pendingReduction; }

    private struct DianWeiState
    {
        public int activations;
        public bool nextAttackAOE;
    }

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
        public int regenPerTurn;
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
