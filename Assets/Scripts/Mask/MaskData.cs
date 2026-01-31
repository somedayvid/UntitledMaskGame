using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Masks/Mask Data")]
public class MaskData : ScriptableObject
{
    public string maskId;              // "mask_01"
    public string displayName;         // "Sun Wukong"
    [TextArea] public string shortDesc; 
    public Sprite icon;               
}


