using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class HealthUI : MonoBehaviour
{
    [Header("Heart Settings")]
    [SerializeField] private GameObject heartPrefab;
    [SerializeField] private Sprite fullHeartSprite;
    [SerializeField] private Sprite emptyHeartSprite;
    [SerializeField] private Vector2 heartSize = new Vector2(50f, 50f);
    [SerializeField] private float spacing = 10f;
    [SerializeField] private Image[] heartImages = new Image[3];

    private List<Image> hearts = new List<Image>();
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        SetupUIPosition();
    }

    private void Start()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            SetupHearts(player.MaxHealth);
        }

        // Make sure all hearts are full at start
        UpdateHearts(3);
    }

    private void SetupUIPosition()
    {
        // Position in top-right corner
        rectTransform.anchorMin = Vector2.one;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = Vector2.one;
        rectTransform.anchoredPosition = new Vector2(-20f, -20f); // 20 pixels from top-right
    }

    private void SetupHearts(int maxHealth)
    {
        // Clear any existing hearts
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
        hearts.Clear();

        // Create hearts from right to left
        for (int i = 0; i < maxHealth; i++)
        {
            GameObject heartObj = Instantiate(heartPrefab, transform);
            RectTransform heartTransform = heartObj.GetComponent<RectTransform>();
            
            // Position from right to left
            float xPos = -(heartSize.x + spacing) * i;
            heartTransform.anchoredPosition = new Vector2(xPos, 0);
            
            Image heartImage = heartObj.GetComponent<Image>();
            heartImage.sprite = fullHeartSprite;
            heartImage.rectTransform.sizeDelta = heartSize;
            hearts.Add(heartImage);
        }

        // Set container size
        float totalWidth = (heartSize.x + spacing) * maxHealth - spacing;
        rectTransform.sizeDelta = new Vector2(totalWidth, heartSize.y);
    }

    public void UpdateHearts(int currentHealth)
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            hearts[i].sprite = i < currentHealth ? fullHeartSprite : emptyHeartSprite;
        }

        for (int i = 0; i < heartImages.Length; i++)
        {
            heartImages[i].sprite = i < currentHealth ? fullHeartSprite : emptyHeartSprite;
        }
    }
}