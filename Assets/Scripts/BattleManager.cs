using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// BattleManager = turn director (minimal changes):
/// - Still owns turn flow: Setup -> PlayerTurn -> EnemyTurn -> ...
/// - Still owns victory/defeat checks.
/// - Still rotates active mask index after each successful play.
/// 
/// NEW (Step 1 of "2nd tier"):
/// - Delegates combat resolution (mana/cost, mask hooks, deck/discard/exile, status, summon, extra turn request)
///   to CombatManagerFacade.
/// - Enemy still calls Enemy.ResolveAction(player) (Enemy.cs unchanged).
/// - After each enemy action, we call combat.AfterEnemyAction(enemy) so Ski bleed etc can trigger.
/// </summary>
public class BattleManager : MonoBehaviour
{
    public enum BattleState
    {
        Setup,
        PlayerTurn,
        EnemyTurn,
        Victory,
        Defeat
    }
    private bool battleHadAnyEnemy = false;


    [Header("Core References")]
    [SerializeField] private Player player;

    [Header("Enemies (Inspector-visible)")]
    [SerializeField] private List<MonoBehaviour> enemyBehaviours = new();

    [Header("Mask Manager")]
    [SerializeField] private MaskManager maskManager;


    [Header("Mask UI")]
    [SerializeField] private MaskPanelController maskPanel;

    [Header("Mask Rotation (runtime)")]
    [SerializeField] private MaskData[] equippedMasks = new MaskData[3];
    [SerializeField] private int activeMaskIndex = 0;

    public CardGen cardGen;
    
    [Header("Combat Facade (NEW)")]
    [Tooltip("Handles mana/cost, deck/discard/exile, mask hooks, status, summons, extra turn requests.")]
    [SerializeField] private CombatManagerFacade combat;

    public BattleState State { get; private set; } = BattleState.Setup;
    public int TurnNumber { get; private set; } = 1;

    private readonly List<Enemy> enemies = new();

    private static BattleManager instance;

    public static BattleManager GetInstance()
    {
        return instance;
    }
    
        // Cache Enemy interface references
    // NEW: extra turn is requested by CombatManagerFacade (e.g., Er Lang Shen card)
    private bool extraTurnPending = false;

    // Debug stuff

    private void Awake()
    {

        instance = this;
        // Cache Enemy interface references (Enemy.cs unchanged)
        enemies.Clear();
        foreach (var mb in enemyBehaviours)
        {
            if (mb is Enemy e) enemies.Add(e);
            else Debug.LogError($"[BattleManager] {mb.name} does not implement Enemy interface.");
        }
    }

    private IEnumerator Start()
    {
        Debug.Log($"[BattleManager] Cached enemies: {enemies.Count}");

        // Bind facade once at startup
        if (combat != null)
        {
            combat.Bind(player, enemies);
            combat.OnRequestExtraTurn += () => extraTurnPending = true;
        }
        yield return null;
        StartBattle();
    }

    public void StartBattle()
    {
        State = BattleState.Setup;
        TurnNumber = 1;
        activeMaskIndex = 0;
        battleHadAnyEnemy = false;


        Debug.Log("=== Battle Start ===");
        Debug.Log($"ActiveMaskIndex={activeMaskIndex} | ActiveMask={GetActiveMaskName()}");

        // NEW: facade initializes external deck/discard/exile and draws starting hand, etc.
        if (combat != null)
            combat.OnBattleStart(GetEquippedMasksSnapshot(), GetActiveIndex());


        EnterPlayerTurn();
    }

    private void EnterPlayerTurn()
    {
        if (State == BattleState.Victory || State == BattleState.Defeat) return;

        State = BattleState.PlayerTurn;

        // NEW: facade handles turn-start triggers (refill mana, draw, ZhongKui negative cards, etc.)
        if (combat != null)
            combat.OnPlayerTurnStart(GetEquippedMasksSnapshot(), GetActiveIndex(), TurnNumber);


        Debug.Log($"--- Player Turn {TurnNumber} --- ActiveMask={GetActiveMaskName()}");
        ActionLog.GetInstance().AddText($"Turn {TurnNumber}");

        CheckVictoryDefeat();
    }

    /// <summary>
    /// UI calls this when the player wants to play a card.
    /// to add: if no target selected, target the first enemy
    /// </summary>
    public bool TryPlayCard(Card card, Enemy target)
    {
        if (State != BattleState.PlayerTurn) return false;
        if (player == null) return false;
        if (card == null) return false;
        if(target == null)
        {
            target = GetFirstAliveEnemy();
        }
        

        bool success;

        // NEW: delegate play logic to facade
        // (If combat is missing, fallback to old behavior.)

        if (combat != null)
            success = combat.TryPlayCard(GetEquippedMasksSnapshot(), GetActiveIndex(), card, target);

        else
            success = player.PlayCard(card, target);

        if (!success) return false;

        // Successful play => rotate mask
        RotateMaskAfterSuccessfulPlay();

        // After each play, check victory/defeat
        CheckVictoryDefeat();

        return true;
    }

    /// <summary>
    /// Convenience: play the card at a hand index against the first alive enemy.
    /// </summary>
    public void TryPlayCardAtHandIndex(int handIndex)
    {
        if (State != BattleState.PlayerTurn) return;
        if (player == null) return;

        if (handIndex < 0 || handIndex >= player.hand.Count) return;

        Enemy target = GetFirstAliveEnemy();
        if (target == null)
        {
            CheckVictoryDefeat();
            return;
        }

        Card card = player.hand[handIndex];
        TryPlayCard(card, target);
    }

    /// <summary>
    /// UI calls this to end the player turn.
    /// </summary>
    public void EndPlayerTurn()
    {
        if (State != BattleState.PlayerTurn) return;

        // NEW: facade handles end-of-turn triggers (Kitsune foxies act, etc.)
        if (combat != null)
            combat.OnPlayerTurnEnd(GetEquippedMasksSnapshot(), GetActiveIndex(), TurnNumber);


        // NEW: handle extra turn request (skip EnemyTurn if requested)
        if (extraTurnPending)
        {
            extraTurnPending = false;
            Debug.Log("[BattleManager] Extra turn granted! Staying in PlayerTurn.");
            EnterPlayerTurn();
            return;
        }

        player.GetComponent<PlayerHandController>().EndOfTurn();


        Debug.Log("--- End Player Turn -> Enemy Turn ---");
        State = BattleState.EnemyTurn;

        RunEnemyTurn();
    }

    private void RunEnemyTurn()
    {
        if (player == null) return;

        foreach (var e in enemies)
        {
            if (State == BattleState.Victory || State == BattleState.Defeat) return;

            if (e == null) continue;
            if (!e.IsAlive()) continue;

            e.PlayAnime();

            // Enemy acts once per enemy turn (Step0)
            // Enemy acts once per enemy turn (Enemy.cs unchanged)
            e.ResolveAction(player);

            // NEW: after enemy action, facade can trigger "after enemy moves" effects (e.g., Ski bleed ticks)
            if (combat != null)
                combat.AfterEnemyAction(e);

            // Check after each enemy action
            CheckVictoryDefeat();
        }
        TurnNumber++;
        EnterPlayerTurn();
    }
    private void RotateMaskAfterSuccessfulPlay()
    {
        if (maskManager == null) return;
        int old = maskManager.ActiveIndex;
        maskManager.RotateActive();
        Debug.Log($"[BattleManager] Mask rotate: {old} -> {maskManager.ActiveIndex}");
    }


    private void CheckVictoryDefeat()
    {
        if (player == null) return;

        if (!player.IsAlive())
        {
            State = BattleState.Defeat;
            GameManager.instance.GameOver();
            Debug.Log("=== DEFEAT ===");
            return;
        }

        bool anyEnemyExists = false;
        bool anyEnemyAlive = false;

        foreach (var e in enemies)
        {
            if (e == null) continue;

            anyEnemyExists = true;
            if (e.IsAlive())
            {
                anyEnemyAlive = true;
                break;
            }
        }

        if (anyEnemyExists) battleHadAnyEnemy = true;

        if (battleHadAnyEnemy && !anyEnemyAlive)
        {
            cardGen.KardGen(3);
            State = BattleState.Victory;
            GameManager.instance.LevelWon();
            ActionLog.GetInstance().AddText("=== VICTORY ===");
        }
    }


    private Enemy GetFirstAliveEnemy()
    {
        foreach (var e in enemies)
        {
            Debug.Log("[BattleManager] First enemy found!");
            if (e != null && e.IsAlive()) return e;
        }
        return null;
    }
    private string GetActiveMaskName()
    {
        if (maskManager != null)
        {
            var m = maskManager.GetActiveMask();
            return m != null ? $"{m.displayName} ({m.maskId})" : "None";
        }

        if (equippedMasks == null || equippedMasks.Length < 3) return "None";
        var mm = equippedMasks[Mathf.Clamp(activeMaskIndex, 0, 2)];
        return mm != null ? mm.displayName : "None";
    }

    private MaskData[] GetEquippedMasksSnapshot()
    {
        return new MaskData[]
        {
        maskManager != null ? maskManager.GetQueueMask(0) : null,
        maskManager != null ? maskManager.GetQueueMask(1) : null,
        maskManager != null ? maskManager.GetQueueMask(2) : null,
        };
    }

    private int GetActiveIndex()
    {
        return maskManager != null ? maskManager.ActiveIndex : 0;
    }

}
