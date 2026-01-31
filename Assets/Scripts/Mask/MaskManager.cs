using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Mesh;

public class MaskManager : MonoBehaviour
{
    public List<MaskData> unlockedMasks;
    public MaskData equippedMask;

    public void EquipMask(MaskData newMask)
    {
        equippedMask = newMask;
        // ֪ͨ Combat / UI
    }
}

