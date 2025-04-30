using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CompanionScript : MonoBehaviour
{
    [Header("AI Companion")]
    public NavMeshAgent AI;
    public Transform player;
    public float minDistanceToPlayer = 2f;
    public float maxDistanceToPlayer = 5f;
    public float companionSpeed = 3.5f;
    public CompanionState stateOfCompanion;
    private bool teleportLeft = true;
    private Animator animator;

    [Header("Companion Visuals")]
    public GameObject companionModel;
    private bool isVisible = true;

    [Header("Companion Audio")]
    public AudioSource audioSource;
    public AudioClip[] companionSounds;

    private Coroutine teleportRoutine;

    [Header("Keybindings")]
    /*[Tooltip("Key to toggle talking state")]
    public KeyCode talkKey = KeyCode.F;
    [Tooltip("Key to toggle visibility")]
    public KeyCode visibilityKey = KeyCode.H;
    [Tooltip("Key to teleport to player")]
    public KeyCode teleportKey = KeyCode.T;
    [Tooltip("Key to toggle following")]
    public KeyCode followKey = KeyCode.S;*/

    private Vector3 lastPosition;
    private Vector3 destination;

    public enum CompanionState
    {
        Follow,
        Idle,
        Talking,
        Teleporting
    }

    void Start()
    {
        animator = GetComponentInChildren<Animator>();

        if (AI == null)
            AI = GetComponent<NavMeshAgent>();

        if (companionModel == null)
            companionModel = this.gameObject;

        AI.speed = companionSpeed;
        AI.stoppingDistance = minDistanceToPlayer;

        stateOfCompanion = CompanionState.Follow;
        lastPosition = transform.position;

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        // Make sure the companion is visible at start
        isVisible = true;
    }

    void Update()
    {
        // Process companion behavior based on current state
        switch (stateOfCompanion)
        {
            case CompanionState.Follow:
                // Update destination every frame to keep up with player movement
                FollowPlayer();
                break;

            case CompanionState.Idle:
                // Do nothing - companion stays in place
                break;

            case CompanionState.Talking:
                // Face player while talking
                if (player != null)
                {
                    Vector3 dirToPlayer = player.position - transform.position;
                    dirToPlayer.y = 0;
                    if (dirToPlayer != Vector3.zero)
                    {
                        transform.rotation = Quaternion.LookRotation(dirToPlayer);
                    }
                }
                break;

            case CompanionState.Teleporting:
                // This state is transitional and changed after teleport completes
                break;
        }

        UpdateAnimation();


        // Check for key inputs using the customizable keys
        // CheckKeyInputs();
    }

    // Handle custom key inputs
    /*private void CheckKeyInputs()
    {
        // Talk key
        if (Input.GetKeyDown(talkKey))
        {
            ToggleTalking();
        }

        // Visibility key
        if (Input.GetKeyDown(visibilityKey))
        {
            ToggleVisibility();
        }

        // Teleport key
        if (Input.GetKeyDown(teleportKey))
        {
            TeleportToPlayer();
        }

        // Follow key
        if (Input.GetKeyDown(followKey))
        {
            ToggleFollowing();
        }
    }*/

    private void UpdateAnimation()
    {
        if (animator == null || AI == null)
            return;

        float speed = AI.velocity.magnitude;

        animator.SetFloat("speed", speed);
    }

    public void FollowPlayer()
    {
        if (player == null)
            return;

        // Calculate distance to player
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Calculate position behind player
        Vector3 playerForward = player.forward;
        Vector3 positionBehindPlayer = player.position - (playerForward * minDistanceToPlayer);

        // Check if we need to update the destination
        // We'll update constantly as the player moves
        if (stateOfCompanion == CompanionState.Follow)
        {
            // Always update destination when player is moving
            AI.isStopped = false;
            AI.SetDestination(positionBehindPlayer);

            // Only stop if we're close enough
            if (distanceToPlayer <= minDistanceToPlayer)
            {
                AI.velocity = Vector3.zero;
            }
        }

        lastPosition = transform.position;
    }

    public void ToggleFollowing()
    {
        if (stateOfCompanion == CompanionState.Follow)
        {
            // Stop following and stay in place
            stateOfCompanion = CompanionState.Idle;
            AI.isStopped = true;
            Debug.Log("Companion stopped following");
        }
        else
        {
            // Resume following
            stateOfCompanion = CompanionState.Follow;
            AI.isStopped = false;
            Debug.Log("Companion resumed following");
        }
    }

    public void ToggleTalking()
    {
        if (stateOfCompanion == CompanionState.Talking)
        {
            // Stop talking and resume previous behavior
            stateOfCompanion = CompanionState.Follow;
            if (audioSource && audioSource.isPlaying)
                audioSource.Stop();
            Debug.Log("Companion stopped talking");
        }
        else
        {
            // Start talking
            stateOfCompanion = CompanionState.Talking;
            AI.isStopped = true;

            // Play random companion sound if available
            if (audioSource != null && companionSounds != null && companionSounds.Length > 0)
            {
                int randomIndex = Random.Range(0, companionSounds.Length);
                audioSource.clip = companionSounds[randomIndex];
                audioSource.Play();
            }

            Debug.Log("Companion talking");
        }
    }

    public void TeleportToPosition(Vector3 position)
    {
        // Store current state to return to after teleporting
        CompanionState previousState = stateOfCompanion;
        stateOfCompanion = CompanionState.Teleporting;

        // Validate the position on the NavMesh before teleporting
        NavMeshHit hit;
        Vector3 validPosition = position;

        // First, check if the requested position is on the NavMesh
        if (!NavMesh.SamplePosition(position, out hit, 5f, NavMesh.AllAreas))
        {
            // If not on NavMesh, find the closest valid position
            Debug.LogWarning("Companion teleport position is not on NavMesh. Finding closest valid position.");

            // Try positions around the player at different heights
            Vector3[] testPositions = new Vector3[]
            {
                position + Vector3.up * 0.5f,       // Try slightly above
                player.position + player.right * 2f, // Try to the right of player
                player.position - player.right * 2f, // Try to the left of player
                player.position + player.forward * 2f, // Try in front of player
                player.position - player.forward * 2f  // Try behind player
            };

            // Check each test position
            foreach (Vector3 testPos in testPositions)
            {
                if (NavMesh.SamplePosition(testPos, out hit, 5f, NavMesh.AllAreas))
                {
                    validPosition = hit.position;
                    Debug.Log("Found valid NavMesh position for companion: " + validPosition);
                    break;
                }
            }

            // If we still don't have a valid position, use a fallback
            if (validPosition == position)
            {
                // Fallback to a position near the player that's definitely on the navmesh
                if (NavMesh.SamplePosition(player.position, out hit, 10f, NavMesh.AllAreas))
                {
                    validPosition = hit.position;
                    Debug.Log("Using fallback NavMesh position for companion: " + validPosition);
                }
                else
                {
                    Debug.LogError("Cannot find any valid NavMesh position for companion teleport!");
                    stateOfCompanion = previousState;
                    return;
                }
            }
        }
        else
        {
            // The position is valid, use the exact NavMesh position
            validPosition = hit.position;
        }

        // Disable NavMeshAgent temporarily to avoid conflicts
        AI.enabled = false;

        // Teleport to validated position
        transform.position = validPosition;

        // Re-enable NavMeshAgent
        AI.enabled = true;

        // Return to previous state
        stateOfCompanion = previousState;

        Debug.Log("Companion teleported to: " + validPosition);
    }

    // OLD METHOD, KEEPING IN CASE
    /*public void TeleportToPlayer()
    {
        if (player != null)
        {
            // Teleport behind player
            Vector3 playerForward = player.forward;
            Vector3 positionBehindPlayer = player.position - (playerForward * minDistanceToPlayer);

            TeleportToPosition(positionBehindPlayer);
        }
    }*/

    public void StartTeleportingToPlayer()
    {
        if (teleportRoutine == null)
        {
            teleportRoutine = StartCoroutine(TeleportWhenClearSight());
        }
    }

    private IEnumerator TeleportWhenClearSight()
    {
        while (true)
        {
            if (player == null)
            {
                teleportRoutine = null;
                yield break;
            }

            Vector3[] directions = { -player.right, player.right };

            foreach (Vector3 dir in directions)
            {
                Vector3 desiredPosition = player.position + dir * minDistanceToPlayer;
                desiredPosition.y += 0.1f;

                if (!Physics.Linecast(desiredPosition, player.position, out RaycastHit hit, LayerMask.GetMask("Default")))
                {
                    TeleportToPosition(desiredPosition);
                    teleportRoutine = null;
                    yield break;
                }
            }

            // Wait a short time before checking again
            yield return new WaitForSeconds(0.25f);
        }
    }

    public void FacePlayer()
    {
        if (player != null)
        {
            Vector3 directionToPlayer = player.position - transform.position;
            directionToPlayer.y = 0; // Keep rotation flat on the Y axis

            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                transform.rotation = targetRotation;
            }
        }
    }

    // Toggle visibility by enabling/disabling renderers
    public void ToggleVisibility()
    {
        isVisible = !isVisible;

        // Toggle renderers instead of deactivating the GameObject
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = isVisible;
        }
    }

    // Set visibility to a specific state
    public void SetVisibility(bool visible)
    {
        if (visible != isVisible)
        {
            isVisible = visible;

            // Set renderers instead of activating/deactivating the GameObject
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = visible;
            }
        }
    }

    // Method to make companion disappear for a specific duration
    public void DisappearFor(float duration)
    {
        StartCoroutine(DisappearAndReappear(duration));
    }

    private IEnumerator DisappearAndReappear(float duration)
    {
        // Disappear
        SetVisibility(false);

        // Wait for specified duration
        yield return new WaitForSeconds(duration);

        // Reappear
        SetVisibility(true);
    }

    // Trigger for interaction
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyUp(KeyCode.F))
        {
            ToggleTalking();
        }
    }
}