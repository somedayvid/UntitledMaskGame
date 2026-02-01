using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    [SerializeField] List<Card> deck;
    List<Card> discard;
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
            deck = discard;
            discard.Clear();
            shuffle();
        }
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
        shuffle();  
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
