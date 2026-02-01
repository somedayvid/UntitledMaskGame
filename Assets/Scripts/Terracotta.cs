using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terracotta : MonoBehaviour, Enemy  

{
    int maxHealth = 30;
    int shield = 0;
    int health = 0;
    void TakeDamage(int amount)
    {
        if (shield>0)
        {
            shield -= amount;
            if (shield < 0) amount = shield * -1;
        }
        health = Mathf.Max(health-amount, 0);
    }
    bool IsAlive()
    {
        return health > 0;
    }
    // Start is called before the first frame update
    void Awake()
    {
        health = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ResolveAction(Player player)
    {
        if (player == null) return;
        if (!player.IsAlive()) return;

        int rng = Random.Range(0, 2);
        if (rng == 1) player.TakeDamage(7);
        else shield += 5;
    }

    void Enemy.TakeDamage(int amount)
    {
        TakeDamage(amount);
    }

    bool Enemy.IsAlive()
    {
        return IsAlive();
    }

    void Enemy.PlayAnime()
    {
        gameObject.GetComponent<EnemyParentClass>().PlayAnimation();
    }
}
