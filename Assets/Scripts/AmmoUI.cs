using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ammoText;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        SetupUIPosition();
    }

    private void SetupUIPosition()
    {
        // Position in top-right corner below hearts
        rectTransform.anchorMin = Vector2.one;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = Vector2.one;
        rectTransform.anchoredPosition = new Vector2(-20f, -60f); // Below hearts
    }

    public void UpdateAmmoText(int currentAmmo)
    {
        ammoText.text = $"Ammo: {currentAmmo}";
    }
}