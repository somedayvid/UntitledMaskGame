using UnityEngine;

/// <summary>
/// Jam-level button script.
/// Attach to a UI Button.
/// Assign maskPanel GameObject in Inspector.
/// </summary>
public class MaskPanelToggleButton : MonoBehaviour
{
    [SerializeField] private GameObject maskPanel;

    private void Awake()
    {
        if (maskPanel != null)
            maskPanel.SetActive(false); // start closed
    }

    // Hook this to Button.onClick
    public void ToggleMaskPanel()
    {
        if (maskPanel == null)
        {
            Debug.LogError("[MaskPanelToggle] maskPanel is NULL");
            return;
        }

        bool next = !maskPanel.activeSelf;
        maskPanel.SetActive(next);

        Debug.Log($"[MaskPanelToggle] MaskPanel => {(next ? "OPEN" : "CLOSED")}");
    }
}
