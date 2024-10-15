using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    private PlayerStats playerStats;

    public float speed = 12f;
    public float gravity = -9.81f;
    public bool IsSprint = false;

    public float playerHeight;

    Vector3 velocity;
    Rigidbody rb;

    public float maxSlopeAngle;
    private RaycastHit slopehit;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
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
    public void OxyOuputRate()
    {
        if(Input.GetKeyDown(KeyCode.Q))
        {
            playerStats.OxygenTankRefillRate++;
            //Debug.Log(playerStats.OxygenTankRefillRate);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            playerStats.OxygenTankRefillRate--;
            //Debug.Log(playerStats.OxygenTankRefillRate);
        }
    }

}
