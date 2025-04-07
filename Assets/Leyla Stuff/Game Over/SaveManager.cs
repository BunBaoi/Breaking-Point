using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;
    private string savePath;

    public bool useEncryption = true;

    public CanvasGroup fadeCanvasGroup;
    private bool isFading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            savePath = Application.persistentDataPath + "/save.dat";

            Debug.Log("Save path: " + savePath);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator FadeToBlack(float duration)
    {
        float timeElapsed = 0f;

        fadeCanvasGroup.gameObject.SetActive(true);
        while (timeElapsed < duration)
        {
            float alpha = Mathf.Lerp(0f, 1f, timeElapsed / duration);
            fadeCanvasGroup.alpha = alpha;
            timeElapsed += Time.unscaledDeltaTime; ;
            yield return null;
        }
        fadeCanvasGroup.alpha = 1f;
    }

    private IEnumerator FadeFromBlack(float duration)
    {
        float timeElapsed = 0f;

        while (timeElapsed < duration)
        {
            float alpha = Mathf.Lerp(1f, 0f, timeElapsed / duration);
            fadeCanvasGroup.alpha = alpha;
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.gameObject.SetActive(false);
    }

    public void SaveGame()
    {
        SaveData data = new SaveData();
        data.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        // Save player position and rotation
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            data.playerPosition = new SerializableVector3(player.transform.position);
            data.playerRotation = new SerializableQuaternion(player.transform.rotation);
        }

        // Save the camera's xRotation
        CameraController cameraController = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<CameraController>();
        if (cameraController != null)
        {
            data.cameraXRotation = cameraController.xRotation;
        }

        // Save bools from bool manager
        foreach (string boolKey in BoolManager.Instance.GetBoolKeys())
        {
            bool boolValue = BoolManager.Instance.GetBool(boolKey);
            data.boolStates.Add(new BoolState(boolKey, boolValue));
            Debug.Log($"Saving Bool State - {boolKey}: {boolValue}");
        }

        // Save object active states
        foreach (var objState in ObjectTracker.Instance.GetTrackedObjects())
        {
            GameObject obj = GameObject.Find(objState.Key);

            if (obj != null)
            {
                // Object is in the scene and is not destroyed, save its state
                data.objectActiveStates.Add(new ObjectActiveState(objState.Key, obj.activeSelf, false));
                Debug.Log($"Object {objState.Key} found in the scene. Active state: {obj.activeSelf}, Not destroyed.");
            }
            else if (ObjectTracker.Instance.IsObjectDestroyed(objState.Key))
            {
                // If the object is not found in the scene but is marked as destroyed
                Debug.Log($"Object {objState.Key} not found in the scene, but marked as destroyed. Adding to save with destroyed state.");
                data.objectActiveStates.Add(new ObjectActiveState(objState.Key, false, true));
            }
            else
            {
                Debug.LogWarning($"Object {objState.Key} not found and not marked as destroyed.");
            }
        }

        // Save the current day, hours, and minutes
        DayNightCycle dayNightCycle = FindObjectOfType<DayNightCycle>();
        if (dayNightCycle != null)
        {
            data.savedDay = dayNightCycle.day;
            data.savedHours = dayNightCycle.hours;
            data.savedMinutes = dayNightCycle.minutes;
        }

        // Save the dialogue progress
        foreach (string dialogueID in DialogueManager.Instance.GetAllDialogueIDs())
        {
            bool dialogueProgress = DialogueManager.Instance.GetDialogueProgress(dialogueID);
            data.dialogueStates.Add(new DialogueState(dialogueID, dialogueProgress));
        }

        // Save oxygen and energy stats
        PlayerStats playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            data.oxygen = playerStats.Oxygen;
            data.energy = playerStats.Energy;
        }

        // Save inventory state
        foreach (string itemID in InventoryManager.Instance.GetInventoryIDs())
            data.inventoryItems.Add(itemID);

        // Save the shown tip IDs
        foreach (string tipID in TipManager.Instance.GetShownTipIDs())
            data.shownTipIDs.Add(tipID);

        // Serialise data to JSON
        string json = JsonUtility.ToJson(data);

        // Encrypt or save as plain text
        if (useEncryption)
        {
            string encryptedJson = EncryptionUtility.Encrypt(json);
            File.WriteAllText(savePath, encryptedJson);
            Debug.Log("Game saved with encryption.");
        }
        else
        {
            File.WriteAllText(savePath, json);
            Debug.Log("Game saved without encryption.");
        }
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning("No save file found!");
            return;
        }

        StartCoroutine(FadeToBlack(1f));

        // Read the save file
        string fileContent = File.ReadAllText(savePath);

        string json = "";
        if (useEncryption)
        {
            string encrypted = fileContent;
            json = EncryptionUtility.Decrypt(encrypted);
        }
        else
        {
            json = fileContent;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // Apply saved data
        StartCoroutine(LoadSceneAndApplyData(data));
    }

    private IEnumerator LoadSceneAndApplyData(SaveData data)
    {
        yield return new WaitForSecondsRealtime(1f);

        if (GameOverMenu.Instance != null)
        {
            GameOverMenu.Instance.HideGameOverPanel();
        }

        // Load the saved scene
        var asyncLoad = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(data.sceneName);
        yield return new WaitUntil(() => asyncLoad.isDone);

        yield return null;

        // Restore player position and rotation
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = data.playerPosition.ToVector3();
            player.transform.rotation = data.playerRotation.ToQuaternion();
        }

        // Restore camera xRotation
        CameraController cameraController = GameObject.FindGameObjectWithTag("PlayerCamera").GetComponent<CameraController>();
        if (cameraController != null)
        {
            cameraController.xRotation = data.cameraXRotation;
        }

        // Restore bools
        foreach (var boolState in data.boolStates)
        {
            if (BoolManager.Instance != null)
            {
                BoolManager.Instance.SetBool(boolState.key, boolState.value);
                Debug.Log($"Restoring Bool State - {boolState.key}: {boolState.value}");
            }
            else
            {
                Debug.LogWarning($"BoolManager not found. Failed to restore {boolState.key}.");
            }
        }

        // Restore object active states
        foreach (var objState in data.objectActiveStates)
        {
            GameObject obj = GameObject.Find(objState.objectName);

            if (obj != null)
            {
                if (!objState.isDestroyed)
                {
                    // If the object is not destroyed, restore its active state
                    obj.SetActive(objState.isActive);
                    Debug.Log($"Object {objState.objectName} restored with active state: {objState.isActive}");
                }
                else
                {
                    // If the object is marked as destroyed, destroy it
                    Debug.Log($"Object {objState.objectName} was marked as destroyed and has been destroyed in the scene.");
                    ObjectTracker.Instance.MarkAsDestroyed(objState.objectName);
                    Destroy(obj);
                }
            }
            else
            {
                // If the object is not found in the scene, check if it was marked as destroyed
                if (objState.isDestroyed)
                {
                    // logfor debugging
                    ObjectTracker.Instance.MarkAsDestroyed(objState.objectName);
                    Debug.LogWarning($"Object {objState.objectName} was destroyed and cannot be restored.");
                }
                else
                {
                    // log if object not found in scene but marked as destroyed
                    Debug.LogWarning($"Object {objState.objectName} not found in the scene.");
                }
            }
        }

        // Restore the saved time
        DayNightCycle dayNightCycle = FindObjectOfType<DayNightCycle>();
        if (dayNightCycle != null)
        {
            dayNightCycle.day = data.savedDay;
            dayNightCycle.hours = data.savedHours;
            dayNightCycle.minutes = data.savedMinutes;

            // Update lighting and UI
            dayNightCycle.UpdateLighting();
            dayNightCycle.UpdateTimeUI();
        }

        // Load oxygen and energy
        PlayerStats playerStats = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.Oxygen = data.oxygen;
            playerStats.Energy = data.energy;
        }

        // Restore dialogue progress
        foreach (var dialogueState in data.dialogueStates)
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.SetDialogueProgress(dialogueState.dialogueID, dialogueState.isCompleted);
                Debug.Log($"Restoring Dialogue Progress for {dialogueState.dialogueID}: {dialogueState.isCompleted}");
            }
            else
            {
                Debug.LogWarning($"DialogueManager not found. Failed to restore dialogue progress for {dialogueState.dialogueID}.");
            }
        }

        // Restore shown tip IDs
        TipManager.Instance.SetShownTipIDs(data.shownTipIDs);

        // Restore Inventory
        InventoryManager.Instance.LoadInventory(data.inventoryItems);

        StartCoroutine(FadeFromBlack(2f));

        Debug.Log("Game loaded.");
    }
}