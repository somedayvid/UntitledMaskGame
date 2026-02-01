using TMPro;
using UnityEngine;

/// <summary>
/// Dummy enemy for testing.
/// BattleManager should call ResolveAction() during EnemyTurn.
/// </summary>
public class DummyEnemy : MonoBehaviour, Enemy
{
    [SerializeField] private int maxHealth = 100;
    private int health;
    public Player player1;
    //Khaslana'd
    public int Health => health;
    public int MaxHealth => maxHealth;

    // Dummy has no shield yet
    public int Shield => 0;
    public int MaxShield => 1; // avoids divide-by-zero later

    private void Start()
    {
        health = maxHealth;
        ResolveAction(player1);
    }

    public void TakeDamage(int amount)
    {
        if (!IsAlive()) return;
        if (amount <= 0)
        {
            ActionLog.GetInstance().AddText("Your attack missed!");
            return;
        }

        health -= amount;
        if (health < 0) health = 0;

        ActionLog.GetInstance().AddText($"[DummyEnemy] Took {amount} damage.");
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

        //Debug.Log($"[DummyEnemy] Attacks player for {dmg}");
        player.TakeDamage(dmg);
    }

    public void PlayAnime()
    {
        gameObject.GetComponent<EnemyParentClass>().PlayAnimation();
    }
}
