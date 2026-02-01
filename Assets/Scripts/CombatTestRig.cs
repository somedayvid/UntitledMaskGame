using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CombatTestRig : MonoBehaviour
{
    [Header("Scene Refs")]
    public CombatManagerFacade combat;
    public Player player;
    public MaskManager maskManager;

    [Tooltip("留空则自动 FindObjectsOfType<DummyEnemy>()")]
    public DummyEnemy[] enemies;

    [Header("Test Options")]
    public bool bindOnStart = true;

    // --- reflection cache
    BindingFlags BF = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    void Start()
    {
        if (bindOnStart) Bind();
    }

    public void Bind()
    {
        if (combat == null || player == null || maskManager == null)
        {
            Debug.LogError("[CombatTestRig] Missing refs (combat/player/maskManager).");
            return;
        }

        if (enemies == null || enemies.Length == 0)
            enemies = FindObjectsOfType<DummyEnemy>(true);

        var list = new List<Enemy>();
        foreach (var e in enemies)
            if (e != null) list.Add(e);

        combat.Bind(player, list);
        Debug.Log($"[CombatTestRig] Bound. Enemies={list.Count}");
    }

    // ---------------------------
    // Button entry points
    // ---------------------------

    public void Test_BaoZheng()
    {
        ResetForTest(queue0: "bao_zheng", queue1: null, queue2: null, active: 0);

        // 两张卡：第一张 cost=2，第二张 cost=2
        var c1 = MakeAttack("A(cost2)", chi: 2, dmg: 10);
        var c2 = MakeAttack("B(cost2)", chi: 2, dmg: 10);
        player.hand.Clear();
        player.hand.Add(c1);
        player.hand.Add(c2);

        int exile0 = GetExileCount();
        int mana0 = GetMana();

        bool ok1 = combat.TryPlayCard(SnapshotMasks(), GetActiveIndex(), c1, GetFirstAliveEnemy());
        Assert(ok1, "BaoZheng: first card should play");
        Assert(GetExileCount() == exile0 + 1, "BaoZheng: first card should be EXILED (banish)");

        int mana1 = GetMana();
        bool ok2 = combat.TryPlayCard(SnapshotMasks(), GetActiveIndex(), c2, GetFirstAliveEnemy());
        Assert(ok2, "BaoZheng: second card should play");

        int mana2 = GetMana();
        Debug.Log($"[BaoZheng] Mana {mana0} -> {mana1} -> {mana2}. (Expect 2nd card discounted)");
    }

    public void Test_ZhongKui_KillGivesMana()
    {
        ResetForTest(queue0: "zhong_kui", queue1: null, queue2: null, active: 0);

        // 让敌人血很低：用反射直接改 DummyEnemy.health
        var e = GetFirstAliveDummy();
        SetDummyHP(e, 3);

        // 做一张能击杀的攻击卡：dmg=10 cost=1
        var kill = MakeAttack("Kill", chi: 1, dmg: 10);
        player.hand.Clear();
        player.hand.Add(kill);

        int mana0 = GetMana();
        bool ok = combat.TryPlayCard(SnapshotMasks(), GetActiveIndex(), kill, e);
        Assert(ok, "ZhongKui: kill card should play");
        Assert(!e.IsAlive(), "ZhongKui: enemy should be dead");

        int mana1 = GetMana();
        Debug.Log($"[ZhongKui] Mana {mana0} -> {mana1} (Expect +2 mana capped by maxMana)");
    }

    public void Test_ErLang_TimeShield()
    {
        ResetForTest(queue0: "er_lang_shen", queue1: null, queue2: null, active: 0);

        // 先打出任意卡触发“加一张0费随机牌进手牌”
        var poke = MakeDefense("Poke", chi: 1, shield: 1);
        player.hand.Clear();
        player.hand.Add(poke);

        int hand0 = player.hand.Count;
        bool ok = combat.TryPlayCard(SnapshotMasks(), GetActiveIndex(), poke, null);
        Assert(ok, "ErLang: poke should play");
        Assert(player.hand.Count == hand0 /*played removes*/ ? false : true,
            "ErLang: after play, hand should have received a new 0-cost card (net count varies).");

        // 找到那张 Time Shield（id 在 CombatManagerFacade 里是 "erlang_time_shield"）
        Card timeShield = FindCardByNameContains(player.hand, "Time Shield");
        if (timeShield == null)
        {
            Debug.LogWarning("[ErLang] Didn't roll Time Shield. Click again until you get it (30%).");
            return;
        }

        // 用掉 time shield
        bool ok2 = combat.TryPlayCard(SnapshotMasks(), GetActiveIndex(), timeShield, null);
        Assert(ok2, "ErLang: time shield card should play");

        // 让 DummyEnemy 行动一次：应该被免疫（Player.TakeDamage 会 cancel）
        int hpBefore = GetPlayerHP();
        GetFirstAliveEnemy().ResolveAction(player);
        int hpAfter = GetPlayerHP();

        Assert(hpAfter == hpBefore, "ErLang: Time Shield should negate next incoming damage once.");
        Debug.Log("[ErLang] Time Shield OK");
    }

    public void Test_Ski_BleedAfterEnemyAction()
    {
        ResetForTest(queue0: "ski", queue1: null, queue2: null, active: 0);

        var e = GetFirstAliveDummy();
        SetDummyHP(e, 50);

        // 出一张攻击，让 enemy 获得 bleed+1
        var atk = MakeAttack("Atk", chi: 1, dmg: 1);
        player.hand.Clear();
        player.hand.Add(atk);

        bool ok = combat.TryPlayCard(SnapshotMasks(), GetActiveIndex(), atk, e);
        Assert(ok, "Ski: attack should play");

        int bleed = GetEnemyBleed(e);
        Assert(bleed >= 1, $"Ski: bleed should be >=1 after attack. got={bleed}");

        // 调 AfterEnemyAction，会触发 3 次随机效果，其中 75% 会造成 bleed 点伤害
        int hpBefore = GetDummyHP(e);
        combat.AfterEnemyAction(e);
        int hpAfter = GetDummyHP(e);

        Debug.Log($"[Ski] Bleed={bleed}, DummyHP {hpBefore}->{hpAfter} (expect usually decreases, randomness ok)");
    }

    // ---------------------------
    // Setup helpers
    // ---------------------------

    void ResetForTest(string queue0, string queue1, string queue2, int active)
    {
        if (combat == null) { Debug.LogError("combat missing"); return; }
        if (player == null) { Debug.LogError("player missing"); return; }
        if (maskManager == null) { Debug.LogError("maskManager missing"); return; }

        // set mask queue + activeIndex via reflection (no need to modify MaskManager)
        SetMaskQueue(queue0, queue1, queue2, active);

        // reset enemies
        foreach (var d in enemies)
            if (d != null) d.ResetDummy();

        // IMPORTANT: CombatManagerFacade.OnBattleStart 会把“当前 hand 当作 deck 模板”然后清空 hand 再抽牌
        // 所以这里把 startHandSize 临时改成 0，避免它乱抽影响测试
        int oldStartHand = combat.startHandSize;
        int oldDrawPerTurn = combat.drawPerTurn;
        combat.startHandSize = 0;
        combat.drawPerTurn = 0;

        combat.OnBattleStart(SnapshotMasks(), GetActiveIndex());
        combat.OnPlayerTurnStart(SnapshotMasks(), GetActiveIndex(), 1);

        combat.startHandSize = oldStartHand;
        combat.drawPerTurn = oldDrawPerTurn;

        player.hand.Clear();
    }

    Card MakeAttack(string name, int chi, int dmg)
    {
        var c = new Card();
        c.cardName = name;
        c.cardType = CardType.Attack;
        c.chi = chi;
        c.damage = dmg;
        c.shield = 0;
        return c;
    }

    Card MakeDefense(string name, int chi, int shield)
    {
        var c = new Card();
        c.cardName = name;
        c.cardType = CardType.Defense;
        c.chi = chi;
        c.damage = 0;
        c.shield = shield;
        return c;
    }

    MaskData[] SnapshotMasks()
    {
        return new MaskData[]
        {
            maskManager.GetQueueMask(0),
            maskManager.GetQueueMask(1),
            maskManager.GetQueueMask(2),
        };
    }

    int GetActiveIndex()
    {
        // ActiveIndex 是 public getter
        return maskManager.ActiveIndex;
    }

    Enemy GetFirstAliveEnemy()
    {
        foreach (var d in enemies)
            if (d != null && d.IsAlive()) return d;
        return enemies != null && enemies.Length > 0 ? enemies[0] : null;
    }

    DummyEnemy GetFirstAliveDummy()
    {
        foreach (var d in enemies)
            if (d != null && d.IsAlive()) return d;
        return enemies != null && enemies.Length > 0 ? enemies[0] : null;
    }

    // ---------------------------
    // Reflection read/write (Combat)
    // ---------------------------

    int GetMana()
    {
        return (int)GetPrivate(combat, "mana");
    }

    int GetExileCount()
    {
        var exile = GetPrivate(combat, "exile") as System.Collections.ICollection;
        return exile != null ? exile.Count : -1;
    }

    int GetDiscardCount()
    {
        var discard = GetPrivate(combat, "discard") as System.Collections.ICollection;
        return discard != null ? discard.Count : -1;
    }

    int GetEnemyBleed(Enemy e)
    {
        // Combat has: private readonly Dictionary<Enemy, EnemyStatus> status
        var dict = GetPrivate(combat, "status") as System.Collections.IDictionary;
        if (dict == null || e == null || !dict.Contains(e)) return 0;

        object st = dict[e]; // boxed struct EnemyStatus
        return (int)GetField(st, "bleed");
    }

    // ---------------------------
    // Reflection read/write (MaskManager)
    // ---------------------------

    void SetMaskQueue(string id0, string id1, string id2, int activeIndex)
    {
        var q = new MaskData[3];
        q[0] = FindMaskById(id0);
        q[1] = FindMaskById(id1);
        q[2] = FindMaskById(id2);

        SetPrivate(maskManager, "queue", q);
        SetPrivate(maskManager, "activeIndex", Mathf.Clamp(activeIndex, 0, 2));
    }

    MaskData FindMaskById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var m in maskManager.allMasks)
            if (m != null && m.maskId == id) return m;

        Debug.LogError($"[CombatTestRig] Mask id not found in MaskManager.allMasks: {id}");
        return null;
    }

    // ---------------------------
    // DummyEnemy internals (HP)
    // ---------------------------

    int GetDummyHP(DummyEnemy d)
    {
        if (d == null) return -1;
        return (int)GetPrivate(d, "health");
    }

    void SetDummyHP(DummyEnemy d, int hp)
    {
        if (d == null) return;
        SetPrivate(d, "health", Mathf.Max(0, hp));
    }

    int GetPlayerHP()
    {
        return (int)GetPrivate(player, "health");
    }

    // ---------------------------
    // Card finders
    // ---------------------------

    Card FindCardByNameContains(List<Card> list, string contains)
    {
        if (list == null) return null;
        foreach (var c in list)
            if (c != null && !string.IsNullOrEmpty(c.cardName) && c.cardName.IndexOf(contains, StringComparison.OrdinalIgnoreCase) >= 0)
                return c;
        return null;
    }

    // ---------------------------
    // tiny reflection helpers
    // ---------------------------

    object GetPrivate(object obj, string fieldName)
    {
        if (obj == null) return null;
        var f = obj.GetType().GetField(fieldName, BF);
        if (f == null) { Debug.LogError($"[Reflection] Field not found: {obj.GetType().Name}.{fieldName}"); return null; }
        return f.GetValue(obj);
    }

    void SetPrivate(object obj, string fieldName, object value)
    {
        if (obj == null) return;
        var f = obj.GetType().GetField(fieldName, BF);
        if (f == null) { Debug.LogError($"[Reflection] Field not found: {obj.GetType().Name}.{fieldName}"); return; }
        f.SetValue(obj, value);
    }

    object GetField(object boxedStruct, string fieldName)
    {
        var t = boxedStruct.GetType();
        var f = t.GetField(fieldName, BF);
        if (f == null) return null;
        return f.GetValue(boxedStruct);
    }

    void Assert(bool cond, string msg)
    {
        if (!cond) Debug.LogError("[TEST FAIL] " + msg);
        else Debug.Log("[TEST PASS] " + msg);
    }
}
