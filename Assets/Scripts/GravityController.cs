using UnityEngine;

public class GravityController : MonoBehaviour
{
    public static GravityController Instance { get; private set; }
    
    [SerializeField] private float normalGravityScale = 1f;
    [SerializeField] private float invertedGravityScale = -1f;
    private bool isGravityInverted;
    
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
    }

    public void InvertGravity()
    {
        isGravityInverted = !isGravityInverted;
        float targetGravityScale = isGravityInverted ? invertedGravityScale : normalGravityScale;
        
        // Affect all Rigidbody2D objects in the scene
        Rigidbody2D[] allRigidbodies = FindObjectsOfType<Rigidbody2D>();
        foreach (Rigidbody2D rb in allRigidbodies)
        {
            rb.gravityScale = targetGravityScale;
        }
        
        Debug.Log($"Gravity Inverted: {isGravityInverted}");
    }

    public bool IsGravityInverted() => isGravityInverted;
}