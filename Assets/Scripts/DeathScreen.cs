using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private TextMeshProUGUI deathText;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button exitButton;
    
    private void Awake()
    {
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        
        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClicked);
        }
    }
    
    public void Show()
    {
        gameObject.SetActive(true);  // Show panel when player dies
        StartCoroutine(FadeIn());
    }
    
    private IEnumerator FadeIn()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = elapsedTime / fadeInDuration;
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
    
    public void OnRestartClicked()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);  // Hide panel when restarting
            player.Respawn();
        }
    }

    public void OnExitClicked()
    {
        StartCoroutine(ExitToMainMenu());
    }

    private IEnumerator ExitToMainMenu()
    {
        // Fade out effect
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = 1 - (elapsedTime / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);  // Hide panel when returning to menu
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitClicked);
        }
    }
}