using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{

    public float Oxygen;
    public float Stamina;

    public float OxygenTank;

    public float OxygenDeduction;
    public float StaminaDeduction;
    public float OxygenTankDeduction;

    public bool Atmosphere;


    // Timer
    public const float TickMax = 1;
    private int Tick;
    private float TickTimer;

    // Slope Climb
    CharacterController controller;


    void Start()
    {
        {
            controller = GetComponent<CharacterController>();
            controller.slopeLimit = 45.0f;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        if(Atmosphere == true)
        {
            TickTimer += Time.deltaTime;
            if(TickTimer >= TickMax)
            {
                TickTimer -= TickMax;
                Tick++;
                Debug.Log(Tick);
                Oxygen = Oxygen - OxygenDeduction;
                OxygenTank = OxygenTank - OxygenTankDeduction;
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
