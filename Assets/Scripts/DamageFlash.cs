using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    [SerializeField] private float flashDuration = 0.2f;
    [SerializeField] private Color flashColor = Color.red;
    
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;
    private bool isFlashing;

    private void Awake()
    {
        // Get all sprite renderers in children
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalColors = new Color[spriteRenderers.Length];
        
        // Store original colors
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
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
            RestoreOriginalColors();
            StartCoroutine(FlashRoutine());
        }
    }

    private IEnumerator FlashRoutine()
    {
        isFlashing = true;
        
        // Set flash color for all sprites
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].color = flashColor;
        }
        
        yield return new WaitForSeconds(flashDuration);
        
        RestoreOriginalColors();
        isFlashing = false;
    }

    private void RestoreOriginalColors()
    {
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].color = originalColors[i];
        }
    }
}