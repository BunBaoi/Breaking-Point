using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CompanionScript : MonoBehaviour
{

    [Header("AI Compnaion")]
    public NavMeshAgent AI;
    public Transform player;
    public float CompanionSpeed;
    public CompanionState StateOfCompanion;
    Vector3 dest;

    [Header("Companion Audio")]
    public AudioSource audioSource;


    public enum CompanionState
    {
        follow,
        idle,
        talking
    }

    void Start()
    {
        
    }


    void Update()
    {
        
    }

    public void OnTriggerEnter(Collider other)
    {
        if (Input.GetKeyUp(KeyCode.F))
        {
            StateOfCompanion = CompanionState.talking;
        }
    }


    public void FollowPlayer()
    {
        switch(StateOfCompanion)
        {
            //Follow PLayer
            case CompanionState.follow:
                dest = player.position;
                AI.destination = dest;
                Debug.Log("Following State");
                break;

            // Idle
            case CompanionState.idle:
                Debug.Log("Idle State");
                break;

            case CompanionState.talking:
                audioSource.Play();
                Debug.Log("Companion Talking");
                break;

        }
    }
}
