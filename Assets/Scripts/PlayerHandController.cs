using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHandController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleManager battleManager;

    [Header("Hand (Inspector-visible)")]
    [SerializeField] private int handLimit = 10;
    [SerializeField] private List<Card> hand = new List<Card>();
    [SerializeField] private List<Image> visualHand = new List<Image>();

    [Header("Selection")]
    [SerializeField] private int selectedIndex = 0;
    [SerializeField] private int previousSelectedIndex = 0;

    [Header("Play Settings")]
    [SerializeField] private bool useDefaultTarget = true;
    [SerializeField] private MonoBehaviour explicitTargetEnemy;

    [Header("Hand Positioning")]
    [SerializeField] private GameObject leftHandBound;
    [SerializeField] private GameObject rightHandBound;
    [SerializeField] private GameObject handPivotPoint;
    [SerializeField] private Transform handTrans;
    [SerializeField] private Image prefab;
    [SerializeField] private float bumpDistance = 15.0f;

    // --------- Public API ---------
    public IReadOnlyList<Card> Hand => hand;
    public IReadOnlyList<Image> VisualHand => visualHand;
    public int HandLimit => handLimit;
    public int SelectedIndex => selectedIndex;

    public int HandCount => hand != null ? hand.Count : 0;

    private void Start()
    {
        for(int i = 0; i < 3; i++)
        {
            AddCardFromPrefab();
        }
        ReorderHand();
    }

    public bool AddCardFromPrefab()
    {
        //if (cardPrefab == null)
        //{
        //    Debug.LogWarning("[Hand] AddCardFromPrefab got null prefab.");
        //    return false;
        //}

        //var info = cardPrefab.GetComponent<CardPrefabInfo>();
        //if (info == null)
        //{
        //    Debug.LogError($"[Hand] Prefab '{cardPrefab.name}' missing CardPrefabInfo component.");
        //    return false;
        //}

        //Card created = info.CreateCardInstance();

        Image cardImg = Instantiate(prefab, handTrans);
        Transform[] tmep = cardImg.GetComponentsInChildren<Transform>();
        tmep[1].GetComponent<TextMeshProUGUI>().text = "Palm Strike";
        tmep[2].GetComponent<TextMeshProUGUI>().text = "1";
        tmep[3].GetComponent<TextMeshProUGUI>().text = "Deal 5 Damage";
        Card tempCard = new Card();

        return AddCardToHand(tempCard, cardImg); 
    }

    public bool RemoveFirstCard()
    {
        if (hand.Count == 0) return false;
        return RemoveCardAt(0); 
    }

    public Card GetSelectedCard()
    {
        if (hand.Count == 0) return null;
        selectedIndex = Mathf.Clamp(selectedIndex, 0, hand.Count - 1);
        return hand[selectedIndex];
    }

    public bool AddCardToHand(Card card, Image cardImg)
    {
        if (card == null) return false;

        if (hand.Count >= handLimit)
        {
            Debug.LogWarning("[Hand] Hand is full (limit=10). Card rejected.");
            return false;
        }

        visualHand.Add(Instantiate(prefab, transform));
        hand.Add(card);

        selectedIndex = Mathf.Clamp(selectedIndex, 0, hand.Count - 1);
        return true;
    }

    public bool RemoveCardAt(int index)
    {
        if (index < 0 || index >= hand.Count) return false;

        hand.RemoveAt(index);
        visualHand.RemoveAt(index);

        if (hand.Count == 0)
        {
            selectedIndex = 0;
        }
        else
        {
            selectedIndex = Mathf.Clamp(selectedIndex, 0, hand.Count - 1);
        }

        return true;
    }

    private void Selection()
    {
        Transform cur = handTrans.GetChild(selectedIndex);
        Transform pre = handTrans.GetChild(previousSelectedIndex);
        pre.position = new Vector2(pre.position.x, pre.position.y - bumpDistance);
        cur.position = new Vector2(cur.position.x, cur.position.y + bumpDistance);
        previousSelectedIndex = selectedIndex;
    }

    public void SelectNext()
    {
        if (hand.Count == 0) return;
        selectedIndex = (selectedIndex + 1) % hand.Count;
        Selection();
    }

    public void SelectPrev()
    {
        if (hand.Count == 0) return;
        selectedIndex--;
        if (selectedIndex < 0) selectedIndex = hand.Count - 1;
        Selection();
    }

    public void PlaySelected()
    {
        if (battleManager == null)
        {
            Debug.LogError("[Hand] battleManager is null.");
            return;
        }

        Card card = GetSelectedCard();
        if (card == null)
        {
            Debug.LogWarning("[Hand] No selected card to play.");
            return;
        }

        Enemy target = null;

        if (!useDefaultTarget)
        {
            if (explicitTargetEnemy is Enemy e) target = e;
            else target = null;
        }

        bool success = battleManager.TryPlayCard(card, target);
        Debug.Log("[Hand]" + card  + " to " + target);

        if (success)
        {
            RemoveCardAt(selectedIndex);
            ReorderHand();
        }
        else
        {
            Debug.Log("[Hand] Play failed (turn state / target / rules). Card not removed.");
        }
    }

    public void ReorderHand()
    {
        if (hand.Count > 0)
        {
            float handWidth = Mathf.Abs(rightHandBound.transform.position.x) + Mathf.Abs(leftHandBound.transform.position.x);
            //Vector2 handCenter = new Vector2(0, leftHandBound.transform.position.y);

            float negSwitch = 1.0f;

            for (int i = 0; i < handTrans.childCount; i++)
            {
                handTrans.GetChild(i).transform.position = Camera.main.WorldToScreenPoint(new Vector2(leftHandBound.transform.position.x + handWidth/hand.Count * i, leftHandBound.transform.position.y));
                //negSwitch*=-1;
            }
            selectedIndex = 0;
            previousSelectedIndex = selectedIndex;
            Transform temp = handTrans.GetChild(selectedIndex);
            temp.position = new Vector2(temp.position.x, temp.position.y + bumpDistance);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) SelectNext();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) SelectPrev();
        if (Input.GetKeyDown(KeyCode.Return)) PlaySelected();
    }
}
