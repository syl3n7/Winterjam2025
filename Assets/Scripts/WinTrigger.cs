using UnityEngine;

public class WinTrigger : MonoBehaviour
{
    private WinScreen winScreen;

    private void Start()
    {
        winScreen = FindObjectOfType<WinScreen>(true); // Include inactive objects
        Debug.Log("WinTrigger initialized, WinScreen found: " + (winScreen != null));
        
        // Modified debug information for 2D collider
        Debug.Log("Trigger Collider2D enabled: " + GetComponent<Collider2D>()?.enabled);
        Debug.Log("Trigger Collider2D isTrigger: " + GetComponent<Collider2D>()?.isTrigger);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger entered by: " + other.gameObject.name);
        Debug.Log("Colliding object layer: " + other.gameObject.layer);
        Debug.Log("Colliding object tag: " + other.gameObject.tag);
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player tag detected");
            if (winScreen != null)
            {
                winScreen.Show();
            }
            else
            {
                Debug.LogWarning("WinScreen not found in the scene!");
            }
        }
    }
}