using UnityEngine;
using UnityEngine.SceneManagement;

public class NPCManager : MonoBehaviour
{
    public float lookRadius = 5f;
    public float rotationSpeed = 5f;

    private Transform player;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player not found in scene.");
        }
    }
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name.Contains("Game") || scene.name.Contains("Level"))
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            else
            {
                Debug.LogError("Player not found in scene.");
            }
        }
    }
        void Update()
    {
        if (player == null) return;

        GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");

        foreach (GameObject npc in npcs)
        {
            float distance = Vector3.Distance(npc.transform.position, player.position);

            if (distance <= lookRadius)
            {
                Vector3 direction = player.position - npc.transform.position;
                direction.y = 0f; // Keep horizontal

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    npc.transform.rotation = Quaternion.Slerp(npc.transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                }
            }
        }
    }
}

