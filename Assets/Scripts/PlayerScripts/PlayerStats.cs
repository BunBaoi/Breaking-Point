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
        playerMovement = GetComponent<PlayerMovement>();
        CurrentStamina = MaxStamina;
        controller.slopeLimit = 45.0f;
    }

    void Update()
    {
        if (IsAlive)
        {
            DeadZone();
            UpdateStamina();
        }
        PlayerAlive();

    }

    public void DrainStamina(float amount)
    {
        CurrentStamina = Mathf.Max(0f, CurrentStamina - amount);
    }

    public void RegenerateStamina(float amount)
    {
        if (!playerMovement.IsHolding() && !playerMovement.IsSprint)
        {
            CurrentStamina = Mathf.Min(MaxStamina, CurrentStamina + amount);
        }
    }

    private void UpdateStamina()
    {
        if (playerMovement.IsHolding())
        {
            DrainStamina(ClimbingStaminaDrain * Time.deltaTime);
        }
        else if (playerMovement.IsSprint)
        {
            DrainStamina(SprintStaminaDrain * Time.deltaTime);
        }
        else
        {
            RegenerateStamina(StaminaRegenRate * Time.deltaTime);
        }
    }

    private void DeadZone()
    {
        if (Atmosphere)
        {
            TickTimer += Time.deltaTime;
            if (TickTimer >= TickMax && IsAlive)
            {
                TickTimer -= TickMax;
                Tick++;

                float currentOxygenRate = playerMovement.IsSprint ? sprintOxygenDeductionRate : baseOxygenDeductionRate;
                if (playerMovement.IsHolding())
                {
                    currentOxygenRate *= climbingOxygenMultiplier;
                }

                Oxygen -= currentOxygenRate;

                if (OxygenTankRefillRate > OxygenTank)
                {
                    OxygenTankRefillRate = OxygenTank;
                }
                else if (Oxygen < 100 && OxygenTank > 0)
                {
                    float oxygenToAdd = Mathf.Min(OxygenTankRefillRate, 100f - Oxygen);
                    Oxygen += oxygenToAdd;
                    OxygenTank -= oxygenToAdd;
                }
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
