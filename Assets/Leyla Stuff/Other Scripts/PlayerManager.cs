using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;

    [Header("Player Settings")]
    public GameObject playerInstance;
    public GameObject playerPrefab;

    [Header("UI Settings")]
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    private Vector3 teleportPosition;
    private Quaternion teleportRotation;
    private bool shouldTeleport = false;
    private string targetTagToFind;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (playerInstance == null)
        {
            playerInstance = GameObject.FindGameObjectWithTag("Player");
        }
        if (playerInstance != null)
        {
            DontDestroyOnLoad(playerInstance);
        }
    }

    // Teleport function that takes a tag to find an empty GameObject to teleport to
    public void TeleportToScene(string sceneName, string targetTag)
    {
        targetTagToFind = targetTag;
        shouldTeleport = true;
        StartCoroutine(FadeAndLoadScene(sceneName));


        PlayerMovement playerMovement = playerInstance.GetComponent<PlayerMovement>();
            if (playerMovement != null)
        {
            playerMovement.SetApplyGravity(false);
        }

        CharacterController characterController = playerInstance.GetComponent<CharacterController>();
        if (characterController != null)
        {
            characterController.enabled = false;  // Disable CharacterController
            Debug.Log("CharacterController disabled.");
        }
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        yield return StartCoroutine(Fade(1f));

        GameManager.Instance.ShowLoadingPanel();

        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName);
        asyncOperation.allowSceneActivation = false;

        while (!asyncOperation.isDone)
        {
            if (asyncOperation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(0.5f);
                asyncOperation.allowSceneActivation = true;
            }

            yield return null;
        }

        GameManager.Instance.HideLoadingPanel();

        yield return StartCoroutine(Fade(0f));

        SaveManager.Instance.SaveGame();
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timeElapsed / fadeDuration);
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene Loaded: {scene.name}");

        if (scene.name.Contains("Level") || scene.name.Contains("Game") || scene.name.Contains("Test"))
        {
            Debug.Log("Scene is a gameplay scene, proceeding with player setup.");

            // If playerInstance is null, try to find the player by tag "Player"
            if (playerInstance == null)
            {
                Debug.Log("playerInstance is null. Searching for GameObject with tag 'Player'.");
                playerInstance = GameObject.FindGameObjectWithTag("Player");

                if (playerInstance == null && playerPrefab != null)
                {
                    Debug.Log("No existing Player found in scene. Instantiating player prefab.");
                    playerInstance = Instantiate(playerPrefab);
                    DontDestroyOnLoad(playerInstance);
                    Debug.Log("Player prefab instantiated and marked as DontDestroyOnLoad.");
                }
                else if (playerInstance != null)
                {
                    Debug.Log("Found Player in scene. Marking as DontDestroyOnLoad.");
                    DontDestroyOnLoad(playerInstance);
                }
                else
                {
                    Debug.LogWarning("No Player found and playerPrefab is null!");
                }
            }
            else
            {
                Debug.Log("playerInstance already exists, skipping instantiation.");
            }

            // Destroy any other object with the "Player" tag that isn't the current playerInstance
            GameObject[] otherPlayers = GameObject.FindGameObjectsWithTag("Player");
            Debug.Log($"Found {otherPlayers.Length} GameObjects with tag 'Player'.");
            foreach (GameObject otherPlayer in otherPlayers)
            {
                if (otherPlayer != playerInstance)
                {
                    Debug.Log($"Destroying duplicate player object: {otherPlayer.name}");
                    Destroy(otherPlayer);
                }
            }

            // Set the player's teleport position and rotation if they exist
            if (shouldTeleport && playerInstance != null)
            {
                // Search for the target object with the tag after the scene is loaded
                GameObject targetObject = GameObject.FindGameObjectWithTag(targetTagToFind);

                CharacterController characterController = playerInstance.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;  // Disable CharacterController
                    Debug.Log("CharacterController disabled.");
                }

                if (targetObject != null)
                {
                    // Teleport the player to the target object position and rotation
                    playerInstance.transform.position = targetObject.transform.position;
                    playerInstance.transform.rotation = targetObject.transform.rotation;
                    shouldTeleport = false;  // Reset teleport flag after teleportation
                    StartCoroutine(EnableCharacterControllerAfterDelay(characterController, 0.1f));
                    Debug.Log($"Player teleported to {targetObject.name} - Position: {playerInstance.transform.position}, Rotation: {playerInstance.transform.rotation}");
                }
                else
                {
                    Debug.LogError($"No object found with tag {targetTagToFind}");
                }
            }
        }
        else
        {
            Debug.Log("Scene is not a gameplay scene. Cleaning up persistent player if exists.");
            if (playerInstance != null)
            {
                Debug.Log("Destroying playerInstance due to scene type.");
                Destroy(playerInstance);
                playerInstance = null;
            }
        }
    }

    private IEnumerator EnableCharacterControllerAfterDelay(CharacterController characterController, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (characterController != null)
        {
            characterController.enabled = true;
            Debug.Log("CharacterController re-enabled.");
        }

        PlayerMovement playerMovement = playerInstance.GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.SetApplyGravity(true);
        }
    }

        void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
