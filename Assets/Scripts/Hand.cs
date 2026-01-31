using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Hand : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject rightHandBound;
    public GameObject leftHandBound;
    public GameObject handRotationPivot;

    private List<Transform> cardsInHand;

    void Start()
    {
        cardsInHand = new List<Transform>();
        foreach(Transform child in transform)
        {
            cardsInHand.Add(child);
        }

        ReorderHand();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    /// <summary>
    /// 
    /// </summary>
    public void ReorderHand()
    {
        float handWidth = Mathf.Abs(rightHandBound.transform.position.x) + Mathf.Abs(leftHandBound.transform.position.x);
        Vector2 handCenter = new Vector2(0, leftHandBound.transform.position.y);

        float negSwitch = 1.0f;

        for(int i = 0; i < cardsInHand.Count; i++)
        {
            cardsInHand[i].transform.position = new Vector3(handCenter.x + i/2 * negSwitch, leftHandBound.transform.position.y);
            negSwitch*=-1;
        }
    }
}
