using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    public GameObject playerInstance;
    public GameObject playerPrefab;
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;

    private Vector3 teleportPosition;
    private Quaternion teleportRotation;

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
        // Find the object with the specified tag
        GameObject targetObject = GameObject.FindGameObjectWithTag(targetTag);

        if (targetObject != null)
        {
            // Get the position and rotation of the target object
            teleportPosition = targetObject.transform.position;
            teleportRotation = targetObject.transform.rotation;
            Debug.Log($"Teleport target found. Position: {teleportPosition}, Rotation: {teleportRotation}");
        }
        else
        {
            Debug.LogError($"No object found with tag {targetTag}");
            return;  // Prevent teleporting if no object is found
        }

        // Start the scene loading process
        StartCoroutine(FadeAndLoadScene(sceneName));
    }

    private IEnumerator FadeAndLoadScene(string sceneName)
    {
        yield return StartCoroutine(Fade(1f));

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

        yield return StartCoroutine(Fade(0f));
    }

    private IEnumerator Fade(float targetAlpha)
    {
        float startAlpha = fadeCanvasGroup.alpha;
        float timeElapsed = 0f;

        while (timeElapsed < fadeDuration)
        {
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timeElapsed / fadeDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        fadeCanvasGroup.alpha = targetAlpha;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Level") || scene.name.Contains("Game") || scene.name.Contains("Test"))
        {
            // If playerInstance is null, try to find the player by tag "Player"
            if (playerInstance == null)
            {
                playerInstance = GameObject.FindGameObjectWithTag("Player");

                if (playerInstance == null && playerPrefab != null)
                {
                    playerInstance = Instantiate(playerPrefab);
                    DontDestroyOnLoad(playerInstance);
                }
                else if (playerInstance != null)
                {
                    DontDestroyOnLoad(playerInstance);
                }
            }

            // Destroy any other object with the "Player" tag that isn't the current playerInstance
            GameObject[] otherPlayers = GameObject.FindGameObjectsWithTag("Player");
            foreach (GameObject otherPlayer in otherPlayers)
            {
                if (otherPlayer != playerInstance)
                {
                    Destroy(otherPlayer);
                }
            }

            // Set the player's teleport position and rotation if they exist
            if (playerInstance != null)
            {
                playerInstance.transform.position = teleportPosition;
                playerInstance.transform.rotation = teleportRotation;
                Debug.Log($"Player teleported to Position: {teleportPosition}, Rotation: {teleportRotation}");
            }
        }
        else
        {
            if (playerInstance != null)
            {
                Destroy(playerInstance);
                playerInstance = null;
            }
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
