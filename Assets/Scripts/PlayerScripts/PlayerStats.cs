using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerStats : MonoBehaviour
{
    [Header("Oxygen System")]
    public float Oxygen = 100f;
    public float OxygenTank = 100f;
    public float OxygenDeductionRate;
    public float OxygenTankRefillRate;
    public float baseOxygenDeductionRate = 2f;
    public float sprintOxygenDeductionRate = 12f;
    public float climbingOxygenMultiplier = 1.5f;

    [Header("Stamina System")]
    public float MaxStamina = 100f;
    public float CurrentStamina;
    public float StaminaRegenRate = 5f;
    public float ClimbingStaminaDrain = 10f;
    public float SprintStaminaDrain = 15f;

    [Header("Status")]
    public bool Atmosphere;
    public bool IsAlive = true;
    private PlayerControls playerControls;
    public QTEMechanic qTEMechanic;

    // Timer
    public const float TickMax = 1;
    private int Tick;
    private float TickTimer;

    // Slope Climb
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

    public PlayerStatus stateOfPlayer;
    public enum PlayerStatus
    {
        FreeRoam,
        QTEBridge,
        RClimbing,
        DeadZone,
    }
    // PRINT ENUM STATUS//

    //public void STP()
    //{
    //    switch (stateOfPlayer)
    //    {
    //        case PlayerStatus.FreeRoam:
    //            Debug.Log("Status: FreeRoam");
    //            break;

    //        case PlayerStatus.QTEBridge:
    //            Debug.Log("Status: QTE Bridge");


    //            break;

    //        case PlayerStatus.RClimbing:
    //            Debug.Log("Status: RClimbing");
    //            break;

    //        case PlayerStatus.DeadZone:
    //            Debug.Log("Status: DeadZone");
    //            break;

    //    }
    //}

    public void DrainStamina(float amount)
    {
        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
    }

    public void RegenerateStamina(float amount)
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
