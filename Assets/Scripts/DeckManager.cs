using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private List<Card> mainDeck = new List<Card>();
    [SerializeField] private List<Card> tempDeck = new List<Card>();
    private List<Card> discardDeck = new List<Card>();

    List<Card> discard = new List<Card>();
    void shuffle()
    {
        for (int i = tempDeck.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = tempDeck[i];
            tempDeck[i] = tempDeck[j];
            tempDeck[j] = temp;
        }
    }
    public Card Draw()
    {
        if (tempDeck.Count == 0)
        {
            tempDeck = new List<Card>(discard);
            discard.Clear();
            shuffle();
        }
        if (tempDeck.Count == 0) return null;
        Card card = tempDeck[0];
        tempDeck.RemoveAt(0);
        return card;
    }
    public void AddCard(Card card)
    {
        tempDeck.Add(card);
        shuffle();
    }

    public void RemoveCard(Card card)
    {
        tempDeck.Remove(card);
    }

    public void StartBattle()
    {
        tempDeck = new List<Card>(mainDeck);
    }

    public void EndBattle()
    {
        tempDeck = null;
        discardDeck.Clear();
    }

    void Awake()
    {
        Card newCard = new Card();
        newCard.cardEffect = CardEffect.DrunkenFist;
        AddCard(newCard);
        AddCard(newCard);
        AddCard(newCard);
        AddCard(newCard);
        Card newCard2 = new Card();
        newCard2.cardEffect = CardEffect.RockThrow;
        AddCard(newCard2);
        AddCard(newCard2);
        AddCard(newCard2);
        AddCard(newCard2);
        AddCard(newCard2);


        shuffle();
    }
}
