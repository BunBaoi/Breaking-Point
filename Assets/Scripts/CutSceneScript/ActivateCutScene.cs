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

    [Header("Bool Conditions")]
    [SerializeField] private List<string> requiredBoolKeysTrue = new List<string>(); // List of bool keys that should be true
    [SerializeField] private List<string> requiredBoolKeysFalse = new List<string>(); // List of bool keys that should be false

    [Header("Cutscene Data")]
    [SerializeField] private CutsceneData cutsceneData;

    // Store the original priority to restore it later
    private int originalPlayerPriority;

    private void Start()
    {
        GameObject playerVirtualCameraObject = GameObject.FindGameObjectWithTag("PlayerVirtualCamera");

        if (playerVirtualCameraObject != null)
        {
            playerCamera = playerVirtualCameraObject.GetComponent<CinemachineVirtualCamera>();
        }

        // Store the original player camera priority
        originalPlayerPriority = playerCamera.Priority;

        // Initially set cutscene camera to low priority
        cutsceneCamera.Priority = 0;
    }

    private bool CanStartCutscene()
    {
        if (CutsceneTracker.Instance.IsCutsceneCompleted(cutsceneData.cutsceneID))
        {
            return false; // Already completed, don't run again
        }

        if (!DialogueManager.Instance.canStartDialogue)
        {
            return false;
        }
        // Check if all required bool conditions are met (true or false)
        foreach (string boolKey in requiredBoolKeysTrue)
        {
            if (!BoolManager.Instance.GetBool(boolKey))
            {
                return false; // If any bool is false when it should be true, return false
            }
        }

        foreach (string boolKey in requiredBoolKeysFalse)
        {
            if (BoolManager.Instance.GetBool(boolKey))
            {
                return false;
            }
        }

        return true; // All conditions are met, return true
    }

    private void OnTriggerEnter(Collider collision)
    {
        if (collision.CompareTag("Player") && CanStartCutscene())
        {
            CutsceneTracker.Instance.MarkCutsceneAsCompleted(cutsceneData.cutsceneID);

            // Disable player camera by setting its priority to 0
            playerCamera.Priority = 0;

            // Enable cutscene camera by setting higher priority
            cutsceneCamera.Priority = 20;

            // Play the cutscene
            playableDirector.Play();

            DialogueManager.Instance.SetInventoryActive(false);

            // Subscribe to the stopped event
            playableDirector.stopped += OnCutsceneFinished;

            // Disable the trigger collider
            GetComponent<BoxCollider>().enabled = false;

            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
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
