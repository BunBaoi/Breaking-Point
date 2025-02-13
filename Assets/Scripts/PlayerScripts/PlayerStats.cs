using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static PlayerStats;

public class PlayerStats : MonoBehaviour
{
    [Header("Oxygen Stats")]
    public float Oxygen;
    public float OxygenTank;
    public float OxygenDeductionRate;
    public float OxygenTankRefillRate;
    public float baseOxygenDeductionRate = 2f;
    public float sprintOxygenDeductionRate = 12f;
    public float climbingOxygenMultiplier = 1.5f;

    [Header("Player Status")]
    public bool IsAlive = true;
    public bool QTEState = false;
    public PlayerStatus stateOfPlayer;

    private PlayerController playerController;
    public QTEMechanic qTEMechanic;

    [Header("Timer")]
    public const float TickMax = 1;
    private int Tick;
    private float TickTimer;

    // Slope Climb // Double check if needed for slope stuff
    CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerControls = GetComponent<PlayerControls>();
        CurrentStamina = MaxStamina;
        controller.slopeLimit = 45.0f;
    }

    void Update()
    {
        DeadZone();
        PlayerAlive();
        UpdateStamina();
    }

    public enum PlayerStatus
    {
        FreeRoam,
        QTEBridge,
        RClimbing,
        DeadZone,
        QTE
    }
    // PRINT ENUM STATUS//

    public void STP()
    {
        switch (stateOfPlayer) // checks current state of player
        {
            case PlayerStatus.FreeRoam:
                Debug.Log("Status: FreeRoam");
                break;

            case PlayerStatus.QTEBridge: // Might remove not activating
                Debug.Log("Status: QTE Bridge");
                break;

            case PlayerStatus.RClimbing:
                Debug.Log("Status: RClimbing");
                break;

            case PlayerStatus.DeadZone:
                Debug.Log("Status: DeadZone");
                break;
            case PlayerStatus.QTE:
                Debug.Log("Status: DeadZone");
                break;

        }
    }


    void DeadZone ()
    {
        if (!playerControls.IsHolding() && !playerControls.IsSprint)
        {
            CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
        }
    }

    private void UpdateStamina()
    {
        if (playerControls.IsHolding())
        {
            DrainStamina(ClimbingStaminaDrain * Time.deltaTime);
        }
        else if (playerControls.IsSprint)
        {
            DrainStamina(SprintStaminaDrain * Time.deltaTime);
        }
        else
        {
            RegenerateStamina(StaminaRegenRate * Time.deltaTime);
        }
    }

    void DeadZone()
    {
        //
        if (stateOfPlayer == PlayerStatus.DeadZone)
        {
            // Tick Rate
            TickTimer += Time.deltaTime;
            if (TickTimer >= TickMax && IsAlive)
            {
                TickTimer -= TickMax;
                Tick++;

                // Oxygen Deduction Rate
                Oxygen = Oxygen - OxygenDeductionRate;

                // Tank Replanish Oxygen
                if (OxygenTankRefillRate > OxygenTank) // Step 1: Checks rate enough in Tank
                {
                    OxygenTankRefillRate = OxygenTank;
                }
                else if (Oxygen < 100 && OxygenTank > 0) //Step 2: Checks there is oxygen in Tank
                {
                    Oxygen = Oxygen + OxygenTankRefillRate;
                    OxygenTank = OxygenTank - OxygenTankRefillRate;
                }
            }

            // PlayerSprint Consume more Oxygen
            if (playerControls.IsSprint == true)
            {
                Debug.Log("Player Consumption Rate Increase");
                OxygenDeductionRate = 12f;
            }
            else
            {
                OxygenDeductionRate = 2f;
            }

        }
    }

    public void OnTriggerEnter(Collider collision)
    {
        if(collision.gameObject.tag == "Level4Zone")
        {
            stateOfPlayer = PlayerStatus.DeadZone;
            Debug.Log("Atmosphere Danger");
        }
        if (collision.gameObject.tag == "Level2QTE.1")
        {
            stateOfPlayer = PlayerStatus.QTE;
            Debug.Log("Level2QTE.1 Enter");
        }
    }
    public void OnTriggerExit(Collider collision)
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
