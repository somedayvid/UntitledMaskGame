using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 Angry - cannot pick target, #% more damage
Sad - Your character frowns.
Happy - Your character smiles
Sun Wukong - Gives you a machine gun
Attack
Defemse
Power
 */

public enum CardAffinity
{
    Angry,
    Sad,
    Happy,
    SunWukong,
}

public class Card : ScriptableObject
{
    public string cardName;
    public CardAffinity cardType;
}


public class Cards : MonoBehaviour
{
    public List<Card> hand = new List<Card>();
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
