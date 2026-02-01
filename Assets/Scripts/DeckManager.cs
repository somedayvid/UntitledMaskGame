using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField] List<Card> deck = new List<Card>();
    List<Card> discard = new List<Card>();
    void shuffle()
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = deck[i];
            deck[i] = deck[j];
            deck[j] = temp;
        }
    }
    public Card Draw()
    {
        if (deck.Count == 0)
        {
            deck = new List<Card>(discard);
            discard.Clear();
            shuffle();
        }
        if (deck.Count == 0) return null;
        Card card = deck[0];
        deck.RemoveAt(0);
        discard.Add(card);
        return card;
    }
    public void AddCard(Card card)
    {
        deck.Add(card);
        shuffle();
    }
    void Start()
    {
        Card newCard = new Card();
        newCard.cardEffect = CardEffect.DrunkenFist;
        AddCard(newCard);
        Card newCard1 = new Card();
        newCard1.cardEffect = CardEffect.Momentum;
        AddCard(newCard1);
        Card newCard2 = new Card();
        newCard2.cardEffect = CardEffect.RockThrow;
        AddCard(newCard2);
        shuffle();
        Debug.Log(Draw().GetName());
        Debug.Log(Draw().GetName());
        Debug.Log(Draw().GetName());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
