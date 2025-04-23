using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CompassManager : MonoBehaviour
{
    public static CompassManager Instance;

    [SerializeField] private RectTransform compassRect; // Scroll view content Rect
    [SerializeField] private GameObject compassMarkerPrefab;
    [SerializeField] private float compassWidth = 2000f; // Full scroll rect width representing 360 degrees
    [SerializeField] private Transform playerCamera;

    private Dictionary<Transform, RectTransform> objectiveMarkers = new Dictionary<Transform, RectTransform>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Update()
    {
        if (playerCamera == null) return;

        float playerYaw = playerCamera.eulerAngles.y;

        foreach (var pair in objectiveMarkers)
        {
            Transform objective = pair.Key;
            RectTransform marker = pair.Value;

            Vector3 directionToObjective = objective.position - playerCamera.position;
            float angleToObjective = Quaternion.LookRotation(directionToObjective).eulerAngles.y;

            float deltaAngle = Mathf.DeltaAngle(playerYaw, angleToObjective); // -180 to 180

            // Map angle to X position
            float normalized = deltaAngle / 180f;
            float xPos = normalized * (compassWidth / 2f);

            // Assign position relative to center of compass
            marker.anchoredPosition = new Vector2(xPos, marker.anchoredPosition.y);
        }

        // Center the scroll view on playerYaw
        float scrollNormalized = playerYaw / 360f; // 0 to 1
        ScrollRect scroll = compassRect.GetComponentInParent<ScrollRect>();
        if (scroll != null)
        {
            scroll.horizontalNormalizedPosition = scrollNormalized;
        }
    }

    public void RegisterObjective(Transform objective)
    {
        if (objectiveMarkers.ContainsKey(objective)) return;

        GameObject markerGO = Instantiate(compassMarkerPrefab, compassRect);
        RectTransform markerRT = markerGO.GetComponent<RectTransform>();
        objectiveMarkers.Add(objective, markerRT);
    }

    public void UnregisterObjective(Transform objective)
    {
        if (objectiveMarkers.TryGetValue(objective, out RectTransform marker))
        {
            Destroy(marker.gameObject);
            objectiveMarkers.Remove(objective);
        }
    }
}