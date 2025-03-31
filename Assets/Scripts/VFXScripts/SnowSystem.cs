using UnityEngine;
using System.Collections.Generic;

public class SnowSystem : MonoBehaviour
{
    public Transform player;
    public GameObject snowParticlePrefab;
    public float gridSize = 20f;
    public float activationDistance = 40f;
    public float deactivationDistance = 60f;

    [Header("Height Settings")]
    public float heightAbovePlayer = 15f;
    public float maxSnowHeight = 5000f;  // Maximum height where snow should appear
    public float minSnowHeight = -100f;  // Minimum height where snow should appear
    public LayerMask terrainLayer;       // Layer mask for terrain raycasting
    public float raycastDistance = 100f; // How far to raycast to find terrain

    [Header("Pooling Settings")]
    public int initialPoolSize = 30;
    public int maxPoolSize = 60;

    [Header("Snow Prewarm Settings")]
    public float prewarmTime = 5f;

    [Header("Predictive Spawning")]
    public float predictionMultiplier = 1.5f;
    public float playerVelocityInfluence = 20f;

    [Header("Visual Transition")]
    public float fadeInTime = 0.5f;
    public float fadeOutTime = 1.0f;

    private Dictionary<Vector2Int, SnowCellInfo> activeSnowSystems = new Dictionary<Vector2Int, SnowCellInfo>();
    private Queue<GameObject> snowSystemPool = new Queue<GameObject>();
    private Vector2Int lastPlayerGridPos;
    private Vector3 playerVelocity;
    private Vector3 lastPlayerPosition;

    // Class to store additional info about each snow cell
    private class SnowCellInfo
    {
        public GameObject snowObject;
        public float visibilityLevel = 1f;
        public bool markedForRemoval = false;

        public SnowCellInfo(GameObject obj)
        {
            snowObject = obj;
            visibilityLevel = 0f;
        }
    }

    void Start()
    {
        if (player == null)
            player = Camera.main.transform;

        lastPlayerPosition = player.position;
        InitializePool();
        UpdateSnowGrid();
        lastPlayerGridPos = GetGridPosition(player.position);
    }

    void InitializePool()
    {
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject snowObj = Instantiate(snowParticlePrefab, Vector3.zero, Quaternion.identity);
            SetupParticleSystem(snowObj, 0f);
            snowObj.transform.parent = transform;
            snowObj.SetActive(false);
            snowSystemPool.Enqueue(snowObj);
        }
    }

    void SetupParticleSystem(GameObject snowObj, float initialVisibility)
    {
        ParticleSystem particleSystem = snowObj.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            ParticleSystem.MainModule mainModule = particleSystem.main;
            var emission = particleSystem.emission;
            emission.enabled = true;

            ParticleSystem.MinMaxGradient startColor = mainModule.startColor;
            Color color = startColor.color;
            color.a = initialVisibility;
            startColor.color = color;
            mainModule.startColor = startColor;
        }
    }

    GameObject GetSnowSystemFromPool(Vector3 position)
    {
        if (snowSystemPool.Count > 0)
        {
            GameObject snowObj = snowSystemPool.Dequeue();
            snowObj.transform.position = position;

            PrewarmParticleSystem(snowObj);

            snowObj.SetActive(true);
            return snowObj;
        }
        else if (activeSnowSystems.Count + snowSystemPool.Count < maxPoolSize)
        {
            GameObject snowObj = Instantiate(snowParticlePrefab, position, Quaternion.identity);
            snowObj.transform.parent = transform;

            PrewarmParticleSystem(snowObj);

            snowObj.SetActive(true);
            return snowObj;
        }

        return null;
    }

    void PrewarmParticleSystem(GameObject snowObj)
    {
        ParticleSystem particleSystem = snowObj.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Stop();
            particleSystem.Clear();
            particleSystem.Simulate(prewarmTime, true, true);
            particleSystem.Play();
        }
    }

    void ReturnSnowSystemToPool(GameObject snowObj)
    {
        ParticleSystem particleSystem = snowObj.GetComponent<ParticleSystem>();
        if (particleSystem != null)
        {
            particleSystem.Stop();
            particleSystem.Clear();
        }

        snowObj.SetActive(false);
        snowSystemPool.Enqueue(snowObj);
    }

    void Update()
    {
        // Calculate player velocity for prediction
        playerVelocity = Vector3.Lerp(playerVelocity, (player.position - lastPlayerPosition) / Time.deltaTime, Time.deltaTime * 5f);
        lastPlayerPosition = player.position;

        Vector2Int currentPlayerGridPos = GetGridPosition(player.position);

        // Check if player has moved to a new grid cell
        if (currentPlayerGridPos != lastPlayerGridPos || playerVelocity.magnitude > 2f)
        {
            UpdateSnowGrid();
            lastPlayerGridPos = currentPlayerGridPos;
        }

        // Handle fade transitions
        UpdateCellVisibility();
    }

    void UpdateCellVisibility()
    {
        List<Vector2Int> cellsToRemove = new List<Vector2Int>();

        foreach (var kvp in activeSnowSystems)
        {
            Vector2Int gridPos = kvp.Key;
            SnowCellInfo cellInfo = kvp.Value;
            ParticleSystem particleSystem = cellInfo.snowObject.GetComponent<ParticleSystem>();

            if (particleSystem != null)
            {
                ParticleSystem.MainModule mainModule = particleSystem.main;
                var startColor = mainModule.startColor;
                Color color = startColor.color;

                if (cellInfo.markedForRemoval)
                {
                    // Fade out
                    cellInfo.visibilityLevel = Mathf.Max(0f, cellInfo.visibilityLevel - Time.deltaTime / fadeOutTime);

                    if (cellInfo.visibilityLevel <= 0.01f)
                    {
                        ReturnSnowSystemToPool(cellInfo.snowObject);
                        cellsToRemove.Add(gridPos);
                    }
                }
                else
                {
                    // Fade in
                    cellInfo.visibilityLevel = Mathf.Min(1f, cellInfo.visibilityLevel + Time.deltaTime / fadeInTime);
                }

                // Apply visibility
                color.a = cellInfo.visibilityLevel;
                startColor.color = color;
                mainModule.startColor = startColor;
            }
        }

        // Remove completely faded out cells
        foreach (Vector2Int gridPos in cellsToRemove)
        {
            activeSnowSystems.Remove(gridPos);
        }
    }

    Vector2Int GetGridPosition(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(worldPos.x / gridSize),
            Mathf.FloorToInt(worldPos.z / gridSize)
        );
    }

    Vector3 GetWorldPosition(Vector2Int gridPos)
    {
        // Initial position using grid coordinates
        Vector3 xzPosition = new Vector3(
            gridPos.x * gridSize + gridSize / 2,
            0,
            gridPos.y * gridSize + gridSize / 2
        );

        // Get the proper height at this position
        float yPosition = GetProperHeightAt(xzPosition);

        // Return the complete position
        return new Vector3(xzPosition.x, yPosition, xzPosition.z);
    }

    float GetProperHeightAt(Vector3 position)
    {
        // Start raycast from high above the position
        Vector3 rayStart = new Vector3(position.x, player.position.y + raycastDistance, position.z);
        RaycastHit hit;

        // Try to find the terrain height
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastDistance * 2, terrainLayer))
        {
            // Found terrain, place snow above it
            float terrainHeight = hit.point.y;

            // Check if this location is within our snow height limits
            if (terrainHeight >= minSnowHeight && terrainHeight <= maxSnowHeight)
            {
                // Return height above terrain
                return terrainHeight + heightAbovePlayer;
            }
        }

        // If no terrain found or outside snow limits, use player height
        return player.position.y + heightAbovePlayer;
    }

    void UpdateSnowGrid()
    {
        Vector2Int playerGridPos = GetGridPosition(player.position);
        HashSet<Vector2Int> cellsToActivate = new HashSet<Vector2Int>();

        // Calculate prediction direction based on player velocity
        Vector2 velocityDirection = new Vector2(playerVelocity.x, playerVelocity.z).normalized;
        Vector2 predictedOffset = velocityDirection * playerVelocity.magnitude * (playerVelocityInfluence / 100f);
        Vector3 predictedPosition = player.position + new Vector3(predictedOffset.x, 0, predictedOffset.y);
        Vector2Int predictedGridPos = GetGridPosition(predictedPosition);

        // Determine regular activation cells
        int activationCells = Mathf.CeilToInt(activationDistance / gridSize);
        for (int x = -activationCells; x <= activationCells; x++)
        {
            for (int z = -activationCells; z <= activationCells; z++)
            {
                Vector2Int gridPos = new Vector2Int(playerGridPos.x + x, playerGridPos.y + z);
                float distance = Vector2Int.Distance(gridPos, playerGridPos) * gridSize;

                if (distance <= activationDistance)
                {
                    cellsToActivate.Add(gridPos);
                }
            }
        }

        // Add predicted cells (in movement direction)
        if (playerVelocity.magnitude > 2f)
        {
            float predictiveActivationDistance = activationDistance * predictionMultiplier;
            activationCells = Mathf.CeilToInt(predictiveActivationDistance / gridSize);

            // Add additional cells in predicted direction
            for (int x = -activationCells; x <= activationCells; x++)
            {
                for (int z = -activationCells; z <= activationCells; z++)
                {
                    Vector2Int gridPos = new Vector2Int(predictedGridPos.x + x, predictedGridPos.y + z);

                    // Don't add cells that are already in the normal activation range
                    if (cellsToActivate.Contains(gridPos))
                        continue;

                    float distanceToPlayer = Vector2Int.Distance(gridPos, playerGridPos) * gridSize;
                    float distanceToPrediction = Vector2Int.Distance(gridPos, predictedGridPos) * gridSize;

                    // Only add cells that are within the prediction range but outside normal range
                    if (distanceToPlayer <= predictiveActivationDistance &&
                        distanceToPrediction <= activationDistance &&
                        distanceToPlayer > activationDistance)
                    {
                        cellsToActivate.Add(gridPos);
                    }
                }
            }
        }

        // Activate new snow systems
        foreach (Vector2Int gridPos in cellsToActivate)
        {
            if (!activeSnowSystems.ContainsKey(gridPos))
            {
                // Get position with proper height for this grid cell
                Vector3 worldPos = GetWorldPosition(gridPos);
                GameObject snowObj = GetSnowSystemFromPool(worldPos);

                if (snowObj == null)
                    continue;

                activeSnowSystems.Add(gridPos, new SnowCellInfo(snowObj));
            }
            else if (activeSnowSystems[gridPos].markedForRemoval)
            {
                // If this cell was marked for removal but is now needed again, unmark it
                activeSnowSystems[gridPos].markedForRemoval = false;
            }
            else
            {
                // Update position of existing snow systems to follow terrain height changes
                Vector3 updatedPos = GetWorldPosition(gridPos);
                activeSnowSystems[gridPos].snowObject.transform.position = updatedPos;
            }
        }

        // Mark far away snow systems for removal (will fade out)
        foreach (var kvp in activeSnowSystems)
        {
            Vector2Int gridPos = kvp.Key;
            if (!cellsToActivate.Contains(gridPos) && !kvp.Value.markedForRemoval)
            {
                float distance = Vector2Int.Distance(gridPos, playerGridPos) * gridSize;

                if (distance > deactivationDistance)
                {
                    kvp.Value.markedForRemoval = true;
                }
            }
        }
    }
}