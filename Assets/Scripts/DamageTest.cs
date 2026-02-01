using UnityEngine;

public class DamageTest : MonoBehaviour
{
    public Player player;

    private void Start()
    {
        player.OnBeforeTakeDamage += (ref Player.DamageContext ctx) =>
        {
            ctx.shieldEfficiency = 0.5f;
        };

        player.AddShield(10);
        player.TakeDamage(10);
    }
}
