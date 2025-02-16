using UnityEngine;
using System.Collections;

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
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private float knockbackDuration = 0.2f;
    private int currentHealth;
    private float damageTimer;
    private bool canDealDamage = true;
    private bool isKnockedBack;

    [Header("Ranged Attack")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float projectileGrowDuration = 0.9f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    private bool isCharging;
    private GameObject currentProjectile;

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
        WaitingAtLastSeen,
        ChargingAttack
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

            case EnemyState.ChargingAttack:
                if (playerCollider == null)
                {
                    if (!isCharging)
                    {
                        currentState = EnemyState.WaitingAtLastSeen;
                        waitTimer = waitTimeAfterChase;
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
        if (Vector2.Distance(transform.position, player.position) <= attackRange)
        {
            currentState = EnemyState.ChargingAttack;
            StartCoroutine(ChargeAndShootProjectile());
        }
        else
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(direction.x * currentSpeed, rb.linearVelocity.y);
        }
    }

    private IEnumerator ChargeAndShootProjectile()
    {
        isCharging = true;
        rb.linearVelocity = Vector2.zero;

        // Create and grow projectile
        currentProjectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        currentProjectile.transform.localScale = Vector3.one * 0.2374319f;

        float elapsedTime = 0f;
        Vector3 targetScale = Vector3.one * 0.45f;
        Vector3 initialScale = currentProjectile.transform.localScale;

        while (elapsedTime < projectileGrowDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / projectileGrowDuration;
            currentProjectile.transform.localScale = Vector3.Lerp(initialScale, targetScale, progress);
            
            // Double detection radius while charging
            detectionRadius *= 2f;
            
            // Update position to follow firePoint
            currentProjectile.transform.position = firePoint.position;
            
            yield return null;
        }

        // Shoot projectile
        if (currentProjectile != null)
        {
            EnemyProjectile projectileComponent = currentProjectile.GetComponent<EnemyProjectile>();
            if (projectileComponent != null)
            {
                // Reverse the direction to match the enemy's reversed facing logic
                bool projectileDirection = !isFacingRight;
                projectileComponent.Initialize(projectileDirection);
            }
        }

        // Reset state
        isCharging = false;
        detectionRadius /= 2f; // Return to normal detection radius
        currentState = EnemyState.WaitingAtLastSeen;
        waitTimer = waitTimeAfterChase;
    }

    private void UpdateFacing()
    {
        Vector3 targetPosition;
        switch (currentState)
        {
            case EnemyState.Patrolling:
                targetPosition = currentTarget;
                break;
            case EnemyState.Chasing:
                targetPosition = player.position;
                break;
            case EnemyState.WaitingAtLastSeen:
                targetPosition = lastKnownPlayerPosition;
                break;
            default:
                return;
        }

        // Reversed the logic here (added the '!')
        bool shouldFaceRight = !(targetPosition.x > transform.position.x);
        
        if (shouldFaceRight != isFacingRight)
        {
            Flip();
        }
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
        if (isKnockedBack) return;
        
        currentHealth -= damage;
        
        if (damageFlash != null)
        {
            damageFlash.Flash();
        }
        
        // Apply knockback
        StartCoroutine(ApplyKnockback());
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator ApplyKnockback()
    {
        isKnockedBack = true;
        
        // Store current state
        EnemyState previousState = currentState;
        currentState = EnemyState.WaitingAtLastSeen;
        
        // Calculate knockback direction (opposite of current facing direction)
        float direction = isFacingRight ? -1f : 1f;
        
        // Apply knockback force
        rb.linearVelocity = new Vector2(direction * knockbackForce, rb.linearVelocity.y + 1f);
        
        yield return new WaitForSeconds(knockbackDuration);
        
        // Reset state
        isKnockedBack = false;
        if (previousState != EnemyState.WaitingAtLastSeen)
        {
            currentState = previousState;
        }
    }

    private void Die()
    {
        // Add death animation here if you have one
        Destroy(gameObject);
    }

    public void ResetEnemy()
    {
        // Reset position
        transform.position = startAtPointA ? pointA.position : pointB.position;
        
        // Reset state
        currentState = EnemyState.Patrolling;
        currentHealth = maxHealth;
        isCharging = false;
        isKnockedBack = false;
        currentSpeed = patrolSpeed;
        waitTimer = 0f;
        damageTimer = 0f;
        canDealDamage = true;
        
        // Reset target and player reference
        currentTarget = startAtPointA ? pointB.position : pointA.position;
        player = null;
        lastKnownPlayerPosition = transform.position;
        
        // Reset detection radius to normal
        detectionRadius = detectionRadius / 2f; // Make sure it's at normal radius
        
        // Reset any active projectiles
        if (currentProjectile != null)
        {
            Destroy(currentProjectile);
            currentProjectile = null;
        }
        
        // Reset velocity
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
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
                // Pass the enemy's position as damage source
                player.TakeDamage(damageAmount, transform.position);
                canDealDamage = false;
                damageTimer = 0f;
            }
        }
    }
}