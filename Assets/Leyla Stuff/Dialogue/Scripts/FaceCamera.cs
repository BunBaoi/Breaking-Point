using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Transform playerCam;

    void Start()
    {
        // Find the camera by its tag
        playerCam = GameObject.FindGameObjectWithTag("PlayerCamera").transform;
    }

    void LateUpdate()
    {
        if (playerCam != null)
        {
            // Make the dialogue bubble face the player camera
            transform.LookAt(playerCam);

            transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y + 180f, 0f);
        }
    }
}
