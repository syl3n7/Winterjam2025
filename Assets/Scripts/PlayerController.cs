using UnityEngine;
using UnityEngine.InputSystem;
using ClearSky;

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
    private bool isAttachedToCeiling;

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
        cachedVelocity.x = moveInput.x * currentSpeed;
        
        if (isAttachedToCeiling)
        {
            cachedVelocity.y = 0f;
        }
        else
        {
            cachedVelocity.y = rb.linearVelocity.y;
        }
        
        rb.linearVelocity = cachedVelocity;

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
            if (Mathf.Abs(moveInput.x) > 0.1f)
            {
                wizAnimator.Run();
            }
            else
            {
                wizAnimator.Idle();
            }

            if (isJumping)
            {
                wizAnimator.Jump();
            }

            if (isAttacking)
            {
                wizAnimator.Attack();
            }

            // Optional: Add ceiling check animation
            if (isAttachedToCeiling)
            {
                wizAnimator.LookUp();
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
        if (context.performed)
        {
            isAttacking = true;
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
        isAttachedToCeiling = true;
        Vector2 newPosition = new Vector2(transform.position.x, attachPoint.y - ceilingDetachThreshold);
        transform.position = newPosition;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
    }

    private void DetachFromCeiling()
    {
        isAttachedToCeiling = false;
    }

    public void TakeDamage()
    {
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