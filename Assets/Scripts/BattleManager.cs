using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Step0 BattleManager (fits your current Player/Enemy/DummyEnemy code):
/// - Owns turn flow: Setup -> PlayerTurn -> EnemyTurn -> ...
/// - Player can play unlimited cards during PlayerTurn.
/// - After each successful card play: rotate active mask index (0->1->2->0).
/// - EnemyTurn: each alive enemy acts once via Enemy.ResolveAction(player).
/// - Checks Victory/Defeat and locks the battle when finished.
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

    [Header("Core References")]
    [SerializeField] private Player player;

    [SerializeField] private List<MonoBehaviour> enemyBehaviours = new();

    [Header("Mask UI")]
    [SerializeField] private MaskPanelController maskPanel;

    [Header("Mask Rotation (runtime)")]
    [SerializeField] private MaskData[] equippedMasks = new MaskData[3];

    [SerializeField] private int activeMaskIndex = 0;

    public BattleState State { get; private set; } = BattleState.Setup;
    public int TurnNumber { get; private set; } = 1;

    private readonly List<Enemy> enemies = new();

    private void Awake()
    {
        // Cache Enemy interface references
        enemies.Clear();
        foreach (var mb in enemyBehaviours)
        {
            if (mb is Enemy e) enemies.Add(e);
            else Debug.LogError($"[BattleManager] {mb.name} does not implement Enemy interface.");
        }
    }

    private void Start()
    {
        StartBattle();
    }

    public void StartBattle()
    {
        State = BattleState.Setup;
        TurnNumber = 1;
        activeMaskIndex = 0;

        Debug.Log("=== Battle Start ===");
        Debug.Log($"ActiveMaskIndex={activeMaskIndex} | ActiveMask={GetActiveMaskName()}");

        EnterPlayerTurn();
    }

    private void EnterPlayerTurn()
    {
        if (State == BattleState.Victory || State == BattleState.Defeat) return;

        State = BattleState.PlayerTurn;
        Debug.Log($"--- Player Turn {TurnNumber} --- ActiveMask={GetActiveMaskName()}");

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


        if (target == null)
        {
            target = GetFirstAliveEnemy();
        }
        // Player.PlayCard currently rejects if enemy is null or dead.
        bool success = player.PlayCard(card, target);

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

            // Enemy acts once per enemy turn (Step0)
            e.ResolveAction(player);

            // Check after each enemy action
            CheckVictoryDefeat();
        }

        // Back to player
        TurnNumber++;
        EnterPlayerTurn();
    }

    private void RotateMaskAfterSuccessfulPlay()
    {
        int old = activeMaskIndex;
        activeMaskIndex = (activeMaskIndex + 1) % 3;

        Debug.Log($"[BattleManager] Mask rotate: {old} -> {activeMaskIndex} | ActiveMask={GetActiveMaskName()}");
    }

    private void CheckVictoryDefeat()
    {
        if (player == null) return;

        if (!player.IsAlive())
        {
            State = BattleState.Defeat;
            Debug.Log("=== DEFEAT ===");
            return;
        }

        bool anyEnemyAlive = false;
        foreach (var e in enemies)
        {
            if (e != null && e.IsAlive())
            {
                anyEnemyAlive = true;
                break;
            }
        }

        if (!anyEnemyAlive)
        {
            State = BattleState.Victory;
            Debug.Log("=== VICTORY ===");
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
        if (equippedMasks == null || equippedMasks.Length < 3) return "None";
        var m = equippedMasks[Mathf.Clamp(activeMaskIndex, 0, 2)];
        return m != null ? m.displayName : "None";
    }
}
