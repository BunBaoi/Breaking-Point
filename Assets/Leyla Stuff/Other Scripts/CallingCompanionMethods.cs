using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CallingCompanionMethods : MonoBehaviour
{
    public static CallingCompanionMethods Instance;

    [SerializeField] private CompanionScript companionScript;
    [SerializeField] private string companionTag = "";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    void Update()
    {
        if (companionScript == null)
        {
            GameObject companionObject = GameObject.FindGameObjectWithTag(companionTag);

            if (companionObject != null)
            {
                companionScript = companionObject.GetComponent<CompanionScript>();
            }
        }
    }

    public void CallFollowPlayer()
    {
        if (companionScript != null)
        {
            companionScript.FollowPlayer();
        }
    }

    public void CallTeleportToPosition(Vector3 position)
    {
        if (companionScript != null)
        {
            companionScript.TeleportToPosition(position);
        }
    }

    public void CallFacePlayer()
    {
        if (companionScript != null)
        {
            companionScript.FacePlayer();
        }
    }

    public void CallTeleportToPlayer()
    {
        if (companionScript != null)
        {
            companionScript.StartTeleportingToPlayer();
        }
    }

    public void CallSetVisibility(bool visible)
    {
        if (companionScript != null)
        {
            companionScript.SetVisibility(visible);
        }
    }

    public void CallDisappearFor(float duration)
    {
        if (companionScript != null)
        {
            companionScript.DisappearFor(duration);
        }
    }
}
