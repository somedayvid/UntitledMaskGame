using TMPro;
using UnityEngine;

public class ManaDisplayTMP : MonoBehaviour
{
    [SerializeField] private CombatManagerFacade combat;
    [SerializeField] private TextMeshProUGUI manaText;

    private void Awake()
    {
        if (manaText == null)
            manaText = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (combat != null)
            combat.OnManaChanged += HandleManaChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (combat != null)
            combat.OnManaChanged -= HandleManaChanged;
    }

    private void HandleManaChanged(int mana, int cap)
    {
        if (manaText != null)
            manaText.text = $"Mana: {mana}/{cap}";
    }

    private void Refresh()
    {
        if (combat == null || manaText == null) return;
        manaText.text = $"Mana: {combat.Mana}/{combat.ManaCap}";
    }
}
