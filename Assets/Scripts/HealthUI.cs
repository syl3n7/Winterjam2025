using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private float heartSpacing = 10f;
    [SerializeField] private Vector2 heartSize = new Vector2(50f, 50f);
    [SerializeField] private Vector2 topRightOffset = new Vector2(20f, 20f);

    private List<Image> hearts = new List<Image>();
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        SetupAnchors();
    }

    private void Start()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            SetupHearts(player.MaxHealth);
        }
    }

    private void SetupAnchors()
    {
        // Position in top-right corner
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(1, 1);
        rectTransform.anchoredPosition = -topRightOffset;
    }

    private void SetupHearts(int maxHealth)
    {
        // Clear existing hearts
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        hearts.Clear();

        // Calculate total width needed
        float totalWidth = (heartSize.x + heartSpacing) * maxHealth - heartSpacing;

        // Create hearts from right to left
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heartObj = Instantiate(heartPrefab, transform);
            RectTransform heartTransform = heartObj.GetComponent<RectTransform>();
            
            // Set size
            heartTransform.sizeDelta = heartSize;
            
            // Position from right to left
            float xPos = -((heartSize.x + heartSpacing) * i);
            heartTransform.anchoredPosition = new Vector2(xPos, 0);
            
            Image heartImage = heartObj.GetComponent<Image>();
            heartImage.sprite = fullHeartSprite;
            hearts.Add(heartImage);
        }

        // Set container width to fit all hearts
        rectTransform.sizeDelta = new Vector2(totalWidth, heartSize.y);
    }

    public void UpdateHearts(int currentHealth)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].sprite = i < currentHealth ? fullHeartSprite : emptyHeartSprite;
        }
    }
}