using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LeylaTestingScript : MonoBehaviour
{
    [SerializeField] private KeyCode key = KeyCode.P;
    [SerializeField] private KeyCode key2 = KeyCode.M;
    [SerializeField] private KeyCode key3 = KeyCode.V;
    [SerializeField] private string boolName = "";
    [SerializeField] private TipManager tipManager;
    [SerializeField] private int tipNumber = 0;
    [SerializeField] private JournalPageAdder journalPageAdder;
    [SerializeField] private ObjectivesPage objectivesPage;
    [SerializeField] private GameOverMenu gameOverMenu;
    [SerializeField] private CinematicSequence cinematicSequence;
    [SerializeField] private CallingCompanionMethods callingCompanionMethods;
    [SerializeField] private Vector3 teleportPosition;

    // Start is called before the first frame update
    /*void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            // BoolManager.Instance.SetBool(boolName, true);
            // cinematicSequence.StartCinematic();
            // SceneManager.LoadScene("MainMenu");
            SaveManager.Instance.SaveGame();
            // Debug.Log("save game");
            // Debug.Log("add page");
            // journalPageAdder.AddObjectivesPage(objectivesPage);
            // tipManager.ShowTip(tipNumber);
            /*if (BoolManager.Instance != null)
            {
                BoolManager.Instance.SetBool(boolName, true);
            }
            else
            {
                Debug.LogError("BoolManager.Instance is null.");
            }
        }
        if (Input.GetKeyDown(key2))
        {
          // BoolManager.Instance.SetBool(boolName, true);
        // callingCompanionMethods.CallTeleportToPosition(teleportPosition);
            SaveManager.Instance.LoadGame();
            // Debug.Log("load game");
        }
        if (Input.GetKeyDown(key3))
        {
            // PlayerManager.Instance.TeleportToScene("Leylas Testing Ground", "Tenzing");
            // gameOverMenu.ShowGameOver();
            // Debug.Log("load game");
        }
    }*/
}
