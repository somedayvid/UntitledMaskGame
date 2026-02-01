using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
    Attack,
    Defense,
    Power,
    Heal,
    NegativeType
}
public enum CardEffect
{
    Null,
    DrunkenFist,
    PalmStrike,
    SmallShieldPotion,
    ShieldPotion,
    OrientalMedicineJug,
    SubmachineGun,
    OrientalDaggerRitual,
    OrientalDagger,
    Meditate,
    OrientalTigerBalm,
    GinsengRoot,
    HeavenlyInsight,
    MandateOfHeaven,
    SunTzusInsight,
    DragonStrike,
    HeavenSplit,
    JadeBarrier,
    Momentum,
    RockThrow,
    Pills,
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
    public int chi;
    // define your card properties here
    public int damage;
    public int shield;
    public Card()
    {
        damage = 5;
        chi = 1;
        cardType = CardType.Attack;
    }
    //example I will try something - Ricky
    // can use abstract or virtual. I will use virtual for now
    // see RedMask.cs for example of override (i just made this. change it however you want)
    void ResolveEffect()
    {
        switch (cardEffect)
        {
            case (CardEffect.DrunkenFist):
                int rng = Random.Range(0, 2);
                if (rng == 0) damage = 0;
                else damage = 12;
                break;
            case (CardEffect.PalmStrike):
                damage = 5;
                break;
            case (CardEffect.SmallShieldPotion):
                chi = 1;
                shield = 5;
                cardType = CardType.Defense;
                break;
            case (CardEffect.ShieldPotion):
                chi = 2;
                shield = 25;
                cardType = CardType.Defense;
                break;
            case (CardEffect.OrientalMedicineJug):
                chi = 3;
                shield = 50;
                cardType = CardType.Defense;
                break;
            case (CardEffect.OrientalDaggerRitual):
                break;
            case (CardEffect.OrientalDagger):
                chi = 0;
                damage = 3;
                break;
            case (CardEffect.Meditate):
                break;
            case (CardEffect.OrientalTigerBalm):
                break;
            case (CardEffect.GinsengRoot):
                break;
            case (CardEffect.HeavenlyInsight):
                break;
            case (CardEffect.MandateOfHeaven):
                break;
            case (CardEffect.SunTzusInsight):
                break;
            case (CardEffect.DragonStrike):
                damage = 5;
                break;
            case (CardEffect.HeavenSplit):
                damage = 5;
                break;
            case (CardEffect.JadeBarrier):
                break;
            case (CardEffect.Momentum):
                break;
            case (CardEffect.RockThrow):
                damage = 3;
                chi = 0;
                break;
            case (CardEffect.Pills):
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