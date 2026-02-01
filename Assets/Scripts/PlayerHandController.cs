using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHandController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private DeckManager deckManager;

    [Header("Hand (Inspector-visible)")]
    [SerializeField] private int handLimit = 10;
    [SerializeField] private List<Card> hand = new List<Card>();
    [SerializeField] private List<GameObject> visualHand = new List<GameObject>();

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
    [SerializeField] private GameObject prefab;
    private float bumpDistance = 2.0f;
    private float increasedScale = 1.0f;
    private float normalScale = 0.5f;

    private int startingHandSize = 5;

    public Transform handPosObj;
    public Transform[] handPosList;

    // --------- Public API ---------
    public IReadOnlyList<Card> Hand => hand;
    public IReadOnlyList<GameObject> VisualHand => visualHand;
    public int HandLimit => handLimit;
    public int SelectedIndex => selectedIndex;

    public int HandCount => hand != null ? hand.Count : 0;

    private void Start()
    {
        handPosList = handPosObj.GetComponentsInChildren<Transform>();
        DrawStartingHand();
        ReorderHand();
    }

    public bool AddCardFromPrefab(Card card)
    {
        return AddCardToHand(card); 
    }

    public void DrawStartingHand()
    {
        for(int i = 0; i < startingHandSize; i++)
        {
            DrawToHand();
        }
        ReorderHand();
    }

    public void DrawToHand()
    {
        Card tempCard = deckManager.Draw();
        AddCardFromPrefab(tempCard);
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

    public bool AddCardToHand(Card card)
    {
        if (card == null) return false;

        if (hand.Count >= handLimit)
        {
            Debug.LogWarning("[Hand] Hand is full (limit=10). Card rejected.");
            return false;
        }
        GameObject tempObj = Instantiate(prefab, transform);
        tempObj.GetComponent<SpriteRenderer>().sprite = card.GetImage();
        visualHand.Add(tempObj);
        hand.Add(card);

        selectedIndex = Mathf.Clamp(selectedIndex, 0, hand.Count - 1);
        return true;
    }

    public bool RemoveCardAt(int index)
    {
        if (index < 0 || index >= hand.Count) return false;

        hand.RemoveAt(index);
        GameObject toDestroy = visualHand[index];
        visualHand.RemoveAt(index);
        Destroy(toDestroy);

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
        visualHand[previousSelectedIndex].GetComponent<VisualCard>().SetNewPosition(new Vector2(visualHand[previousSelectedIndex].transform.position.x, visualHand[previousSelectedIndex].transform.position.y - bumpDistance));
        visualHand[selectedIndex].GetComponent<VisualCard>().SetNewPosition(new Vector2(visualHand[selectedIndex].transform.position.x, visualHand[selectedIndex].transform.position.y + bumpDistance));

        visualHand[previousSelectedIndex].transform.localScale = Vector3.one * normalScale;
        visualHand[selectedIndex].transform.localScale = Vector3.one * increasedScale;

        visualHand[selectedIndex].GetComponent<SpriteRenderer>().sortingOrder  = 1;
        visualHand[previousSelectedIndex].GetComponent<SpriteRenderer>().sortingOrder  = 0;

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

        if (success)
        {
            deckManager.DiscardCard(card);
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
        foreach(Transform t in handPosList)
        {
            t.position = handPosObj.transform.position;
        }
        if (visualHand.Count > 0)
        {
            float handWidth = Mathf.Abs(rightHandBound.transform.position.x) + Mathf.Abs(leftHandBound.transform.position.x);

            float perCardSpace = handWidth/visualHand.Count;
            for (int i = 0; i < visualHand.Count; i++)
            {
                handPosList[i].transform.position = new Vector2(leftHandBound.transform.position.x + perCardSpace * i, leftHandBound.transform.position.y);
                visualHand[i].GetComponent<VisualCard>().SetNewPosition(handPosList[i].transform.position);
            }
            selectedIndex = 0;
            previousSelectedIndex = selectedIndex;
            visualHand[selectedIndex].transform.localScale = Vector2.one * increasedScale;
            visualHand[selectedIndex].GetComponent<VisualCard>().SetNewPosition(new Vector2(leftHandBound.transform.position.x, leftHandBound.transform.position.y + bumpDistance));
        }
    }

    public void VisuallyMoveToDiscard()
    {
        for(int i = visualHand.Count - 1; i > 0; i--)
        {
            visualHand[i].GetComponent<VisualCard>().SetNewPosition(new Vector2(30, -10));
        }

        StartCoroutine(RemoveAll());
    }

    private IEnumerator RemoveAll()
    {
        yield return new WaitForSeconds(1);
        for (int i = visualHand.Count - 1; i > 0; i--)
        {
            RemoveCardAt(i);
        }
        DrawStartingHand();
    }

    public void EndOfTurn()
    {
        selectedIndex = -1;
        previousSelectedIndex = -1;
        deckManager.MoveHandToDiscard(hand);
        VisuallyMoveToDiscard();
        selectedIndex = 1;
        previousSelectedIndex = 1;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow)) SelectNext();
        if (Input.GetKeyDown(KeyCode.LeftArrow)) SelectPrev();
        if (Input.GetKeyDown(KeyCode.UpArrow)) PlaySelected();
    }
}
