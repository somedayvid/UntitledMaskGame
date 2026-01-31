using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICombatant
{
    int HP { get; }

    bool IsAlive { get; }

    void ApplyDamage(int amount);

    void AddShield(int amount);
}

