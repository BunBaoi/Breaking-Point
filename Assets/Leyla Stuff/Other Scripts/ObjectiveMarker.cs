using UnityEngine;
using System.Collections.Generic;

public class ObjectiveMarker : MonoBehaviour
{
    [Tooltip("The prefab to use as a marker (e.g. exclamation point, icon, etc.)")]
    [SerializeField] private GameObject markerPrefab;

    [Tooltip("Offset above the object’s head")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);

    [Header("Marker Shows When ALL These Bools Are True")]
    [SerializeField] private List<string> showIfBoolsTrue = new List<string>();

    [Header("Marker Hides When ANY of These Bools Are True")]
    [SerializeField] private List<string> hideIfBoolsTrue = new List<string>();

    private GameObject instantiatedMarker;

    private bool isRegisteredWithCompass = false;

    private void Start()
    {
        if (markerPrefab != null)
        {
            instantiatedMarker = Instantiate(markerPrefab, transform);
            instantiatedMarker.transform.localPosition = offset;
        }

        if (BoolManager.Instance != null)
        {
            UpdateMarkerVisibility();
            BoolManager.Instance.OnBoolUpdated += UpdateMarkerVisibility;
        }
    }

    private void LateUpdate()
    {
        if (instantiatedMarker != null)
        {
            GameObject playerCamObj = GameObject.FindGameObjectWithTag("PlayerCamera");
            if (playerCamObj != null)
            {
                Transform camTransform = playerCamObj.transform;
                instantiatedMarker.transform.LookAt(camTransform);
            }
        }
    }


    private void OnDestroy()
    {
        if (BoolManager.Instance != null)
            BoolManager.Instance.OnBoolUpdated -= UpdateMarkerVisibility;

        if (isRegisteredWithCompass)
            CompassManager.Instance?.UnregisterObjective(transform);
    }

    private void UpdateMarkerVisibility()
    {
        if (BoolManager.Instance == null)
            return;

        bool allRequiredBoolsTrue = true;
        foreach (string key in showIfBoolsTrue)
        {
            if (!BoolManager.Instance.GetBool(key))
            {
                allRequiredBoolsTrue = false;
                break;
            }
        }

        bool anyBlockingBoolTrue = false;
        foreach (string key in hideIfBoolsTrue)
        {
            if (BoolManager.Instance.GetBool(key))
            {
                anyBlockingBoolTrue = true;
                break;
            }
        }

        bool shouldShow = allRequiredBoolsTrue && !anyBlockingBoolTrue;

        // Toggle the 3D marker
        if (instantiatedMarker != null)
            instantiatedMarker.SetActive(shouldShow);

        // Register or unregister from compass
        if (shouldShow && !isRegisteredWithCompass)
        {
            CompassManager.Instance?.RegisterObjective(transform);
            isRegisteredWithCompass = true;
        }
        else if (!shouldShow && isRegisteredWithCompass)
        {
            CompassManager.Instance?.UnregisterObjective(transform);
            isRegisteredWithCompass = false;
        }
    }
}

