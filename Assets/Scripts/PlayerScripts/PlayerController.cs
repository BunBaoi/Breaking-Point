using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static PlayerStats;
//using static QTEMechanic;

public class PlayerController : MonoBehaviour
{
    public QTEvent qTEvent;
    public QTEMechanic qTEMechanic;
    public CharacterController controller;
    public PlayerStats playerStats;

    public GameObject targetPos;
    public GameObject playerPos;

    public float objectSpeed = 3;
    public float speed = 12f;
    public float gravity = -9.81f;
    public bool IsSprint = false;
    public float playerHeight;

    Vector3 velocity;
    Rigidbody rb;

    public bool canMove = true; // Active QTE disable -> WASD during QTE state 


    void Start()
    {
        Vector3 target = targetPos.transform.position;
    }

    void Update()
    {
        if (canMove) // Boolean for player movement
        {
            float x = Input.GetAxis("Horizontal");
            float z = Input.GetAxis("Vertical");

            Vector3 move = transform.right * x + transform.forward * z;

            controller.Move(move * speed * Time.deltaTime);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        sprint();
        OxyOuputRate();
        QTEControl();

    }
    public void SetMovementState(bool newCanMove) // Boolean for player movement
    {
        canMove = newCanMove;
    }

    private void sprint()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = 20f;
            IsSprint = true;
        }
        else
        {
            speed = 12f;
            IsSprint = false;
        }

    }

    public void QTEControl()
    {
        if (Input.GetKeyDown(KeyCode.F) && playerStats.stateOfPlayer == PlayerStatus.QTE)
        {
            qTEMechanic.QTEMove();
            canMove = false;
            Debug.Log("Player Movement Locked");
            playerStats.QTEState = true;

        }
    }

    public void OxyOuputRate()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            playerStats.OxygenTankRefillRate++;
            //Debug.log("Rate Up");
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            playerStats.OxygenTankRefillRate--;
            //Debug.log("Rate Down");

            // Need to add condition that the rate can't go negative

        }
    }

}
