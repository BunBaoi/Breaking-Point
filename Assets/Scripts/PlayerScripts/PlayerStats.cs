using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{

    public float Oxygen;
    public float Stamina;

    public float OxygenTank;

    public float OxygenDeductionRate;
    public float StaminaDeduction;
    public float OxygenTankRefillRate;

    public bool Atmosphere;

    private PlayerMovement playerMovement;


    // Timer
    public const float TickMax = 1;
    private int Tick;
    private float TickTimer;

    // Slope Climb
    CharacterController controller;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = 45.0f;

        playerMovement = GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        DeadZone();

    }
    private void DeadZone ()
    {
        if (Atmosphere == true)
        {
            TickTimer += Time.deltaTime;
            if (TickTimer >= TickMax)
            {
                TickTimer -= TickMax;
                Tick++;
                Debug.Log(Tick);
                Oxygen = Oxygen - OxygenDeductionRate;
                if (Oxygen < 100 || OxygenTank > 0)
                {
                    Oxygen = Oxygen + OxygenTankRefillRate;
                    OxygenTank = OxygenTank - OxygenTankRefillRate;
                }
                else
                {
                    
                }


            }
            
            // PlayerSprint consume more oxygen
            if (playerMovement.IsSprint == true)
            {
                Debug.Log("Player Consumption Increase");
                OxygenDeductionRate = 12f;
            }
            else
            {
                OxygenDeductionRate = 2f;
            }



        }
    }


    public void OnTriggerEnter(Collider other)
    {
        Atmosphere = true;
        Debug.Log("Atmosphere Danger");
        
        
    }
    public void OnTriggerExit(Collider other)
    {
        Atmosphere = false;
        Debug.Log("Atmosphere Safe");
    }
}
