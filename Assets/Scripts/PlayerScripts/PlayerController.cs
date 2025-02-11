using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static QTEMechanic;

public class PlayerController : MonoBehaviour
{
    public QTEvent qTEvent;
    public QTEMechanic qTEMechanic;
    public CharacterController controller;
    public PlayerStats playerStats;

    public float objectSpeed = 3;
    // private float moveDuration = 5f;
    public GameObject targetPos;
    public GameObject playerPos;



    public float speed = 12f;
    public float gravity = -9.81f;
    public bool IsSprint = false;

    public float playerHeight;

    Vector3 velocity;
    Rigidbody rb;

    //public float maxSlopeAngle;
    //private RaycastHit slopehit;

    public bool canMove = true; // Active QTE disable -> WASD during state 


    void Start()
    {
        Vector3 target = targetPos.transform.position;
    }

    void Update()
    {
        if (canMove)
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


        if (qTEMechanic.QTEMechanicScriptActive == true)
        {
            //Debug.Log("Movement Lock");
        }
    }
    public void SetMovementState(bool newCanMove)
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
        if (Input.GetKeyDown(KeyCode.F)) //CAN PRESS MULTIPLE TIMES THUS CAN AFFECT QTEvent KEY LOAD FIX -> CAN ONLY BE PRESSED ONCE WITHIN CERTAIN AREA
        {
            qTEMechanic.QTEMove();
            canMove = false;
            Debug.Log("Player Movement Locked");
            /*
            CharacterController characterController = GetComponent<CharacterController>();
            if (characterController != null) 
            {
                characterController.enabled = false;
            }
            */
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
        }
    }

}
