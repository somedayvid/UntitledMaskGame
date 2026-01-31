using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Attack,
    Defense,
    Power
}
public enum CardEffect
{
    Null,
    DrunkenFist,
}
//[CreateAssetMenu(fileName = "Card", menuName = "ScriptableObjects/Card", order = 1)]

public class Card
{
    //change this accordingly
    
    public int ID;
    public Mood moodType;
    public string cardName;
    public CardType cardType;
    public CardEffect cardEffect;

    // define your card properties here
    public int damage;
    public int shield;
    public Card()
    {
        damage = 5;
        cardType = CardType.Attack;
    }
    //example I will try something - Ricky
    // can use abstract or virtual. I will use virtual for now
    // see RedMask.cs for example of override (i just made this. change it however you want)
    void resolveEffect()
    {
        switch (cardEffect)
        {
            case (CardEffect.DrunkenFist):
                int rng = Random.Range(0, 2);
                if (rng == 0) damage = 0;
                else damage = 12;
                break;

        }

    }
    public virtual void UseCard()
    {
        resolveEffect();
        Debug.Log($"Using card: {cardName} with ID: {ID}");
        // Implement card effect logic here
    }
}