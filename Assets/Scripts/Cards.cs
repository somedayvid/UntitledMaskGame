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

public enum CardType
{
    Attack,
    Defense,
    Power
}

public class Card : ScriptableObject
{
    public string cardName;
    public CardType cardType;
    public int damage = 5;
    public int shield = 5;
}


public class Cards : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
