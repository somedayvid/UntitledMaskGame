using UnityEngine;
public class DummyEnemy : MonoBehaviour, Enemy
{
    private int health;
    private int maxHealth = 100;
    public Player p1;
    private void Awake()
    {
        health = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log("Enemy has taken: " + amount + "damage");
        //Just set hp to zero if it will go negative
        if (health < 0) health = 0;

    }
    public void ResetDummy()
    {
        health = maxHealth;
    }
    public bool IsAlive()
    {
        return health > 0;
    }
    public void ResolveAction(Player player)
    {
        //Debug.Log("ello");
        //Debug.Log(player.isAlive());
        if (!player.isAlive()) return;
        int rng = Random.Range(0, 500);
        int dmg = 1;
        if (rng == 0) dmg = 1000;
        player.TakeDamage(dmg);
    }
    public void Update()
    {
        ResolveAction(p1);
    }
}
