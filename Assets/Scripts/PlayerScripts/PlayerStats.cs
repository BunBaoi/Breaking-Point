using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerStats : MonoBehaviour
{

    public float Oxygen;
    //public float Stamina;

    public float OxygenTank;

    public float OxygenDeductionRate;
    //public float StaminaDeduction;
    public float OxygenTankRefillRate;

    public bool Atmosphere;
    public bool IsAlive = true;
    public bool PlayerStaticState;

    private PlayerMovement playerMovement;

    private Lvl2QTELadderBridge QTELadderBridge;

    // Timer
    public const float TickMax = 1;
    private int Tick;
    private float TickTimer;

    // Slope Climb
    CharacterController controller;


    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        controller.slopeLimit = 45.0f;

    }

    // Update is called once per frame
    void Update()
    {
        DeadZone();
        PlayerAlive();

    }

    public PlayerStatus stateOfPlayer;
    public enum PlayerStatus
    {
        FreeRoam,
        QTEBridge,
        RClimbing,
        DeadZone,
    }

    public void STP()
    {
        switch (stateOfPlayer)
        {
            case PlayerStatus.FreeRoam:
                Debug.Log("Status: FreeRoam");
                break;

            case PlayerStatus.QTEBridge:
                Debug.Log("Status: QTE Bridge");
                QTELadderBridge.QTEActive();

                break;

            case PlayerStatus.RClimbing:
                Debug.Log("Status: RClimbing");
                break;

            case PlayerStatus.DeadZone:
                Debug.Log("Status: DeadZone");
                break;

        }
    }


    void DeadZone ()
    {
        //if (Atmosphere == true)
        if (stateOfPlayer == PlayerStatus.DeadZone)
        {
            // Tick Rate
            TickTimer += Time.deltaTime;
            if (TickTimer >= TickMax && IsAlive == true)
            {
                TickTimer -= TickMax;
                Tick++;

                //Oxygen Rate deduction
                Oxygen = Oxygen - OxygenDeductionRate;

                // Tank replanish Oxygen
                if (OxygenTankRefillRate > OxygenTank) // Step 1: Checks rate enough in tank
                {
                    OxygenTankRefillRate = OxygenTank;
                }
                else if (Oxygen < 100 && OxygenTank > 0) // Step 2: Checks there is oxygen in tank
                {
                    Oxygen = Oxygen + OxygenTankRefillRate;
                    OxygenTank = OxygenTank - OxygenTankRefillRate;
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
        stateOfPlayer = PlayerStatus.DeadZone;
        Debug.Log("Atmosphere Danger");
        
        
    }
    public void OnTriggerExit(Collider other)
    {
        stateOfPlayer = PlayerStatus.FreeRoam;
        Debug.Log("Atmosphere Safe");
    }

    public void PlayerAlive()
    {
        if (Oxygen <= 0)
        {
            IsAlive = false;
            Debug.Log("Player Died From Oxygen Starvation");
        }
    }
}
