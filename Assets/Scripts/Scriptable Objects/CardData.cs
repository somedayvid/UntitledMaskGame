using UnityEngine;


[CreateAssetMenu(fileName = "CardData", menuName = "ScriptableObjects/CardData", order = 1)]
public class CardData : ScriptableObject
{
    public int ID;
    public Mood moodType;
    public string cardName;
    public CardType cardType;
    public CardEffect cardEffect;

    public int damage = 5;
    public int shield = 0;
}
