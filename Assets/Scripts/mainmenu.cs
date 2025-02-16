using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Add this for Button component

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button quitButton; // Reference to quit button

    private void Start()
    {
        // Check if running in WebGL
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            // Disable quit button in WebGL
            if (quitButton != null)
            {
                quitButton.interactable = false;
                quitButton.gameObject.SetActive(false); // Optional: completely hide the button
            }
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("Level Design");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }
}