using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;
    private PlayerStats playerStats;
    public float walkSpeed = 12f;
    public float gravity = -9.81f;
    public bool IsSprint = false;
    public float playerHeight;

    Vector3 velocity;

    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        HandleGroundMovement();
        ApplyGravity();
        sprint();
        OxyOuputRate();
    }

    void HandleGroundMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * walkSpeed * Time.deltaTime);
    }

    void ApplyGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void sprint()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            walkSpeed = 20f;
            IsSprint = true;
        }
        else
        {
            walkSpeed = 12f;
            IsSprint = false;
        }
    }

    public void OxyOuputRate()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            playerStats.OxygenTankRefillRate++;
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            playerStats.OxygenTankRefillRate--;
        }
    }
}