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

    [Header("Combat")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float attackCooldown = 0.5f;
    private float attackTimer;

    [Header("Input Buffer")]
    [SerializeField] private float inputBufferTime = 0.2f;
    private float jumpBufferCounter;

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
        
        if (isAttachedToCeiling)
        {
            // When on ceiling, only allow X movement
            rb.linearVelocity = new Vector2(moveInput.x * currentSpeed, 0f);
            
            // Explicitly maintain Y position
            Vector3 pos = transform.position;
            pos.y = transform.position.y; // Keep Y position constant
            transform.position = pos;
            
            // Ensure Y movement is constrained
            rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
        }
        else
        {
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.linearVelocity = new Vector2(moveInput.x * currentSpeed, rb.linearVelocity.y);
        }

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
            // Handle running animation for both ground and ceiling
            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                if (isAttachedToCeiling)
                {
                    // Optional: You could create a specific ceiling-run animation
                    wizAnimator.Run();
                    wizAnimator.LookUp(); // Maintain the upward look while running
                }
                else
                {
                    wizAnimator.Run();
                }
            }
            else
            {
                if (isAttachedToCeiling)
                {
                    wizAnimator.LookUp();
                }
                else
                {
                    wizAnimator.Idle();
                }
            }

            if (isJumping && !isAttachedToCeiling)
            {
                wizAnimator.Jump();
            }

            if (isAttacking)
            {
                wizAnimator.Attack();
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
        if (projectilePrefab != null && firePoint != null)
        {
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            
            // Flip projectile direction based on player facing direction
            if (!isFacingRight)
            {
                Vector3 scale = projectile.transform.localScale;
                scale.x *= -1;
                projectile.transform.localScale = scale;
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
        
        // If we're attached to ceiling, check if we should detach
        if (isAttachedToCeiling)
        {
            if (!ceilingHit || !ceilingHit.collider.CompareTag("Ground"))
            {
                DetachFromCeiling();
            }
        }
        // If we're not attached, check if we can attach
        else if (Input.GetKey(attachToCeilingKey) && ceilingHit && ceilingHit.collider.CompareTag("Ground"))
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
            attachPoint.y - (colliderHeight * 0.5f) - 0.01f); // Tiny offset to ensure contact
        transform.position = newPosition;
        
        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;
        
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
        // Restore original constraints
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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

    public void Die()
    {
        if (wizAnimator != null)
        {
            wizAnimator.Die();
        }
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        inputActions.Disable();
        rb.simulated = false;
        
        yield return new WaitForSeconds(1f);
        
        // Use the Transform's position
        transform.position = respawnPoint.position;
        currentHealth = maxHealth;
        transform.rotation = Quaternion.identity;
        
        // Reset states
        isAttachedToCeiling = false;
        isFlipping = false;
        isJumping = false;
        isAttacking = false;
        
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