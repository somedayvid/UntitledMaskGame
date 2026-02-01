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

    public struct DamageContext
    {
        public int incomingDamage;
        public bool cancelDamage;
        public float shieldEfficiency;
        public object source;
    }

    public delegate void BeforeTakeDamageHandler(ref DamageContext ctx);
    public event BeforeTakeDamageHandler OnBeforeTakeDamage;

    public event System.Action<int> OnAfterTakeDamage;

    public struct ShieldContext
    {
        public int amount;     
        public object source; 
    }
    public delegate void BeforeGainShieldHandler(ref ShieldContext ctx);
    public event BeforeGainShieldHandler OnBeforeGainShield;



    [Header("Stats")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int health;
    [SerializeField] private int shield;
    [SerializeField] private int strength;
    //Khaslana'd
    public int Health => health;
    public int MaxHealth => maxHealth;

    public int Shield => shield;
    public int MaxShield => Mathf.Max(1, maxHealth);

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
        strength = 0;
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

            int dmg = Mathf.RoundToInt((strength+card.damage) * GetDamageMultiplier());
            enemy.TakeDamage(dmg);
        }
        else if (card.cardType == CardType.Defense)
        {
            AddShield(card.shield);
        }
        else
        {

        }

        hand.Remove(card);

        OnCardPlayed?.Invoke();

        return true;
    }

    public void TakeDamage(int rawDamage)
    {
        if (!IsAlive()) return;
        if (rawDamage <= 0) return;

        var ctx = new DamageContext
        {
            incomingDamage = rawDamage,
            cancelDamage = false,
            shieldEfficiency = 1f,
            source = null
        };

        OnBeforeTakeDamage?.Invoke(ref ctx);

        if (ctx.cancelDamage)
        {
            OnAfterTakeDamage?.Invoke(0);
            return;
        }

        int damage = ctx.incomingDamage;
        if (damage <= 0) { OnAfterTakeDamage?.Invoke(0); return; }

        // ������ �������ֻ������ԭ���ġ��۶ܡ����һ��Ч�ʲ���
        int shieldAbsorbCap = Mathf.FloorToInt(shield * ctx.shieldEfficiency);
        int absorbed = Mathf.Min(shieldAbsorbCap, damage);

        // ��Ҫ����ʵ���ĵ�shield�����ȥ
        int shieldSpent = (ctx.shieldEfficiency <= 0f) ? 0 : Mathf.CeilToInt(absorbed / ctx.shieldEfficiency);
        shield = Mathf.Max(0, shield - shieldSpent);

        damage -= absorbed;

        int hpLoss = 0;
        if (damage > 0)
        {
            hpLoss = damage;
            health -= damage;
            if (health < 0) health = 0;
        }

        ActionLog.GetInstance().AddText($"[Player] Took damage. HP={health}, Shield={shield}");
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

        var ctx = new ShieldContext
        {
            amount = amount,
            source = null
        };

        OnBeforeGainShield?.Invoke(ref ctx);

        int final = ctx.amount;
        if (final <= 0) return;

        shield += final;
        Debug.Log($"[Player] Gained shield +{final}. HP={health}, Shield={shield}");
    }

}
