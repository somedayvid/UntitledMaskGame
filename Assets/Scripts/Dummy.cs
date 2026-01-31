using UnityEngine;
public class DummyEnemy : MonoBehaviour, Enemy
{
    private int health;
    private int maxHealth = 100;
    private void Awake()
    {
        health = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
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
}
