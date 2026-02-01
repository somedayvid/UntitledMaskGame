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


    private void Start()
    {
        hpText.text = player.Health.ToString() + '/' + player.MaxHealth.ToString();
    }

    private void LateUpdate()
    {
        if (player != null)
        {
            SetFill(healthFill, player.Health, player.MaxHealth);
            SetFill(shieldFill, player.Shield, player.MaxShield);
            return;
        }

        if (dummy != null)
        {
            SetFill(healthFill, dummy.Health, dummy.MaxHealth);

            if (shieldFill != null)
                shieldFill.gameObject.SetActive(false);

            return;
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
            hpText.text = player.Health.ToString() + '/' + player.MaxHealth.ToString();
        }

    }
}
