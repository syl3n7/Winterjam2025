using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float patrolSpeed = 3f;
    [SerializeField] private float chaseSpeed = 7f;
    [SerializeField] private bool startAtPointA = true;

    [Header("Detection")]
    [SerializeField] private float detectionRadius = 5f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float waitTimeAfterChase = 3f;
    [SerializeField] private float accelerationTime = 0.5f;

    [Header("Combat")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int damageAmount = 1;
    [SerializeField] private float damageInterval = 0.7f;
    private int currentHealth;
    private float damageTimer;
    private bool canDealDamage = true;

    private Vector3 currentTarget;
    private Rigidbody2D rb;
    private bool isFacingRight = true;
    private Transform player;
    private float currentSpeed;
    private float waitTimer;
    private bool isWaitingAfterChase;
    private Vector3 lastKnownPlayerPosition;
    private EnemyState currentState = EnemyState.Patrolling;

    private DamageFlash damageFlash;

    private enum EnemyState
    {
        Patrolling,
        Chasing,
        WaitingAtLastSeen
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        currentTarget = startAtPointA ? pointB.position : pointA.position;
        currentSpeed = patrolSpeed;
        currentHealth = maxHealth;

        if (pointA == null || pointB == null)
        {
            Debug.LogError($"Patrol points not set on {gameObject.name}");
            enabled = false;
        }

        damageFlash = GetComponent<DamageFlash>();
    }

    private void Update()
    {
        // Check for player in detection radius
        Collider2D playerCollider = Physics2D.OverlapCircle(transform.position, detectionRadius, playerLayer);

        switch (currentState)
        {
            case EnemyState.Patrolling:
                if (playerCollider != null)
                {
                    currentState = EnemyState.Chasing;
                    player = playerCollider.transform;
                    StartCoroutine(AccelerateToChaseSpeed());
                }
                else
                {
                    PatrolBehavior();
                }
                break;

            case EnemyState.Chasing:
                if (playerCollider != null)
                {
                    lastKnownPlayerPosition = player.position;
                    ChasePlayer();
                }
                else
                {
                    currentState = EnemyState.WaitingAtLastSeen;
                    waitTimer = waitTimeAfterChase;
                }
                break;

            case EnemyState.WaitingAtLastSeen:
                if (playerCollider != null)
                {
                    currentState = EnemyState.Chasing;
                    player = playerCollider.transform;
                }
                else
                {
                    waitTimer -= Time.deltaTime;
                    if (waitTimer <= 0)
                    {
                        currentState = EnemyState.Patrolling;
                        StartCoroutine(DecelerateToPatrolSpeed());
                    }
                }
                break;
        }

        UpdateFacing();
    }

    private void PatrolBehavior()
    {
        Vector2 direction = (currentTarget - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * currentSpeed, rb.linearVelocity.y);

        if (Vector2.Distance(new Vector2(transform.position.x, 0),
                           new Vector2(currentTarget.x, 0)) < 0.1f)
        {
            currentTarget = currentTarget == pointA.position ? pointB.position : pointA.position;
        }
    }

    private void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        rb.linearVelocity = new Vector2(direction.x * currentSpeed, rb.linearVelocity.y);
    }

    private void UpdateFacing()
    {
        if (rb.linearVelocity.x > 0 && !isFacingRight)
            Flip();
        else if (rb.linearVelocity.x < 0 && isFacingRight)
            Flip();
    }

    private System.Collections.IEnumerator AccelerateToChaseSpeed()
    {
        float[] speedSteps = { 3f, 5f, 5.5f, 6f, 7f };
        float stepDuration = accelerationTime / (speedSteps.Length - 1);
        
        for (int i = 0; i < speedSteps.Length - 1; i++)
        {
            float startStepSpeed = speedSteps[i];
            float targetStepSpeed = speedSteps[i + 1];
            float elapsedTime = 0f;
            
            while (elapsedTime < stepDuration)
            {
                elapsedTime += Time.deltaTime;
                currentSpeed = Mathf.Lerp(startStepSpeed, targetStepSpeed, elapsedTime / stepDuration);
                yield return null;
            }
        }
        
        currentSpeed = chaseSpeed;
    }

    private System.Collections.IEnumerator DecelerateToPatrolSpeed()
    {
        float[] speedSteps = { 7f, 6f, 5.5f, 5f, 3f };
        float stepDuration = accelerationTime / (speedSteps.Length - 1);
        
        for (int i = 0; i < speedSteps.Length - 1; i++)
        {
            float startStepSpeed = speedSteps[i];
            float targetStepSpeed = speedSteps[i + 1];
            float elapsedTime = 0f;
            
            while (elapsedTime < stepDuration)
            {
                elapsedTime += Time.deltaTime;
                currentSpeed = Mathf.Lerp(startStepSpeed, targetStepSpeed, elapsedTime / stepDuration);
                yield return null;
            }
        }
        
        currentSpeed = patrolSpeed;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        
        if (damageFlash != null)
        {
            damageFlash.Flash();
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Add death animation here if you have one
        Destroy(gameObject);
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

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && canDealDamage)
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damageAmount);
                canDealDamage = false;
                damageTimer = 0f;
            }
        }
    }
}