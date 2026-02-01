using System.Collections.Generic;
using UnityEngine;

public class CardGen : MonoBehaviour
{
    public List<Card> generatedCards = new List<Card>();
    void Start()
    {
        KardGen(3); 
    }
    public void KardGen(int count)
    {
        generatedCards.Clear();

        for (int i = 0; i < count; i++)
        {
            Card card = new Card();
            CardEffect randomEffect = (CardEffect)Random.Range(
                1,
                System.Enum.GetValues(typeof(CardEffect)).Length
            );

            card.cardEffect = randomEffect;
            card.cardName = card.GetName();
            generatedCards.Add(card);
            Debug.Log($"Generated card {i + 1}: {card.cardName}");
        }
    }
}
