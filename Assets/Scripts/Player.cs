using System;
using System.Collections.Generic;
using UnityEngine;

public enum Mood
{
    Neutral,
    Angry,
    Sad,
    Happy,
    SunWukong,
    JadeEmperor,
}

public class Player : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int health;
    [SerializeField] private int shield;

    [Header("State")]
    [SerializeField] private Mood mood = Mood.Neutral;

    [Header("Hand")]
    public List<Card> hand = new List<Card>();

    /// <summary>
    /// Fired when a card is successfully played.
    /// </summary>
    public event Action OnCardPlayed;

    private void Awake()
    {
        health = maxHealth;
        shield = 0;
    }

    public bool IsAlive()
    {
        return health > 0;
    }

    private float GetDamageMultiplier()
    {
        switch (mood)
        {
            case Mood.Angry:
                return 1.2f;
            case Mood.Neutral:
            default:
                return 1f;
        }
    }

    private float GetDamageResistMultiplier()
    {
        // If you want "Sad" to change incoming damage, do it here.
        switch (mood)
        {
            case Mood.Sad:
                return 1.2f;
            default:
                return 1f;
        }
    }

    /// <summary>
    /// Plays a card.
    /// - Attack cards require a valid enemy target.
    /// - Defense/Power cards can ignore target (enemy can be null).
    /// </summary>
    public bool PlayCard(Card card, Enemy enemy)
    {
        if (card == null) return false;

        // Let the card resolve its own effect first (CardEffect like DrunkenFist modifies damage).
        card.UseCard();

        // Validate target only for Attack cards
        if (card.cardType == CardType.Attack)
        {
            if (enemy == null || !enemy.IsAlive()) return false;

            int dmg = Mathf.RoundToInt(card.damage * GetDamageMultiplier());
            enemy.TakeDamage(dmg);
        }
        else if (card.cardType == CardType.Defense)
        {
            // Use your Card.cs field (card.shield) instead of hardcoding +5
            AddShield(card.shield);
        }
        else
        {
            // Power: Step0 does nothing yet
        }

        // Remove from hand (Step0 behavior)
        hand.Remove(card);

        // Notify systems (BattleManager rotates masks here)
        OnCardPlayed?.Invoke();

        return true;
    }

    /// <summary>
    /// Fixed damage pipeline: shield absorbs damage first.
    /// </summary>
    public void TakeDamage(int rawDamage)
    {
        if (!IsAlive()) return;
        if (rawDamage <= 0) return;

        int damage = Mathf.RoundToInt(rawDamage * GetDamageResistMultiplier());

        int absorbed = Mathf.Min(shield, damage);
        shield -= absorbed;
        damage -= absorbed;

        if (damage > 0)
        {
            health -= damage;
            if (health < 0) health = 0;
        }

        Debug.Log($"[Player] Took damage. HP={health}, Shield={shield}");
    }

    public void HealPlayer(int heal)
    {
        if (!IsAlive()) return;
        if (heal <= 0) return;

        health = Mathf.Min(health + heal, maxHealth);
        Debug.Log($"[Player] Healed. HP={health}, Shield={shield}");
    }

    public void AddShield(int amount)
    {
        if (!IsAlive()) return;
        if (amount <= 0) return;

        shield += amount;
        Debug.Log($"[Player] Gained shield. HP={health}, Shield={shield}");
    }
}
