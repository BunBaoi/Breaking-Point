using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FMOD.Studio;
using FMODUnity;
using Unity.VisualScripting;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Oxygen Stats")]
    public float Oxygen = 100f;
    [SerializeField] private float OxygenDeductionRate = 2f;
    [SerializeField] private float SprintOxygenDrainRate = 12f;
    public float OxygenTankRefillRate;
    [SerializeField] private bool isInOxygenDrainZone = false;
    [SerializeField] private Item oxygenTankItem;
    [SerializeField] private bool HasOxygenTank => inventoryManager.HasItem(oxygenTankItem); // Checks inventory

    [Header("Oxygen FMOD Audio")]
    [SerializeField] private EventReference oxygenRange100_75;
    [SerializeField] private EventReference oxygenRange74_50;
    [SerializeField] private EventReference oxygenRange49_25;
    [SerializeField] private EventReference oxygenRange24_15;
    [SerializeField] private EventReference oxygenRange14_10;
    [SerializeField] private EventReference oxygenRange9_0;

    private EventInstance oxygenLoopInstance;
    private int currentOxygenRange = -1;

    [Header("Energy Stats")]
    [SerializeField] private float previousEnergy;
    public float Energy = 100f;  // Current Energy
    [SerializeField] private float EnergyDrainRate = 5f;  // Energy drain per second in an EnergyDrain zone
    [SerializeField] private float SprintEnergyDrainRate = 10f; // Energy drain when sprinting
    [SerializeField] private bool isInEnergyDrainZone = false;
    [SerializeField] private bool energyChanged = false;

    [Header("UI Elements")]
    [SerializeField] private CanvasGroup transitionCanvasGroup;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CanvasGroup oxygenUIParent;
    [SerializeField] private CanvasGroup energyUIParent;
    [SerializeField] private CanvasGroup connectorImage;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private Image oxygenRadialFill;  // Oxygen radial fill image
    [SerializeField] private Image energyRadialFill;  // Energy radial fill image
    [SerializeField] private TMP_Text oxygenText;
    private Coroutine oxygenFadeCoroutine;
    private Coroutine energyFadeCoroutine;
    private Coroutine connectorFadeCoroutine;

    [Header("Player Status")]
    public bool IsAlive = true;
    public bool isInCamp = false;
    public bool QTEState = false;
    public PlayerStatus stateOfPlayer;

    private PlayerMovement playerMovement;
    [SerializeField] private CameraController cameraController;
    private InventoryManager inventoryManager;
    [SerializeField] private Canvas inventoryCanvas;
    [SerializeField] private GameObject[] playerHands;
    [SerializeField] public QTEMechanicScript qTEMechanicScript;
    public QTEvent qTEvent;
    public Vector3 targetPosition;

    [Header("Timer")]
    [SerializeField] private const float TickMax = 1;
    private int Tick;
    private float TickTimer;

    CharacterController controller;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

        void Start()
    {
        previousEnergy = Energy;

        controller = GetComponent<CharacterController>();
        playerMovement = GetComponent<PlayerMovement>();
        inventoryManager = FindObjectOfType<InventoryManager>();

        controller.slopeLimit = 45.0f;

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.gameObject.SetActive(false);
        }
    }

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
        OxygenZone,
        QTE
    }

    void HandleEnergyDrain()
    {
        if (isInEnergyDrainZone)
        {
            float drainRate = playerMovement.IsSprint ? SprintEnergyDrainRate : EnergyDrainRate;
            Energy -= drainRate * Time.deltaTime;
            Energy = Mathf.Max(Energy, 0);

            if (Energy <= 0)
            {
                PlayerDeath();
            }
        }
    }

    void HandleOxygenDrain()
    {
            if (isInOxygenDrainZone)
            {
                // If no oxygen tank is present, do not allow oxygen to drain and insta death
                if (!HasOxygenTank || !IsAlive)
                {
                PlayerDeath();
                StopOxygenSound();
                return;
                }

                // Oxygen deduction based on sprinting
                float currentOxygenDrainRate = playerMovement.IsSprint ? SprintOxygenDrainRate : OxygenDeductionRate;
                Oxygen -= currentOxygenDrainRate * Time.deltaTime;
                Oxygen = Mathf.Max(Oxygen, 0);

                // Death
                if (Oxygen <= 0 && HasOxygenTank)
                {
                PlayerDeath();
                }

            HandleOxygenSound();

            // Tank replenish Oxygen
            if (Oxygen < 100 && HasOxygenTank)
            {
                Oxygen += OxygenTankRefillRate * Time.deltaTime;
                Oxygen = Mathf.Min(Oxygen, 100f);
            }
        }
        else
        {
            StopOxygenSound();
        }
    }

    void HandleOxygenSound()
    {
        int newRange = -1;

        if (Oxygen >= 75f) newRange = 5;
        else if (Oxygen >= 50f) newRange = 4;
        else if (Oxygen >= 25f) newRange = 3;
        else if (Oxygen >= 15f) newRange = 2;
        else if (Oxygen >= 10f) newRange = 1;
        else newRange = 0;

        // Check if the range has changed
        if (newRange != currentOxygenRange)
        {
            currentOxygenRange = newRange;

            if (oxygenLoopInstance.isValid())
            {
                oxygenLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
                oxygenLoopInstance.release();
            }

            EventReference soundToPlay = new EventReference();

            switch (currentOxygenRange)
            {
                case 5: soundToPlay = oxygenRange100_75; break;
                case 4: soundToPlay = oxygenRange74_50; break;
                case 3: soundToPlay = oxygenRange49_25; break;
                case 2: soundToPlay = oxygenRange24_15; break;
                case 1: soundToPlay = oxygenRange14_10; break;
                case 0: soundToPlay = oxygenRange9_0; break;
            }

            if (!soundToPlay.IsNull)
            {
                oxygenLoopInstance = FMODUnity.RuntimeManager.CreateInstance(soundToPlay);
                oxygenLoopInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(gameObject));
                oxygenLoopInstance.start();
            }
        }
        else
        {
            if (oxygenLoopInstance.isValid())
            {
                FMOD.Studio.PLAYBACK_STATE playbackState;
                oxygenLoopInstance.getPlaybackState(out playbackState);

                if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                {
                    oxygenLoopInstance.start();
                }
            }
        }
    }

    void StopOxygenSound()
    {
        if (oxygenLoopInstance.isValid())
        {
            oxygenLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            oxygenLoopInstance.release();
            currentOxygenRange = -1;
        }
    }

    public void ReplenishEnergy(float amount)
    {
        energyChanged = true;
        Energy = Mathf.Clamp(amount, 0f, 100f);
    }

    void UpdateUIElements()
    {
        // Update the oxygen radial fill
        if (oxygenRadialFill != null)
        {
            if (HasOxygenTank)
            {
                oxygenRadialFill.fillAmount = Oxygen / 100f;
            }
            else
            {
                oxygenRadialFill.fillAmount = 0f;
            }
        }

        // Update the energy radial fill
        if (energyRadialFill != null)
        {
            energyRadialFill.fillAmount = Energy / 100f;
        }

        // Update the oxygen text to display the whole number oxygen value
        if (oxygenText != null)
        {
            if (HasOxygenTank)
            {
                oxygenText.text = Mathf.RoundToInt(Oxygen).ToString();
            }
            else
            {
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

            case PlayerStatus.OxygenZone:
                Debug.Log("Status: OxygenZone");
                break;
            case PlayerStatus.QTE:
                //Debug.Log("Status: QTE");
                break;

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Entered Trigger: {other.gameObject.name}");

        if (other.CompareTag("OxygenDrain"))
        {
            stateOfPlayer = PlayerStatus.OxygenZone;
            isInOxygenDrainZone = true;
            Debug.Log("Atmosphere Danger");
        }
        if (other.CompareTag("Level2QTE.1"))
        {
            stateOfPlayer = PlayerStatus.QTE;
            //Debug.Log("Level2QTE.1 Enter");
        }
        if (other.CompareTag("EnergyDrain"))
        {
            isInEnergyDrainZone = true;
            Debug.Log("Energy Drain Zone Entered");
        }
        if (other.CompareTag("Camp"))
        {
            isInCamp = true;
            Debug.Log("Entered Camp - Safe Zone");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("OxygenDrain"))
        {
            stateOfPlayer = PlayerStatus.OxygenZone;
            isInOxygenDrainZone = true;
            Debug.Log("Atmosphere Danger");
        }
        if (other.CompareTag("Level2QTE.1"))
        {
            stateOfPlayer = PlayerStatus.QTE;
            //Debug.Log("Level2QTE.1 Enter");
        }
        if (other.CompareTag("EnergyDrain"))
        {
            isInEnergyDrainZone = true;
            Debug.Log("Energy Drain Zone Entered");
        }
        if (other.CompareTag("Camp"))
        {
            isInCamp = true;
            Debug.Log("Entered Camp - Safe Zone");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log($"Exited Trigger: {other.gameObject.name}");

        if (other.CompareTag("OxygenDrain"))
        {
            isInOxygenDrainZone = false;
            stateOfPlayer = PlayerStatus.FreeRoam;
            //Debug.Log("Exited Oxygen Drain Zone");
        }
        if (other.CompareTag("Level2QTE.1"))
        {
            stateOfPlayer = PlayerStatus.FreeRoam;
            //Debug.Log("Exited Level2QTE.1");
        }
        if (other.CompareTag("EnergyDrain"))
        {
            isInEnergyDrainZone = false;
            Debug.Log("Exited Energy Drain Zone");
        }
        if (other.CompareTag("Camp"))
        {
            isInCamp = false;
            Debug.Log("Exited Camp - Danger Zone");
        }
    }

    public void PlayerDeath()
    {
        if (!IsAlive || isInCamp) return;

        IsAlive = false;

        if (inventoryManager != null)
        {
            inventoryManager.enabled = false;
            inventoryCanvas.gameObject.SetActive(false);
        }
        if (cameraController != null)
        {
            cameraController.SetLookState(false);
        }
        if (playerMovement != null)
        {
            playerMovement.SetMovementState(false);
        }

        if (inventoryManager != null)
        {
            inventoryManager.enabled = false;
            inventoryCanvas.gameObject.SetActive(false);
        }

        if (playerHands != null)
        {
            foreach (GameObject hand in playerHands)
            {
                hand.SetActive(false);
            }
        }

        if (controller != null)
        {
            controller.enabled = false;
        }

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.gameObject.SetActive(true);
        }

        StartCoroutine(PlayerDeathSequence());
        }
    

    private IEnumerator PlayerDeathSequence()
    {
        float targetRotation = 90f;
        float rotationSpeed = 30f;

        Vector3 currentRotation = transform.rotation.eulerAngles;

        while (Mathf.Abs(Mathf.DeltaAngle(currentRotation.x, targetRotation)) > 0.1f)
        {
            float step = rotationSpeed * Time.unscaledDeltaTime;
            currentRotation.x = Mathf.MoveTowardsAngle(currentRotation.x, targetRotation, step);

            transform.rotation = Quaternion.Euler(currentRotation);
            yield return null;
        }

        currentRotation.x = targetRotation;
        transform.rotation = Quaternion.Euler(currentRotation);

        if (transitionCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(transitionCanvasGroup, 0f, 1f, 1f));
        }

        StopOxygenSound();
        Time.timeScale = 1;
        GameOverMenu.Instance.ShowGameOver();

        yield return new WaitForSecondsRealtime(0.5f);

        if (transitionCanvasGroup != null)
        {
            yield return StartCoroutine(FadeCanvasGroup(transitionCanvasGroup, 1f, 0f, 1f));
        }

        if (transitionCanvasGroup != null)
        {
            transitionCanvasGroup.gameObject.SetActive(false);
        }

        if (controller != null)
        {
            controller.enabled = true;
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float startAlpha, float endAlpha, float duration)
    {
        // Set initial alpha
        canvasGroup.alpha = startAlpha;

        float timeElapsed = 0f;
        while (timeElapsed < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, timeElapsed / duration);
            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // Ensure the final alpha value is exactly what we need
        canvasGroup.alpha = endAlpha;
    }

public IEnumerator MoveCube(Vector3 targetPosition) // targetPosition = Player <-
    {
        Vector3 startPosition = qTEMechanicScript.objectPlayer.position;
        float timeElapsed = 0;
        //Debug.Log(startPosition); // The start position is where the game object starts and leave off from. From testing the qte object moves starts and moves from the player to "targeted position"
        //Debug.Log("Checkpoint Pos" + targetPosition); // "target" = "targetPosition"

        while (timeElapsed < qTEMechanicScript.MoTSpeed)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / qTEMechanicScript.MoTSpeed); // "startPosition" -> "targetPosition" + speed overtime
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        if (qTEMechanicScript.Pos_STOP_1.tag == "QTEStop" ||
            qTEMechanicScript.Pos_STOP_2.tag == "QTEStop" ||
            qTEMechanicScript.Pos_STOP_3.tag == "QTEStop" && 
            qTEMechanicScript.PositionOfPlayer == QTEMechanicScript.PlayerPos.PlayerPos4 || 
            qTEMechanicScript.PositionOfPlayer == QTEMechanicScript.PlayerPos.PlayerPos13 ||
            qTEMechanicScript.PositionOfPlayer == QTEMechanicScript.PlayerPos.PlayerPos21) //THIS STOPS QTE BY CHANGING THE ENUM 

        {
            Debug.Log("Stop game here");
            qTEMechanicScript.QTEMechanicScriptActive = false;
            QTEState = false;
            qTEMechanicScript.playerMovement.canMove = true;

        }
        if (qTEMechanicScript.QTEMechanicScriptActive == true) // CHANGE HEARRRRRRRRRR
        {
            qTEvent.OpenreloadUI(); // PLAYING TWICE UPON QTE COMPLETION AND MOVE COMPLETION // UPDATE may not need to be fixed
            //qTEMechanicScript.QTEMechanicScriptActive = true; // KEY TO ACTIVATINE TIMER 
        }
        //else
        //{
        //    qTEMechanicScript.QTEMechanicScriptActive = false;
        //    QTEState = false;
        //    qTEMechanicScript.CHKPos4 = true;
        //    qTEMechanicScript.playerMovement.canMove = true;
        //    Debug.Log("Player Movement Unlocked");
        //}

    }

    // drain energy
    public void DrainEnergy(float amount)
    {
        energyChanged = true;
        Energy -= amount;
        Energy = Mathf.Max(Energy, 0); // Prevent Energy from going below 0

        if (Energy <= 0)
        {
            PlayerDeath();
        }
    }

    // Get energy percentage (0-100)
    public float GetEnergyPercentage()
    {
        return Energy;
    }

    // if player has enough energy
    public bool HasEnoughEnergy(float threshold)
    {
        return Energy >= threshold;
    }

}
