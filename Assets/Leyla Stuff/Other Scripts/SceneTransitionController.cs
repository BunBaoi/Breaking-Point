using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneTransitionController : MonoBehaviour
{
    public float transitionDuration = 1.0f;
    public Image transitionImage;

    private PlayerMovement playerMovement;
    private InventoryManager inventoryManager;

    private CanvasGroup canvasGroup;

    private void Start()
    {
        canvasGroup = transitionImage.GetComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
    }

    public void StartTransition(string sceneName)
    {
        GameManager.Instance.ShowLoadingPanel();
        // Start the transition coroutine
        StartCoroutine(Transition(sceneName));

        Transform parent = transform.parent;

        if ((parent == null || parent.name != "GameManager"))
        {
            DontDestroyOnLoad(gameObject);
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();
            inventoryManager = playerObject.GetComponent<InventoryManager>();

            if (playerMovement != null)
            {
                playerMovement.SetMovementState(false);
            }
            if (inventoryManager != null)
            {
                inventoryManager.enabled = false;
            }
        }
    }

    private IEnumerator Transition(string sceneName)
    {
        canvasGroup.gameObject.SetActive(true);
        // Fade to black
        while (canvasGroup.alpha < 1)
        {
            Time.timeScale = 1;
            canvasGroup.alpha += Time.deltaTime / transitionDuration;
            yield return null;
        }

        if (GameOverMenu.Instance != null)
        {
            GameOverMenu.Instance.HideGameOverPanel();
        }

        canvasGroup.alpha = 1f;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Fade back out
        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= Time.deltaTime / transitionDuration;
            yield return null;
        }

        GameManager.Instance.HideLoadingPanel();

        canvasGroup.alpha = 0f;
        canvasGroup.gameObject.SetActive(false);
        StartCoroutine(DestroyObject());
    }

    private IEnumerator DestroyObject()
    {
        yield return new WaitForSeconds(0.1f);

        Transform parent = transform.parent;

        if ((parent == null || parent.name != "GameManager"))
        {
            Destroy(gameObject);
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerMovement = playerObject.GetComponent<PlayerMovement>();
            inventoryManager = playerObject.GetComponent<InventoryManager>();

            if (playerMovement != null)
            {
                playerMovement.SetMovementState(true);
            }
            if (inventoryManager != null)
            {
                inventoryManager.enabled = true;
            }
        }
    }
}
