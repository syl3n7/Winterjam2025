using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float moveSpeed = 3f; 
    [SerializeField] private bool startAtPointA = true;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private LayerMask playerLayer;

    private Vector3 currentTarget;
    private Rigidbody2D rb;
    private bool isFacingRight = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        currentTarget = startAtPointA ? pointB.position : pointA.position;

        if (pointA == null || pointB == null)
        {
            Debug.LogError($"Patrol points not set on {gameObject.name}");
            enabled = false;
        }
    }

    private void Update()
    {
        // Check for player in detection radius
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);
        if (playerCollider != null)
        {
            // Player detected - handle accordingly
            // You can add chase behavior here later
            return;
        }

        // Move towards current target
        Vector2 direction = (currentTarget - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);

        // Update facing direction
        if (direction.x > 0 && !isFacingRight)
            Flip();
        else if (direction.x < 0 && isFacingRight)
            Flip();

        // Check if reached target
        if (Vector2.Distance(new Vector2(transform.position.x, 0), 
                           new Vector2(currentTarget.x, 0)) < 0.1f)
        {
            currentTarget = currentTarget == pointA.position ? pointB.position : pointA.position;
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnDrawGizmos()
    {
        if (pointA != null && pointB != null)
        {
            // Draw patrol path
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pointA.position, 0.3f);
            Gizmos.DrawWireSphere(pointB.position, 0.3f);
            Gizmos.DrawLine(pointA.position, pointB.position);

            // Draw detection radius
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
    }
}