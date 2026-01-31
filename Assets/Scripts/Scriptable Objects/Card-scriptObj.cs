using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Card", menuName = "ScriptableObjects/Card", order = 1)]

   

public class Card : ScriptableObject
{
    //change this accordingly
    public enum Mood
    {
        Angry,
        Sad,
        Happy,
        SunWukong,
        JadeEmperor,
    }

    public int ID;
    public Mood moodType;
    public string cardName;

    // define your card properties here
    private int health;
    private int shield;
    private int maxHealth;

    //example I will try something - Ricky
    // can use abstract or virtual. I will use virtual for now
    // see RedMask.cs for example of override (i just made this. change it however you want)
    public virtual void UseCard()
    {
        Debug.Log($"Using card: {name} with ID: {ID}");
        // Implement card effect logic here


        
    }
}
