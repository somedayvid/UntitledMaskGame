using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Enemy
{
    void TakeDamage(int amount);
    bool IsAlive();
    void ResolveAction(Player player);
}

