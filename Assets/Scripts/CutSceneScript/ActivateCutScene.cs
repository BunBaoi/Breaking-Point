using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using Cinemachine;

public class ActivateCutScene : MonoBehaviour
{
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private CinemachineVirtualCamera playerCamera;
    [SerializeField] private CinemachineVirtualCamera cutsceneCamera;

    // Store the original priority to restore it later
    private int originalPlayerPriority;

    private void Start()
    {
        // Store the original player camera priority
        originalPlayerPriority = playerCamera.Priority;

        // Initially set cutscene camera to low priority
        cutsceneCamera.Priority = 0;
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player"))
        {
            // Disable player camera by setting its priority to 0
            playerCamera.Priority = 0;

            // Enable cutscene camera by setting higher priority
            cutsceneCamera.Priority = 20;

            // Play the cutscene
            playableDirector.Play();

            // Subscribe to the stopped event
            playableDirector.stopped += OnCutsceneFinished;

            // Disable the trigger collider
            GetComponent<BoxCollider>().enabled = false;
        }
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        // Restore player camera's original priority
        playerCamera.Priority = originalPlayerPriority;

        // Disable cutscene camera
        cutsceneCamera.Priority = 0;

        // Unsubscribe from the event
        playableDirector.stopped -= OnCutsceneFinished;
    }

    private void OnDestroy()
    {
        // Clean up event subscription
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnCutsceneFinished;
        }
    }
}
