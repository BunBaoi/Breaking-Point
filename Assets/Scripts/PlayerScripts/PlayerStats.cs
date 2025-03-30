using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static PlayerStats;

public class PlayerStats : MonoBehaviour
{
    [Header("Oxygen Stats")]
    [SerializeField] private float Oxygen = 100f;
    public float OxygenTank;
    [SerializeField] private float OxygenDeductionRate = 2f;
    [SerializeField] private float SprintOxygenDrainRate = 12f;
    public float OxygenTankRefillRate;
    [SerializeField] private bool isInOxygenDrainZone = false;
    [SerializeField] private Item oxygenTankItem;
    [SerializeField] private bool HasOxygenTank => inventoryManager.HasItem(oxygenTankItem); // Checks inventory

    [Header("Energy Stats")]
    [SerializeField] private float previousEnergy;
    [SerializeField] private float Energy = 100f;  // Current Energy
    [SerializeField] private float EnergyDrainRate = 5f;  // Energy drain per second in an EnergyDrain zone
    [SerializeField] private float SprintEnergyDrainRate = 10f; // Energy drain when sprinting
    [SerializeField] private bool isInEnergyDrainZone = false;
    [SerializeField] private bool energyChanged = false;

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CanvasGroup oxygenUIParent;
    [SerializeField] private CanvasGroup energyUIParent;
    [SerializeField] private CanvasGroup connectorImage;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Image oxygenRadialFill;  // Reference to the Oxygen radial fill image
    [SerializeField] private Image energyRadialFill;  // Reference to the Energy radial fill image
    [SerializeField] private TMP_Text oxygenText;
    private Coroutine oxygenFadeCoroutine;
    private Coroutine energyFadeCoroutine;
    private Coroutine connectorFadeCoroutine;

    [Header("Player Status")]
    public bool IsAlive = true;
    public bool QTEState = false;
    public PlayerStatus stateOfPlayer;

    private PlayerMovement playerMovement;
    private InventoryManager inventoryManager;
    [SerializeField] public QTEMechanicScript qTEMechanicScript;
    public QTEvent qTEvent;
    public Vector3 targetPosition;

    [Header("Timer")]
    [SerializeField] private const float TickMax = 1;
    private int Tick;
    private float TickTimer;

    CharacterController controller;

    void Start()
    {
        previousEnergy = Energy;

        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        inventoryManager = FindObjectOfType<InventoryManager>();

        controller.slopeLimit = 45.0f;

    }

    // Update is called once per frame
    void Update()
    {
        if (Energy != previousEnergy)
        {
            energyChanged = true;
        }
        else
        {
            energyChanged = false;
        }

        previousEnergy = Energy;

        // DeadZone();
        PlayerAlive();
        HandleEnergyDrain();
        HandleOxygenDrain();
        UpdateUIElements();
        HandleUITransitions();
    }

    // Fade in the canvas
    public void FadeIn()
    {
        StartCoroutine(FadeCanvas(0f, 1f));
    }

    // Fade out the canvas
    public void FadeOut()
    {
        StartCoroutine(FadeCanvas(1f, 0f));
    }

    public bool OxygenTankEquipped()
    {
        Item equippedItem = inventoryManager.GetEquippedItem();
        if (equippedItem != null && equippedItem == oxygenTankItem)
        {
            return true;
        }
        return false;
    }

    private void HandleUITransitions()
    {
        // Handle Oxygen UI Fade
        if (OxygenTankEquipped())
        {
            StartFadeUI(oxygenUIParent, true, 0.5f, ref oxygenFadeCoroutine);
        }
        else
        {
            StartFadeUI(oxygenUIParent, false, 0.5f, ref oxygenFadeCoroutine);
        }

        // Handle Energy UI Fade
        if (energyChanged)  // If the energy is draining
        {
            StartFadeUI(energyUIParent, true, 0.5f, ref energyFadeCoroutine);
        }
        else
        {
            StartFadeUI(energyUIParent, false, 0.5f, ref energyFadeCoroutine);
        }

        // Handle Connector Image Fade (only if both Oxygen and Energy UI are visible)
        if (oxygenUIParent.alpha > 0 && energyUIParent.alpha > 0)
        {
            StartFadeUI(connectorImage, true, 0.5f, ref connectorFadeCoroutine);
        }
        else
        {
            StartFadeUI(connectorImage, false, 0.5f, ref connectorFadeCoroutine);
        }
    }

    private void StartFadeUI(CanvasGroup parent, bool fadeIn, float duration, ref Coroutine coroutineReference)
    {
        // If a fade is already running for this UI element, don't start another coroutine
        if (coroutineReference != null) return;

        // Start the fade coroutine if one is not already running
        coroutineReference = StartCoroutine(FadeUI(parent, fadeIn, duration));
    }

    private IEnumerator FadeUI(CanvasGroup parent, bool fadeIn, float duration)
    {
        float targetAlpha = fadeIn ? 1f : 0f;
        float elapsedTime = 0f;

        // Fade the parent
        while (elapsedTime < duration)
        {
            parent.alpha = Mathf.Lerp(parent.alpha, targetAlpha, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        parent.alpha = targetAlpha;

        // After the coroutine finishes, set the coroutine reference to null
    if (parent == oxygenUIParent)
        oxygenFadeCoroutine = null;
    if (parent == energyUIParent)
        energyFadeCoroutine = null;
    if (parent == connectorImage)
        connectorFadeCoroutine = null;
    }

    private IEnumerator FadeCanvas(float fromAlpha, float toAlpha)
    {
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(fromAlpha, toAlpha, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        canvasGroup.alpha = toAlpha;
    }

    public enum PlayerStatus
    {
        FreeRoam,
        QTEBridge,
        RClimbing,
        DeadZone,
        QTE
    }

    void HandleEnergyDrain()
    {
        if (isInEnergyDrainZone) // If the player is inside an EnergyDrain zone
        {
            float drainRate = playerMovement.IsSprint ? SprintEnergyDrainRate : EnergyDrainRate;
            Energy -= drainRate * Time.deltaTime;
            Energy = Mathf.Max(Energy, 0); // Prevent Energy from going below 0

            if (Energy == 0)
            {
                Debug.Log("Player has no energy left");
                // add pass out here / game over
            }
        }
    }

    void HandleOxygenDrain()
    {
            if (isInOxygenDrainZone)
            {
                // If no oxygen tank is present, do not allow oxygen to drain
                if (!HasOxygenTank)
                {
                    Debug.Log("No Oxygen Tank: Oxygen drain is stopped.");
                    return;
                }

                // Oxygen deduction based on sprinting
                float currentOxygenDrainRate = playerMovement.IsSprint ? SprintOxygenDrainRate : OxygenDeductionRate;
                Oxygen -= currentOxygenDrainRate * Time.deltaTime;
                Oxygen = Mathf.Max(Oxygen, 0); // Prevent Oxygen from going below 0

                // Death
                if (Oxygen == 0 && HasOxygenTank)
                {
                    Debug.Log("Player has no oxygen left");
                    // add pass out or game over logic
                }

                if (Oxygen < 25) // This should play an effect to signify low oxygen & oxygen rate
                {
                    
                }

                // Tank replenish Oxygen
                if (OxygenTankRefillRate > OxygenTank && HasOxygenTank) // Step 1: Refill rate bigger than tank
                {
                    OxygenTankRefillRate = OxygenTank;
                }
                else if (Oxygen < 100 && OxygenTank > 0 && HasOxygenTank) // Step 2: Checks there is oxygen in tank
                {
                    Oxygen += OxygenTankRefillRate;
                    OxygenTank -= OxygenTankRefillRate;

                    // Ensure Oxygen doesn't go above 100
                    Oxygen = Mathf.Min(Oxygen, 100f);
                }
            }
        else
        {
            RefillingOxygenFromTank();
        }
    }

    void RefillingOxygenFromTank()
    {
        if (HasOxygenTank)
        {
            // If the player has an oxygen tank, refill oxygen based on what's available in the tank
            if (Oxygen < 100 && OxygenTank > 0)
            {
                // Determine the amount to replenish, which is either the remaining oxygen needed or the amount in the tank
                float oxygenNeeded = 100 - Oxygen;
                float oxygenToReplenish = Mathf.Min(oxygenNeeded, OxygenTank);

                // Refill oxygen and reduce the oxygen tank
                Oxygen += oxygenToReplenish;
                OxygenTank -= oxygenToReplenish;

                // Ensure Oxygen doesn't go above 100
                Oxygen = Mathf.Min(Oxygen, 100f);
            }
        }
    }

    public void ReplenishEnergy(float amount)
    {
        energyChanged = true;
        // Set Energy to the specified value (make sure it doesn't exceed the maximum value of 100)
        Energy = Mathf.Clamp(amount, 0f, 100f);
        Debug.Log("Energy replenished to: " + Energy);
    }

    void UpdateUIElements()
    {
        // Update the oxygen radial fill
        if (oxygenRadialFill != null)
        {
            if (HasOxygenTank)
            {
                // If the player has an oxygen tank, display oxygen based on tank amount
                oxygenRadialFill.fillAmount = Oxygen / 100f;
            }
            else
            {
                // If the player doesn't have an oxygen tank, display a warning or specific text
                oxygenRadialFill.fillAmount = 0f;
            }
        }

        // Update the energy radial fill (already handled in the existing code)
        if (energyRadialFill != null)
        {
            energyRadialFill.fillAmount = Energy / 100f;  // Update based on Energy level
        }

        // Update the oxygen text to display the whole number oxygen value
        if (oxygenText != null)
        {
            if (HasOxygenTank)
            {
                // If the player has an oxygen tank, display oxygen based on tank amount
                oxygenText.text = Mathf.RoundToInt(Oxygen).ToString();
            }
            else
            {
                // If the player doesn't have an oxygen tank, display a warning or specific text
                oxygenText.text = "No Tank";
            }
        }
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

    
    /*void DeadZone ()
    {
        if (stateOfPlayer == PlayerStatus.DeadZone)
        {
            if (!HasOxygenTank)
            {
                // Instant death if no Oxygen Tank
                IsAlive = false;
                Debug.Log("Player Died: No Oxygen Tank in Dead Zone");
                return;
            }
            // Tick Rate
            TickTimer += Time.deltaTime;
            if (TickTimer >= TickMax && IsAlive == true)
            {
                TickTimer -= TickMax;
                Tick++;

                //Oxygen Rate deduction
                Oxygen = Oxygen - OxygenDeductionRate;

                if (Oxygen < 25) // This should play an effect to signafy low oxygen & oxygen rate
                {

                }

                // Tank replanish Oxygen
                if (OxygenTankRefillRate > OxygenTank && HasOxygenTank) // Step 1: Refill rate bigger then tank
                                                       // When the Tank is less then the rate itself
                                                       // It would equal the remaing tank to the rate just so the player doesn't get extra
                {
                    OxygenTankRefillRate = OxygenTank;
                }
                else if (Oxygen < 100 && OxygenTank > 0 && HasOxygenTank) // Step 2: Checks there is oxygen in tank
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
    }*/

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Entered Trigger: {other.gameObject.name}");

        if (other.CompareTag("OxygenZone"))
        {
            stateOfPlayer = PlayerStatus.DeadZone;
            isInOxygenDrainZone = true;
            Debug.Log("Atmosphere Danger");
        }
        else if (other.CompareTag("Level2QTE.1"))
        {
            stateOfPlayer = PlayerStatus.QTE;
            Debug.Log("Level2QTE.1 Enter");
        }
        else if (other.CompareTag("EnergyDrain")) // Energy drain trigger
        {
            isInEnergyDrainZone = true;
            Debug.Log("Energy Drain Zone Entered");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"Exited Trigger: {other.gameObject.name}");

        if (other.CompareTag("OxygenZone") || other.CompareTag("Level2QTE.1"))
        {
            stateOfPlayer = PlayerStatus.FreeRoam;
            isInOxygenDrainZone = false;
            Debug.Log("Atmosphere Safe");
        }
        else if (other.CompareTag("EnergyDrain")) // Stop draining energy when leaving
        {
            isInEnergyDrainZone = false;
            Debug.Log("Exited Energy Drain Zone");
        }
    }
    
    public void PlayerAlive() // Ways of player Died
    {
        // Oxygen Death
        if (Oxygen <= 0 && OxygenTank > 0)
        {
            IsAlive = false;
            Debug.Log("Player Died From Low Oxygen Output");
        }
        else if (Oxygen <= 0 && OxygenTank <= 0)
        {
            IsAlive = false;
            Debug.Log("Player Died From Oxygen Starvation");
        }
    }

    public IEnumerator MoveCube(Vector3 targetPosition) // targetPosition = Player <-
    {
        Vector3 startPosition = qTEMechanicScript.objectPlayer.position;
        float timeElapsed = 0;
        Debug.Log(startPosition); // The start position is where the game object starts and leave off from. From testing the qte object moves starts and moves from the player to "targeted position"
        Debug.Log("Checkpoint Pos" + targetPosition); // "target" = "targetPosition"

        while (timeElapsed < qTEMechanicScript.MoTSpeed)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / qTEMechanicScript.MoTSpeed); // "startPosition" -> "targetPosition" + speed overtime
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        if (qTEMechanicScript.PositionOfPlayer != QTEMechanicScript.PlayerPos.PlayerPos4)
        {
            qTEvent.OpenreloadUI(); // PLAYING TWICE UPON QTE COMPLETION AND MOVE COMPLETION // UPDATE may not need to be fixed
            qTEMechanicScript.QTEMechanicScriptActive = true; // KEY TO ACTIVATINE TIMER 
        }
        else
        {
            qTEMechanicScript.QTEMechanicScriptActive = false;
            QTEState = false;
            qTEMechanicScript.CHKPos4 = true;
            qTEMechanicScript.playerMovement.canMove = true;
            Debug.Log("Player Movement Unlocked");
        }

    }

    // Public method to drain energy (can be called from other scripts)
    public void DrainEnergy(float amount)
    {
        energyChanged = true;
        Energy -= amount;
        Energy = Mathf.Max(Energy, 0); // Prevent Energy from going below 0

        if (Energy == 0)
        {
            Debug.Log("Player has no energy left");
            // Handle zero energy state
        }
    }

    // Public method to get energy percentage (0-100)
    public float GetEnergyPercentage()
    {
        return Energy;
    }

    // Method to check if player has enough energy
    public bool HasEnoughEnergy(float threshold)
    {
        return Energy >= threshold;
    }

}
