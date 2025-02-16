using UnityEngine;

public class GravityController : MonoBehaviour
{
    public static GravityController Instance { get; private set; }
    
    [SerializeField] private float normalGravityScale = 1f;
    [SerializeField] private float invertedGravityScale = -1f;
    private bool isGravityInverted;
    private Rigidbody2D playerRigidbody;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // Get the player's Rigidbody2D on start
        playerRigidbody = FindObjectOfType<PlayerController>()?.GetComponent<Rigidbody2D>();
        if (playerRigidbody == null)
        {
            Debug.LogError("Player Rigidbody2D not found!");
        }
    }

    public void InvertGravity()
    {
        if (playerRigidbody == null) return;
        
        isGravityInverted = !isGravityInverted;
        float targetGravityScale = isGravityInverted ? invertedGravityScale : normalGravityScale;
        
        // Only affect the player's Rigidbody2D
        playerRigidbody.gravityScale = targetGravityScale;
        
        Debug.Log($"Player Gravity Inverted: {isGravityInverted}");
    }

    public void ResetGravity()
    {
        if (playerRigidbody == null) return;
        
        isGravityInverted = false;
        playerRigidbody.gravityScale = normalGravityScale;
        Debug.Log("Gravity Reset to Normal");
    }

    public bool IsGravityInverted() => isGravityInverted;
}