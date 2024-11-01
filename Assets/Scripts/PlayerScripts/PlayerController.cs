using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public QTEvent qTEvent;
    public CharacterController controller;
    public PlayerStats playerStats;

    public Vector3 target = new Vector3 (10f, 0, 0);
    public float objectSpeed = 3;

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
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("Z Key");
            qTEvent.QTEActive();
            MoveToPosition(target);
            //transform.position = Vector3.Lerp(transform.position, target, 10);
        }
        else
        {
            Debug.Log("ERROR404");
        }
    }

    public void MoveToPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
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
