using UnityEngine;
using UnityEngine.InputSystem;
using ClearSky;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Input Actions")]
    private PlayerInput playerInput;
    private InputSystem_Actions inputActions;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction attackAction;
    private InputAction interactAction;
    private InputAction sprintAction;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    private Vector2 moveInput;
    private Vector2 cachedVelocity;

    [Header("Ceiling Walking")]
    [SerializeField] private float ceilingCheckDistance = 1f;
    [SerializeField] private float ceilingDetachThreshold = 0.1f;
    [SerializeField] private KeyCode attachToCeilingKey = KeyCode.LeftShift;
    [SerializeField] private float flipDuration = 0.5f;
    private bool isAttachedToCeiling;
    private bool isFlipping;
    private float flipProgress;
    private Quaternion startRotation;
    private Quaternion targetRotation;

    [Header("Health & Respawn")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private Transform respawnPoint;
    private int currentHealth;
    public int MaxHealth => maxHealth;

    [Header("States")]
    private bool isJumping;
    private bool isGrounded;
    private bool isAttacking;
    private bool isInteracting;
    private bool isSprinting;
    private bool isFacingRight = true;

    [Header("Components")]
    private Rigidbody2D rb;
    private Animator animator;
    private WizDemo1 wizAnimator;
    private DamageFlash damageFlash;

    [Header("Combat")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown = 0.5f;
    private float attackTimer;

    [Header("Combat")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;
    private bool isKnockedBack;

    [Header("Ammo")]
    [SerializeField] private int currentAmmo = 0;
    [SerializeField] private float pickupRadius = 1f;
    [SerializeField] private LayerMask stalactiteLayer;
    private AmmoUI ammoUI;

    [Header("Input Buffer")]
    [SerializeField] private float inputBufferTime = 0.2f;
    private float jumpBufferCounter;

    [Header("Reset References")]
    [SerializeField] private EnemyPatrol[] enemyArray;
    [SerializeField] private Transform[] ammoSpawnPoints;
    [SerializeField] private GameObject ammoPrefab;

    private Vector2 lastDamageSourcePosition;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        wizAnimator = GetComponent<WizDemo1>();
        playerInput = GetComponent<PlayerInput>();
        
        inputActions = new InputSystem_Actions();  
        moveAction = inputActions.Player.Move;
        jumpAction = inputActions.Player.Jump;
        attackAction = inputActions.Player.Attack;
        interactAction = inputActions.Player.Interact;
        sprintAction = inputActions.Player.Sprint;
        
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        damageFlash = GetComponent<DamageFlash>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        if (respawnPoint == null)
        {
            // Create a new respawn point GameObject if none is assigned
            GameObject spawnPoint = new GameObject("RespawnPoint");
            spawnPoint.transform.position = transform.position;
            respawnPoint = spawnPoint.transform;
        }

        ammoUI = FindObjectOfType<AmmoUI>();
        if (ammoUI != null)
        {
            ammoUI.UpdateAmmoText(currentAmmo);
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
        moveAction.performed += OnMove;
        moveAction.canceled += OnMove;
        jumpAction.performed += OnJump;
        jumpAction.canceled += OnJump;
        attackAction.performed += OnAttack;
        attackAction.canceled += OnAttackCanceled;
        interactAction.performed += OnInteract;
        interactAction.canceled += OnInteractCanceled;
        sprintAction.performed += OnSprint;
        sprintAction.canceled += OnSprintCanceled;
    }

    private void OnDisable()
    {
        inputActions.Disable();
        moveAction.performed -= OnMove;
        moveAction.canceled -= OnMove;
        jumpAction.performed -= OnJump;
        jumpAction.canceled -= OnJump;
        attackAction.performed -= OnAttack;
        attackAction.canceled -= OnAttackCanceled;
        interactAction.performed -= OnInteract;
        interactAction.canceled -= OnInteractCanceled;
        sprintAction.performed -= OnSprint;
        sprintAction.canceled -= OnSprintCanceled;
    }

    private void FixedUpdate()
    {
        CheckGrounded();
        CheckCeilingAttachment();  // Add this line
        HandleMovement();
        HandleJumpBuffer();
        UpdateAnimator();
        CheckAmmoPickup();
    }

    private void Update()
    {
        if (attackTimer > 0)
        {
            attackTimer -= Time.deltaTime;
        }
        // ...existing code...
    }

    private void CheckGrounded()
    {
        bool wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        Debug.DrawLine(transform.position, groundCheck.position, isGrounded ? Color.green : Color.red);
        
        // Only reset jumping if we've actually landed
        if (isGrounded && !wasGrounded)
        {
            isJumping = false;
            Debug.Log("Landed on ground");
        }
    }

    private void HandleMovement()
    {
        float currentSpeed = isSprinting ? sprintSpeed : moveSpeed;
        
        // Simplified movement code - no need to handle ceiling separately
        rb.linearVelocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);

        // Handle facing direction
        if (moveInput.x != 0)
        {
            bool shouldFaceRight = moveInput.x > 0;
            if (shouldFaceRight != isFacingRight)
            {
                Flip();
            }
        }
    }

    private void HandleJumpBuffer()
    {
        if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
            if (isGrounded && !isJumping) // Added !isJumping check
            {
                rb.linearVelocity = Vector2.up * jumpForce;
                jumpBufferCounter = 0;
                isJumping = true;
                Debug.Log("Buffer jump executed");
            }
        }
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void UpdateAnimator()
    {
        if (wizAnimator != null)
        {
            // Add a small threshold to prevent jittery transitions
            float moveThreshold = 0.1f;
            bool isMoving = Mathf.Abs(moveInput.x) > moveThreshold;

            if (isAttachedToCeiling)
            {
                if (isMoving)
                {
                    wizAnimator.Run();
                }
                else
                {
                    wizAnimator.Idle();
                }
            }
            else
            {
                if (isMoving)
                {
                    wizAnimator.Run();
                }
                else
                {
                    wizAnimator.Idle();
                }
            }
        }
    }

    #region Input Callbacks
    private void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        Debug.Log($"Move Input: {moveInput}"); // Add this debug line
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log($"Jump performed. IsGrounded: {isGrounded}, IsAttachedToCeiling: {isAttachedToCeiling}");
            
            if (isAttachedToCeiling)
            {
                DetachFromCeiling();
                rb.linearVelocity = Vector2.down * jumpForce;
                isJumping = true;
                Debug.Log("Jumping from ceiling");
            }
            else if (isGrounded)
            {
                jumpBufferCounter = inputBufferTime;
                isJumping = true;
                rb.linearVelocity = Vector2.up * jumpForce;
                Debug.Log($"Jumping from ground with force: {jumpForce}");
            }
        }
        else if (context.canceled && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
            Debug.Log("Jump canceled - cutting velocity");
        }
    }

    private void OnAttack(InputAction.CallbackContext context)
    {
        if (context.performed && attackTimer <= 0)
        {
            isAttacking = true;
            ShootProjectile();
            attackTimer = attackCooldown;
        }
    }

    private void ShootProjectile()
    {
        if (projectilePrefab != null && firePoint != null && currentAmmo > 0)
        {
            currentAmmo--;
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile projectileComponent = projectile.GetComponent<Projectile>();
            
            if (projectileComponent != null)
            {
                // Tell the projectile which direction to move
                projectileComponent.Initialize(isFacingRight);
            }

            // Update UI
            if (ammoUI != null)
            {
                ammoUI.UpdateAmmoText(currentAmmo);
            }
        }
    }

    private void OnAttackCanceled(InputAction.CallbackContext context)
    {
        isAttacking = false;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isInteracting = true;
        }
    }

    private void OnInteractCanceled(InputAction.CallbackContext context)
    {
        isInteracting = false;
    }

    private void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            isSprinting = true;
        }
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        isSprinting = false;
    }
    #endregion

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isJumping = false;
            if (wizAnimator != null)
            {
                wizAnimator.Idle();
            }
        }
    }

    private void CheckCeilingAttachment()
    {
        RaycastHit2D ceilingHit = Physics2D.Raycast(transform.position, Vector2.up, ceilingCheckDistance, groundLayer);
        Debug.DrawRay(transform.position, Vector2.up * ceilingCheckDistance, Color.yellow); // Debug visualization
        
        // If we're attached to ceiling, check if we should detach
        if (isAttachedToCeiling)
        {
            if (!ceilingHit)
            {
                DetachFromCeiling();
            }
        }
        // If we're not attached, check if we can attach
        else if (Input.GetKey(attachToCeilingKey) && ceilingHit)
        {
            AttachToCeiling(ceilingHit.point);
        }
    }

    private void AttachToCeiling(Vector2 attachPoint)
    {
        if (isFlipping) return;
        
        isAttachedToCeiling = true;
        
        // More precise position calculation
        Collider2D collider = GetComponent<Collider2D>();
        float colliderHeight = collider != null ? collider.bounds.size.y : 1f;
        Vector2 newPosition = new Vector2(transform.position.x, 
            attachPoint.y - (colliderHeight * 0.5f));
        transform.position = newPosition;
        
        rb.linearVelocity = Vector2.zero;
        
        // Use GravityController instead of directly modifying gravity
        GravityController.Instance.InvertGravity();
        
        StartCoroutine(FlipToCeiling());
    }

    private IEnumerator FlipToCeiling()
    {
        isFlipping = true;
        flipProgress = 0f;
        startRotation = transform.rotation;
        targetRotation = Quaternion.Euler(0, 0, 180f);
        Vector3 startPosition = transform.position;
        
        rb.simulated = false;
        
        while (flipProgress < 1f)
        {
            flipProgress += Time.deltaTime / flipDuration;
            float smoothProgress = Mathf.SmoothStep(0, 1, flipProgress);
            
            // Rotation
            float currentAngle = Mathf.LerpAngle(0, 180, smoothProgress);
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
            
            // Improved arc movement
            float arcHeight = Mathf.Sin(smoothProgress * Mathf.PI) * 0.3f; // Reduced arc height
            Vector3 newPosition = startPosition;
            newPosition.y += arcHeight;
            transform.position = newPosition;
            
            yield return null;
        }
        
        // Snap to exact ceiling position
        transform.position = startPosition;
        transform.rotation = targetRotation;
        rb.simulated = true;
        isFlipping = false;
    }

    private void DetachFromCeiling()
    {
        if (isFlipping) return;
        
        isAttachedToCeiling = false;
        
        // Use GravityController to restore normal gravity
        GravityController.Instance.InvertGravity();
        
        StartCoroutine(FlipFromCeiling());
    }

    private IEnumerator FlipFromCeiling()
    {
        isFlipping = true;
        flipProgress = 0f;
        startRotation = transform.rotation;
        targetRotation = Quaternion.identity;
        
        rb.simulated = false;
        
        while (flipProgress < 1f)
        {
            flipProgress += Time.deltaTime / flipDuration;
            
            float smoothProgress = Mathf.SmoothStep(0, 1, flipProgress);
            float currentAngle = Mathf.LerpAngle(180, 0, smoothProgress);
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
            
            // Add a small downward arc during the flip
            float arcHeight = Mathf.Sin(smoothProgress * Mathf.PI) * 0.5f;
            Vector3 currentPos = transform.position;
            currentPos.y -= arcHeight * Time.deltaTime;
            transform.position = currentPos;
            
            yield return null;
        }
        
        transform.rotation = targetRotation;
        rb.simulated = true;
        isFlipping = false;
    }

    public void TakeDamage()
    {
        currentHealth--;
    
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (wizAnimator != null)
        {
            wizAnimator.Hurt();
        }
    }

    public void TakeDamage(int damage)
    {
        if (isFlipping || isKnockedBack) return;

        currentHealth -= damage;
        
        if (damageFlash != null)
        {
            damageFlash.Flash();
        }

        // Notify health UI
        HealthUI healthUI = FindObjectOfType<HealthUI>();
        if (healthUI != null)
        {
            healthUI.UpdateHearts(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Apply knockback
        StartCoroutine(ApplyKnockback());

        if (wizAnimator != null)
        {
            wizAnimator.Hurt();
        }
    }

    public void TakeDamage(int damage, Vector2 damageSourcePosition)
    {
        if (isFlipping || isKnockedBack) return;

        lastDamageSourcePosition = damageSourcePosition;
        currentHealth -= damage;
        
        if (damageFlash != null)
        {
            damageFlash.Flash();
        }

        // Notify health UI
        HealthUI healthUI = FindObjectOfType<HealthUI>();
        if (healthUI != null)
        {
            healthUI.UpdateHearts(currentHealth);
        }

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // Apply knockback based on damage source position
        StartCoroutine(ApplyKnockback());

        if (wizAnimator != null)
        {
            wizAnimator.Hurt();
        }
    }

    private IEnumerator ApplyKnockback()
    {
        isKnockedBack = true;
        
        // If player is on ceiling, detach and reset gravity first
        if (isAttachedToCeiling)
        {
            isAttachedToCeiling = false;
            GravityController.Instance.ResetGravity();
            transform.rotation = Quaternion.identity;
        }
        
        // Calculate knockback direction based on damage source
        Vector2 knockbackDirection = (transform.position - (Vector3)lastDamageSourcePosition).normalized;
        
        // Apply the knockback force with upward boost
        float knockbackStrength = knockbackForce * 5f;
        rb.linearVelocity = Vector2.zero; // Reset current velocity
        rb.AddForce(new Vector2(knockbackDirection.x * knockbackStrength, 3f), ForceMode2D.Impulse);
        
        // Briefly disable player input
        inputActions.Disable();
        
        yield return new WaitForSeconds(knockbackDuration);
        
        // Re-enable input and reset state
        inputActions.Enable();
        isKnockedBack = false;
    }

    public void Die()
    {
        if (wizAnimator != null)
        {
            wizAnimator.Die();
        }
        GravityController.Instance.ResetGravity();
        transform.rotation = Quaternion.identity; // Reset rotation
        isAttachedToCeiling = false;
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        inputActions.Disable();
        rb.simulated = false;
        
        yield return new WaitForSeconds(1f);
        
        // Reset player
        transform.position = respawnPoint.position;
        currentHealth = maxHealth;
        transform.rotation = Quaternion.identity;
        currentAmmo = 0;
        
        // Reset states
        isAttachedToCeiling = false;
        isFlipping = false;
        isJumping = false;
        isAttacking = false;
        
        // Reset enemies
        foreach (EnemyPatrol enemy in enemyArray)
        {
            if (enemy != null)
            {
                enemy.ResetEnemy();
            }
        }
        
        // Reset stalactites using LayerMask
        Collider2D[] stalactites = Physics2D.OverlapCircleAll(Vector2.zero, float.MaxValue, stalactiteLayer);
        foreach (Collider2D stalactite in stalactites)
        {
            Destroy(stalactite.gameObject);
        }
        
        // Spawn new stalactites at designated points
        foreach (Transform spawnPoint in ammoSpawnPoints)
        {
            if (spawnPoint != null)
            {
                Instantiate(ammoPrefab, spawnPoint.position, Quaternion.identity);
            }
        }
        
        // Update UI
        HealthUI healthUI = FindObjectOfType<HealthUI>();
        if (healthUI != null)
        {
            healthUI.UpdateHearts(currentHealth);
        }
        if (ammoUI != null)
        {
            ammoUI.UpdateAmmoText(currentAmmo);
        }
        
        // Re-enable player
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;
        inputActions.Enable();
        
        if (wizAnimator != null)
        {
            wizAnimator.Idle();
        }
    }

    // Optional: Add method to set new respawn point (for checkpoints)
    public void SetRespawnPoint(Transform newRespawnPoint)
    {
        respawnPoint = newRespawnPoint;
    }

    private void CheckAmmoPickup()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, pickupRadius, stalactiteLayer);
        foreach (Collider2D collider in hitColliders)
        {
            // Add ammo
            currentAmmo++;
            
            // Update UI
            if (ammoUI != null)
            {
                ammoUI.UpdateAmmoText(currentAmmo);
            }
            
            // Destroy pickup
            Destroy(collider.gameObject);
        }
    }

    private void OnDrawGizmos()
    {
        if (groundCheck != null)
        {
            // Draw ground check radius
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}