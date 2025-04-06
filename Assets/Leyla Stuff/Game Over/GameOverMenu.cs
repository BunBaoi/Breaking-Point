using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class GameOverMenu : MonoBehaviour
{
    public static GameOverMenu Instance;

    [SerializeField] private GameObject gameOverPanel;
    public Button[] optionButtons;
    public GameObject[] optionPanels;
    [SerializeField] private float delay = 0.5f;

    private PlayerMovement playerMovement;
    private InventoryManager inventoryManager;

    private void Awake()
    {
        gameOverPanel.SetActive(false);

        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int index = i;
            optionButtons[i].onClick.AddListener(() => ShowPanel(index));
        }

        HideAllPanels();
    }

    void ShowPanel(int index)
    {
        HideAllPanels();

        if (index >= 0 && index < optionPanels.Length)
        {
            optionPanels[index].SetActive(true);
        }
    }

    void HideAllPanels()
    {
        foreach (var panel in optionPanels)
        {
            panel.SetActive(false);
        }
    }

    public void LoadLastCheckpoint()
    {
        SaveManager.Instance.LoadGame();
    }

    public void ShowGameOver()
    {
        // Show the Game Over Panel
        gameOverPanel.SetActive(true);

        // Pause the game (stop time)
        Time.timeScale = 0f;

        // Unlock the cursor and make it visible
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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

    public void HideGameOver()
    {
        Time.timeScale = 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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

    public void HideGameOverPanel()
    {
        gameOverPanel.SetActive(false);
    }
}

