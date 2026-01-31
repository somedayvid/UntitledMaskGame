using UnityEngine;
using UnityEngine.UI;

public class MaskPanelController : MonoBehaviour
{
    [Header("Data")]
    public MaskData[] allMasks = new MaskData[8];

    private MaskData[] queue = new MaskData[3];
    private MaskData[] inventory = new MaskData[5];

    [Header("UI - Queue Slots")]
    public Image[] queueSlotImages = new Image[3];

    [Header("UI - Inventory Slots")]
    public Image[] inventorySlotImages = new Image[5];

    void Start()
    {
        InitMasks();
        RefreshUI();
    }

    void InitMasks()
    {
        for (int i = 0; i < 3; i++) queue[i] = allMasks[i];
        for (int i = 0; i < 5; i++) inventory[i] = allMasks[i + 3];
    }

    public void OnClickInventory(int index)
    {
        Debug.Log("Clicked inventory slot: " + index);
        if (index < 0 || index >= inventory.Length) return;

        // mask clicked
        MaskData clicked = inventory[index];

        // kick last
        MaskData kicked = queue[2];

        // move left
        queue[2] = queue[1];
        queue[1] = queue[0];
        queue[0] = clicked;

        // one got kicked to inventory
        inventory[index] = kicked;

        RefreshUI();
    }

    void RefreshUI()
    {
        for (int i = 0; i < 3; i++)
        {
            queueSlotImages[i].sprite = queue[i].icon;
            queueSlotImages[i].enabled = true;
        }

        for (int i = 0; i < 5; i++)
        {
            inventorySlotImages[i].sprite = inventory[i].icon;
            inventorySlotImages[i].enabled = true;
        }
    }
}
