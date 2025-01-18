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




    void Start()
    {
        Vector3 target = targetPos.transform.position;
    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);
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

    private void sprint()
    {
        if(Input.GetKey(KeyCode.LeftShift))
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
        if (Input.GetKeyDown(KeyCode.F))
        {
            qTEMechanic.QTEMove();
            
            //qTEMechanic.PositionOfPlayer = PlayerPos.PlayerPos2;
            //Vector3 target = targetPos.transform.position;
            CharacterController characterController = GetComponent<CharacterController>();
            if (characterController != null) 
            {
                characterController.enabled = false;
            }
        }
    }
    
    public void OxyOuputRate()
    {
        if(Input.GetKeyDown(KeyCode.E))
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
