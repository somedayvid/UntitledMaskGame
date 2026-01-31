using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
public enum Mood
{
    Neutral,
    Angry,
    Sad,
    Happy,
    SunWukong,
    JadeEmperor,
}
public class Player : MonoBehaviour
{
    private int health;
    private int shield;
    // setting base value? 
    private int maxHealth = 100;
    private Mood mood;
    public List<Card> hand = new List<Card>();
    public List<GameObject> visualHand = new List<GameObject>();
    public List<Card> deck = new List<Card>();
    public DummyEnemy dumbass;

    public Hand handFunc;

    public int chi;
    public int maxChi;

    public int currentHandIndex;
    public int previousHandIndex;

    public GameObject rightHandBound;
    public GameObject leftHandBound;

    public GameObject cardPrefab;

    void Start()
    {
        health = maxHealth;
        CreateCard(new Card());
        CreateCard(new Card());
        CreateCard(new Card());
        CreateCard(new Card());
        //DealHand();
        //Card temp = new Card();
        //DummyEnemy e = dumbass;
        //PlayCard(temp, e);

        ReorderHand();
        currentHandIndex = hand.Count/2;
        previousHandIndex = currentHandIndex;
    }

    private void Update()
    {
        PlayerInput();
    }

    private float GetDamageMultiplier()
    {
        switch (mood)
        {
            case Mood.Angry:
                return 1.2f; 
            case Mood.Neutral:
                return 1f;
            default:
                return 1f;
        }
    }

    public void PlayerInput()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            if (hand.Count > 0)
            {
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    currentHandIndex++;
                    if (currentHandIndex > hand.Count - 1)
                    {
                        currentHandIndex = 0;
                    }
                }


                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    currentHandIndex--;
                    if (currentHandIndex < 0)
                    {
                        currentHandIndex = hand.Count - 1;
                    }
                }

                visualHand[currentHandIndex].transform.position = new Vector2(visualHand[currentHandIndex].transform.position.x, visualHand[currentHandIndex].transform.position.y + 1);
                visualHand[previousHandIndex].transform.position = new Vector2(visualHand[previousHandIndex].transform.position.x, visualHand[previousHandIndex].transform.position.y - 1);
                previousHandIndex = currentHandIndex;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Return) && hand.Count > 0)
        {
            PlayCard(hand[currentHandIndex], dumbass);
        }
    }

    public bool isAlive()
    {
        return health >= 0;
    }
    public bool PlayCard(Card card, Enemy enemy)
    {
        if (card == null || enemy == null || !enemy.IsAlive()) return false;

        switch (card.cardType)
        {
            case CardType.Attack:
                print(card.damage);
                print(GetDamageMultiplier());
                int dmg = Mathf.RoundToInt(card.damage * GetDamageMultiplier());
                enemy.TakeDamage(dmg);
                break;
            case CardType.Defense:
                shield += 5;
                break;
            case CardType.Power:
                break;
        }

        hand.RemoveAt(currentHandIndex);
        GameObject toDelete = visualHand[currentHandIndex];
        visualHand.RemoveAt(currentHandIndex);
        Destroy(toDelete);
        ReorderHand();
        return true;
    }
    private float GetDamageResist()
    {
        switch (mood)
        {
            case Mood.Sad:
                return 1.2f;
            default:
                return 1f;
        }
    }
    // should sadness make you  take less damage or just give you more shield when you gain shield?
    public void TakeDamage(int damage)
    {
        Debug.Log("hp:" + health);
        if(damage<=shield) shield-=damage;
        else
        {
            shield = 0;
            damage -= shield;
            health -= damage;
        }
        Debug.Log("Player has taken " + damage + "Damage");
        if(health<=0) Debug.Log("Player has died.");
    }
    public void HealPlayer(int heal)
    {
        //prevent overheals
        health = Mathf.Min(health+heal, maxHealth);
    }
    public void AddShield(int _shield)
    {
        shield += _shield;
    }

    public void CreateCard(Card cardObj)
    {
        visualHand.Add(Instantiate(cardPrefab, transform));
        hand.Add(cardObj);
    }

    // Start is called before the first frame update
    void Awake()
    {
        health = maxHealth;
        Debug.Log(isAlive());
        Card temp = new Card();
        shield = 0;
    }

    public void ReorderHand()
    {
        if (hand.Count > 0)
        {
            float handWidth = Mathf.Abs(rightHandBound.transform.position.x) + Mathf.Abs(leftHandBound.transform.position.x);
            Vector2 handCenter = new Vector2(0, leftHandBound.transform.position.y);

            float negSwitch = 1.0f;

            for (int i = 0; i < hand.Count; i++)
            {
                visualHand[i].transform.position = new Vector3(leftHandBound.transform.position.x + handWidth/hand.Count * i, leftHandBound.transform.position.y);
                negSwitch*=-1;
            }
            currentHandIndex = hand.Count/2;
            previousHandIndex = currentHandIndex;
            visualHand[currentHandIndex].transform.position = new Vector2(visualHand[currentHandIndex].transform.position.x, visualHand[currentHandIndex].transform.position.y + 1);
        }
    }
}
