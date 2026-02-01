using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField] private List<Card> mainDeck = new List<Card>();
    [SerializeField] private List<Card> tempDeck = new List<Card>();
    private List<Card> discardDeck = new List<Card>();

    public TextMeshProUGUI discardPileCount;
    public TextMeshProUGUI combatDeckCount;

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
            tempDeck = new List<Card>(discardDeck);
            discardDeck.Clear();
            discardPileCount.text = discardDeck.Count.ToString();
            combatDeckCount.text = tempDeck.Count.ToString();
            shuffle();
        }
        if (tempDeck.Count == 0) return null;
        Card card = tempDeck[0];
        tempDeck.RemoveAt(0);
        combatDeckCount.text = tempDeck.Count.ToString();
        return card;
    }
    public void AddCard(Card card)
    {
        tempDeck.Add(card);
        shuffle();
        combatDeckCount.text = tempDeck.Count.ToString();
    }

    public void RemoveCard(Card card)
    {
        tempDeck.Remove(card);
        combatDeckCount.text = tempDeck.Count.ToString();
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

    public void DiscardCard(Card card)
    {
        discardDeck.Add(card);
        discardPileCount.text = discardDeck.Count.ToString();
    }

    public void MoveHandToDiscard(List<Card> playerHand)
    {
        discardDeck.AddRange(playerHand);
        discardPileCount.text = discardDeck.Count.ToString();
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
        AddCard(newCard2);


        shuffle();
    }
}
