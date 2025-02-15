using UnityEngine;
using UnityEngine.Tilemaps;

public class ThorneController : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float damageInterval = 0.7f;
    private float damageTimer;
    private bool canDealDamage = true;

    private void Start()
    {
        // Ensure the TilemapCollider2D is set to "Is Trigger"
        TilemapCollider2D tilemapCollider = GetComponent<TilemapCollider2D>();
        if (tilemapCollider != null)
        {
            tilemapCollider.isTrigger = true;
        }
    }

    private void FixedUpdate()
    {
        if (!canDealDamage)
        {
            damageTimer += Time.fixedDeltaTime;
            if (damageTimer >= damageInterval)
            {
                canDealDamage = true;
                damageTimer = 0f;
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && canDealDamage)
        {
            PlayerController player = collision.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damageAmount);
                canDealDamage = false;
                damageTimer = 0f;
            }
        }
    }
}
