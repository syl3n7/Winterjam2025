using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color flashColor = Color.red;
    
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isFlashing;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void Flash()
    {
        if (!isFlashing)
        {
            StartCoroutine(FlashRoutine());
        }
        else
        {
            // Reset and restart flash
            StopAllCoroutines();
            spriteRenderer.color = originalColor;
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
        isFlashing = false;
    }
}