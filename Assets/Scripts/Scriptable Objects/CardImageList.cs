using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardImageList : MonoBehaviour
{
    public List<Sprite> spriteList;

    public static CardImageList instance;
    public static CardImageList GetInstance()
    {
        return instance;
    }

    private void Awake()
    {
        instance = this;
    }

    public Sprite Img(int index)
    {
        return spriteList[index];
    }
}
