using UnityEngine;

/// <summary>
/// Dummy enemy for testing.
/// BattleManager should call ResolveAction() during EnemyTurn.
/// </summary>
public class DummyEnemy : MonoBehaviour, Enemy
{
    [SerializeField] private int maxHealth = 100;
    private int health;

    private void Awake()
    {
        health = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive()) return;
        if (amount <= 0) return;

        health -= amount;
        if (health < 0) health = 0;

        Debug.Log($"[DummyEnemy] Took {amount} damage. HP={health}");
    }

    public void ResetDummy()
    {
        health = maxHealth;
    }

    public bool IsAlive()
    {
        return health > 0;
    }

    /// <summary>
    /// Random behavior test code.
    /// Called ONCE per enemy turn by BattleManager.
    /// </summary>
    public void ResolveAction(Player player)
    {
        if (player == null) return;
        if (!player.IsAlive()) return;

        int rng = Random.Range(0, 500);
        int dmg = 1;
        if (rng == 0) dmg = 1000; // keep your rare big hit test

        Debug.Log($"[DummyEnemy] Attacks player for {dmg}");
        player.TakeDamage(dmg);
    }

    private void Update()
    {
        // Intentionally empty. Do NOT call ResolveAction() here.
        // Turn-based control must come from BattleManager.
    }
}
