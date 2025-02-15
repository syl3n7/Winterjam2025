using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform Player;
    [SerializeField] private float smoothSpeed = 0.125f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 2f, -10f); // Offset for camera position

    private void LateUpdate()
    {
        if (Player == null) return;

        // Calculate desired position
        Vector3 desiredPosition = Player.position + offset;
        
        // Smoothly interpolate between current position and desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // Update camera position
        transform.position = smoothedPosition;
    }
}
