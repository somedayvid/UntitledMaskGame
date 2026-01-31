using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
public enum Mood
{
    Angry,
    Sad,
    Happy,
    SunWukong,
    JadeEmperor,
}
public class Player : MonoBehaviour
{
    private int health;
    private int shield;
    // setting base value? 
    private int maxHealth = 100;
    private Mood mood;
    public List<Card> hand = new List<Card>();
    public DummyEnemy dumbass;
    private float GetDamageMultiplier()
    {
        switch (mood)
        {
            case Mood.Angry:
                return 1.2f; 
            default:
                return 1f;
        }
    }
    public bool isAlive()
    {
        return health >= 0;
    }
    public bool PlayCard(Card card, Enemy enemy)
    {
        if (card == null || enemy == null || !enemy.IsAlive()) return false;

        switch (card.cardType)
        {
            case CardType.Attack:
                int dmg = Mathf.RoundToInt(card.damage * GetDamageMultiplier());
                enemy.TakeDamage(dmg);
                break;
            case CardType.Defense:
                shield += 5;
                break;
            case CardType.Power:
                break;
        }

        hand.Remove(card);
        return true;
    }
    private float GetDamageResist()
    {
        switch (mood)
        {
            case Mood.Sad:
                return 1.2f;
            default:
                return 1f;
        }
    }
    // should sadness make you  take less damage or just give you more shield when you gain shield?
    public void TakeDamage(int damage)
    {
        Debug.Log("hp:" + health);
        if(damage<=shield) shield-=damage;
        else
        {
            shield = 0;
            damage -= shield;
            health -= damage;
        }
        Debug.Log("Player has taken " + damage + "Damage");
        if(health<=0) Debug.Log("Player has died.");
    }
    public void HealPlayer(int heal)
    {
        //prevent overheals
        health = Mathf.Min(health+heal, maxHealth);
    }
    public void AddShield(int _shield)
    {
        shield += _shield;
    }
    // Start is called before the first frame update
    void Awake()
    {
        health = maxHealth;
        Debug.Log(isAlive());
        Card temp = new Card();
        DummyEnemy e = dumbass;
        shield = 0;
        PlayCard(temp,e);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
