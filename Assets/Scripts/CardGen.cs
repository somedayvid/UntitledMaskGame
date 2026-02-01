using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardGen : MonoBehaviour
{
    public List<Card> generatedCards = new List<Card>();
    public DeckManager deckManager;
    public GameObject cardGenPanel;
    [SerializeField] private List<Image> imgList;

    public void KardGen(int count) 
    {
        cardGenPanel.SetActive(true);
        generatedCards.Clear();

        for (int i = 0; i < count; i++)
        {
            Card card = new Card();
            CardEffect randomEffect = (CardEffect)Random.Range(
                1,
                System.Enum.GetValues(typeof(CardEffect)).Length - 1
            );

            card.cardEffect = randomEffect;
            card.cardName = card.GetName();
            generatedCards.Add(card);
            Debug.Log($"Generated card {i + 1}: {card.cardName}");
        }
        imgList[0].sprite = generatedCards[0].GetImage();
        imgList[1].sprite = generatedCards[1].GetImage();
        imgList[2].sprite = generatedCards[2].GetImage();
    }

    public void SelectThisCard(int index)
    {
        deckManager.AddCardMain(generatedCards[index]);
        cardGenPanel.SetActive(false);
        Debug.Log($"{generatedCards[index].cardName} added to main deck");
    }
}
