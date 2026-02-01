using UnityEngine;

public class CardPrefabInfo : MonoBehaviour
{
    [Header("Card Template (copied into hand as a new Card instance)")]
    public Card template = new Card();

    public Card CreateCardInstance()
    {
        return new Card
        {
            ID = template.ID,
            moodType = template.moodType,
            cardName = template.cardName,
            cardType = template.cardType,
            cardEffect = template.cardEffect,
            damage = template.damage,
            shield = template.shield
        };
    }
}
