using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitBarsUI : MonoBehaviour
{
    [Header("Fill Images (drag from hierarchy)")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image shieldFill; // optional for Dummy


    public Player player;
    public DummyEnemy dummy;

    public TextMeshProUGUI hpText;
    public TextMeshProUGUI shieldText;


    private void Start()
    {
        hpText.text = player.Health.ToString() + '/' + player.MaxHealth.ToString();
    }

    private void LateUpdate()
    {
        if (dummy != null)
        {
            SetFill(healthFill, dummy.Health, dummy.MaxHealth);

            if (shieldFill != null)
                shieldFill.gameObject.SetActive(false);

            if (hpText != null)
                hpText.text = $"{dummy.Health}/{dummy.MaxHealth}";

            if (shieldText != null)
                shieldText.gameObject.SetActive(false);

            return;
        }

            SetFill(healthFill, player.Health, player.MaxHealth);
            SetFill(shieldFill, player.Shield, player.MaxShield);

            if (hpText != null)
                hpText.text = $"{player.Health}/{player.MaxHealth}";

            if (shieldText != null)
            {
                if (player.Shield > 0)
                {
                    shieldText.gameObject.SetActive(true);
                    shieldText.text = $"{player.Shield}";
                }
                else
                {
                    shieldText.gameObject.SetActive(false);
                }
            }


    }


    private void SetFill(Image img, int current, int max)
    {
        if (img == null) return;

        if (max <= 0)
            img.fillAmount = 0f;
        else
        {
            img.fillAmount = Mathf.Clamp01((float)current / max);
        }

    }
}
