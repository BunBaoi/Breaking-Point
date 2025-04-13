using UnityEngine;
using Cinemachine;
using System.Collections.Generic;

public class RaycastController : MonoBehaviour
{
    public string playerCameraTag = "PlayerCamera";
    public string gameplayCamTag = "PlayerVirtualCamera";
    public string[] raycastScriptNames; // script names here

    private CinemachineBrain brain;
    private MonoBehaviour[] raycastScripts;

    void Start()
    {
        GameObject cameraObj = GameObject.FindGameObjectWithTag(playerCameraTag);
        if (cameraObj != null)
            brain = cameraObj.GetComponent<CinemachineBrain>();

        if (brain == null)
            Debug.LogWarning("CinemachineBrain not found on tagged PlayerCamera.");
    }

    void Update()
    {
        if (brain.ActiveVirtualCamera != null)
        {
            GameObject liveCamGO = brain.ActiveVirtualCamera.VirtualCameraGameObject;
            bool isGameplayCam = liveCamGO.CompareTag(gameplayCamTag);

            // Only find if not already found (or re-find if needed)
            if (raycastScripts == null || raycastScripts.Length == 0)
            {
                raycastScripts = FindRaycastScriptsByNames();
            }

            foreach (var script in raycastScripts)
            {
                if (script != null)
                    script.enabled = isGameplayCam;
            }
        }
    }

    MonoBehaviour[] FindRaycastScriptsByNames()
    {
        var allBehaviours = FindObjectsOfType<MonoBehaviour>();
        List<MonoBehaviour> found = new List<MonoBehaviour>();

        foreach (var behaviour in allBehaviours)
        {
            string scriptName = behaviour.GetType().Name;
            foreach (var targetName in raycastScriptNames)
            {
                if (scriptName == targetName)
                {
                    found.Add(behaviour);
                    break;
                }
            }
        }

        return found.ToArray();
    }
}
