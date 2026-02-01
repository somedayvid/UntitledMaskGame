using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class testing : MonoBehaviour
{
    public List<Image> images = new List<Image>();
    public Transform hand;
    public Image prefab;

    private void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            Image tempObj = Instantiate(prefab, hand);
            Transform[] tmep = tempObj.GetComponentsInChildren<Transform>();
            //tempObj.GetComponentsInChildren<Transform>();
            //foreach (Transform t in tmep)
            //{
            //    print(t.name);
            //}
            tmep[1].GetComponent<TextMeshProUGUI>().text = "Supah Buddha Strike";
            tmep[2].GetComponent<TextMeshProUGUI>().text = "3";
            tmep[3].GetComponent<TextMeshProUGUI>().text = "Deal 15 Damage";
            images.Add(tempObj);
        }
    }
}
