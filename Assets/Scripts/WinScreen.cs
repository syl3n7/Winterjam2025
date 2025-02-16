using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class WinScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float fadeInDuration = 1f;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button menuButton;
    
    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        
        // Setup button listeners
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        
        if (menuButton != null)
        {
            menuButton.onClick.AddListener(OnMenuClicked);
        }
    }

    private void Start()
    {
        Debug.Log("WinScreen Canvas: " + GetComponentInParent<Canvas>()?.name);
        Debug.Log("WinScreen CanvasGroup: " + (canvasGroup != null));
        Debug.Log("WinScreen GameObject: " + gameObject.name);
    }
    
    public void Show()
    {
        Debug.Log("Showing win screen");
        gameObject.SetActive(true);  // Make sure object is active first
        if (gameObject.activeInHierarchy)  // Check if successfully activated
        {
            StartCoroutine(FadeIn());
        }
        else
        {
            Debug.LogError("Failed to activate WinScreen GameObject!");
            canvasGroup.alpha = 1f;  // Fallback to instant show
        }
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
    
    public void OnContinueClicked()
    {
        // Load next level or do something else
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void OnMenuClicked()
    {
        StartCoroutine(ExitToMainMenu());
    }

    private IEnumerator ExitToMainMenu()
    {
        float elapsedTime = 0;
        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = 1 - (elapsedTime / fadeInDuration);
            yield return null;
        }
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {
        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OnMenuClicked);
        }
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(OnContinueClicked);
        }
    }
}