using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;


//
public class Deck : MonoBehaviour
{
    public event EventHandler OnDeckChanged;

    [SerializeField] private Dictionary<int, Card> deck;

    public static Deck instance;
        
    private void Awake()
    {
        if (instance != this && instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    public void Initialize()
    {
        if (deck != null)
        {
            deck.Clear();
        }

        deck = new();

        
    }

    public void AddCard(Card cardToAdd, int amount)
    {
        //if (cardsList.TryGetValue(cardToAdd.ID, out Card card))
        //{
        //    // Card already exists, you can implement logic to increase quantity if needed

        //}
        //else
        //{
        //    cardsList.Add(cardToAdd.ID, cardToAdd);
        //}

        // technically, the check above is redundant since we are adding the same type of cards with unique IDS
        deck.Add(cardToAdd.ID, cardToAdd);

        // make sure to do: backpack.OnBackpackChanged += backpack_OnBackpackChanged; in the class that needs to listen to changes
        RaiseBackpackChanges();
    }

    public void RemoveCard(Card cardToRemove)
    {
        if (deck.ContainsKey(cardToRemove.ID))
        {
            deck.Remove(cardToRemove.ID);
            // make sure to do: backpack.OnBackpackChanged += backpack_OnBackpackChanged; in the class that needs to listen to changes
            RaiseBackpackChanges();
        }
    }

    public static Deck GetInstance()
    {
        return instance;
    }

    public Dictionary<int, Card> GetCardsList()
    {
        return deck;
    }

    private void RaiseBackpackChanges()
    {
        OnDeckChanged?.Invoke(this, EventArgs.Empty);
    }


}
