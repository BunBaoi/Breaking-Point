using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SunriseSunsetTimes
{
    public int hour;
    public int minute;
}

public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance;

    [Header("Time Settings")]
    public int day = 1; // Track in-game days
    public int hours = 6;
    public int minutes = 0;
    [SerializeField] private float realSecondsPerGameMinute = 1f; // Every second increases time by 1 in-game minute
    private float currentLightRotation;

    [Header("Light Settings")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Gradient lightColourGradient;
    [SerializeField] private AnimationCurve lightIntensityCurve;

    [Header("Lantern Light Settings")]
    [SerializeField] private LayerMask lanternLayerMask;
    [SerializeField] private Light[] lanternLights;

    [Header("Ambient Light Settings")]
    [SerializeField] private Gradient ambientColourGradient;

    [Header("Sunrise and Sunset Times")]
    [SerializeField] private SunriseSunsetTimes sunriseTime;
    [SerializeField] private SunriseSunsetTimes sunsetTime;

    [Header("Light Rotation Settings")]
    [SerializeField] private float sunriseRotation = -30f;
    [SerializeField] private float sunsetRotation = 210f;
    [SerializeField] private float nightRotation = 290f;

    [Header("Skybox Settings")]
    [SerializeField] private Material sunriseSkybox;
    [SerializeField] private Material daySkybox;
    [SerializeField] private Material sunsetSkybox;
   [SerializeField] private Material nightSkybox;
    [SerializeField] private float transitionDuration = 1f;
    [SerializeField] private float rotationSpeed = 1f;
    private float skyboxRotation = 0f;
    private enum SkyState { Sunrise, Day, Sunset, Night }
    private SkyState currentSkyState;
    private SkyState targetSkyState;
    [SerializeField] private bool isTransitioningSkybox = false;
    [SerializeField] private float transitionTimer = 0f;
    private float exposureValue = 1f;
    [SerializeField] private float transitionExposure = 0f; // The value exposure goes to during skybox change
    [SerializeField] private float endingExposure = 1f;

    [Header("Fog Settings")]
    [SerializeField] private Color fogColorAtSunset = Color.gray;
    [SerializeField] private float fogDensityAtSunset = 0.5f;
    [SerializeField] private float fogDensityAtSunrise = 0f;
    [SerializeField] private float fogFadeDuration = 5f;
    private float currentFogDensity = 0f; // Current fog density
    private Color currentFogColor = Color.clear; // Current fog colour

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;

    private float timer = 0f;
    [SerializeField] private bool isTimeRunning = true;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (directionalLight != null)
        {
            RenderSettings.sun = directionalLight;
        }

        UpdateLighting();

        SetInitialSkyboxState();

        FindLanternLights();
    }

    private void SetInitialSkyboxState()
    {
        int currentTotalMinutes = hours * 60 + minutes;
        int sunriseTotalMinutes = sunriseTime.hour * 60 + sunriseTime.minute;
        int sunsetTotalMinutes = sunsetTime.hour * 60 + sunsetTime.minute;
        int sunsetEndTotalMinutes = sunsetTotalMinutes + 30;
        int sunriseEndTotalMinutes = sunriseTotalMinutes + 30;

        if (currentTotalMinutes < sunriseTotalMinutes || currentTotalMinutes >= sunsetEndTotalMinutes)
        {
            currentSkyState = SkyState.Night;
            RenderSettings.skybox = nightSkybox;
        }
        else if (currentTotalMinutes >= sunriseTotalMinutes && currentTotalMinutes < sunriseEndTotalMinutes)
        {
            currentSkyState = SkyState.Sunrise;
            RenderSettings.skybox = sunriseSkybox;
        }
        else if (currentTotalMinutes >= sunriseEndTotalMinutes && currentTotalMinutes < sunsetTotalMinutes)
        {
            currentSkyState = SkyState.Day;
            RenderSettings.skybox = daySkybox;
        }
        else
        {
            currentSkyState = SkyState.Sunset;
            RenderSettings.skybox = sunsetSkybox;
        }

        // Optional: set default exposure and rotation
        RenderSettings.skybox.SetFloat("_Exposure", exposureValue);
        RenderSettings.skybox.SetFloat("_Rotation", skyboxRotation);
    }

    private void Start()
    {
        if (directionalLight != null)
        {
            RenderSettings.sun = directionalLight;
        }

        UpdateTimeUI();
        UpdateLighting();

        targetSkyState = SkyState.Sunrise;
        RenderSettings.skybox = sunriseSkybox;
    }

    private void Update()
    {
        if (isTimeRunning)
        {
            timer += Time.deltaTime;
            if (timer >= realSecondsPerGameMinute)
            {
                timer = 0f;
                ProgressTime();

                // Only update UI if the minutes are divisible by 10
                if (minutes % 10 == 0)
                {
                    UpdateTimeUI();
                }
            }
        }
        UpdateLighting();
        HandleLanternLights();
    }

    private void HandleLanternLights()
    {
        int currentTotalMinutes = hours * 60 + minutes;
        int lightOffTimeMinutes = 7 * 60 + 30; // 7:30 AM
        int lightOnTimeMinutes = 16 * 60;      // 4:00 PM

        // Enable lights from 16:00 (4:00 PM) until 7:30 AM next day
        if (currentTotalMinutes >= lightOnTimeMinutes || currentTotalMinutes < lightOffTimeMinutes)
        {
            ToggleLanternLights(true);
        }
        else
        {
            ToggleLanternLights(false);
        }
    }

    private void FindLanternLights()
    {
        // Find all lights in the scene
        Light[] allLights = FindObjectsOfType<Light>();

        // Filter only the ones on the Lantern layer
        lanternLights = System.Array.FindAll(allLights, light => ((1 << light.gameObject.layer) & lanternLayerMask.value) != 0);

        // If nothing is found, keep it null
        if (lanternLights == null || lanternLights.Length == 0)
        {
            lanternLights = null;
        }
    }

    private void ToggleLanternLights(bool enable)
    {
        if (lanternLights == null || lanternLights.Length == 0)
            return; // No lanterns found, nothing to do

        foreach (var lanternLight in lanternLights)
        {
            if (lanternLight != null) // Check in case something got destroyed
                lanternLight.enabled = enable;
        }
    }

    private void ProgressTime()
    {
        minutes++;

        if (minutes >= 60)
        {
            minutes = 0;
            hours++;
            if (hours >= 24)
            {
                hours = 0;
                day++; // Increment day when time resets
            }
        }
    }

    public void UpdateLighting()
    {
        int currentTotalMinutes = hours * 60 + minutes;
        int sunriseTotalMinutes = sunriseTime.hour * 60 + sunriseTime.minute;
        int sunsetTotalMinutes = sunsetTime.hour * 60 + sunsetTime.minute;
        int sunsetEndTotalMinutes = sunsetTotalMinutes + 30;
        int sunriseEndTotalMinutes = sunriseTotalMinutes + 30;

        if (directionalLight != null)
        {
            directionalLight.color = lightColourGradient.Evaluate(currentTotalMinutes / 1440f);
            directionalLight.intensity = lightIntensityCurve.Evaluate(currentTotalMinutes / 1440f);

            float targetRotation;

            if (currentTotalMinutes < sunriseTotalMinutes)
            {
                targetRotation = sunriseRotation;
            }
            else if (currentTotalMinutes < sunsetTotalMinutes)
            {
                float t = Mathf.InverseLerp(sunriseTotalMinutes, sunsetTotalMinutes, currentTotalMinutes);
                targetRotation = Mathf.Lerp(sunriseRotation, sunsetRotation, t);
            }
            else
            {
                float t = Mathf.InverseLerp(sunsetTotalMinutes, 1440, currentTotalMinutes);
                targetRotation = Mathf.Lerp(sunsetRotation, nightRotation, t);
            }

            // Smoothly rotate the directional light
            currentLightRotation = Mathf.LerpAngle(currentLightRotation, targetRotation, Time.deltaTime * rotationSpeed);
            directionalLight.transform.rotation = Quaternion.Euler(new Vector3(currentLightRotation, 170f, 0));
        }

    RenderSettings.ambientLight = ambientColourGradient.Evaluate(currentTotalMinutes / 1440f);

        // === SKYBOX SWITCHING ===
        if (currentTotalMinutes < sunriseTotalMinutes || currentTotalMinutes >= sunsetEndTotalMinutes)
        {
            targetSkyState = SkyState.Night;
        }
        else if (currentTotalMinutes >= sunriseTotalMinutes && currentTotalMinutes < sunriseEndTotalMinutes)
        {
            targetSkyState = SkyState.Sunrise;
        }
        else if (currentTotalMinutes >= sunriseEndTotalMinutes && currentTotalMinutes < sunsetTotalMinutes)
        {
            targetSkyState = SkyState.Day;
        }
        else
        {
            targetSkyState = SkyState.Sunset;
        }

        // Debug.Log($"Current Time: {currentTotalMinutes}, Target: {targetSkyState}, Current: {currentSkyState}");

        if (targetSkyState != currentSkyState && !isTransitioningSkybox)
        {
            Debug.Log("Starting skybox transition...");
            isTransitioningSkybox = true;
            transitionTimer = 0f;
        }

        // Run transition
        if (isTransitioningSkybox)
        {
            transitionTimer += Time.deltaTime;

            if (transitionTimer < transitionDuration / 2f)
            {
                exposureValue = Mathf.Lerp(endingExposure, transitionExposure, transitionTimer / (transitionDuration / 2f));
            }
            else if (transitionTimer >= transitionDuration / 2f && transitionTimer < transitionDuration)
            {
                // Change skybox at halfway point
                if (currentSkyState != targetSkyState)
                {
                    Debug.Log($"Switching skybox to: {targetSkyState}");

                    switch (targetSkyState)
                    {
                        case SkyState.Sunrise: RenderSettings.skybox = sunriseSkybox; break;
                        case SkyState.Day: RenderSettings.skybox = daySkybox; break;
                        case SkyState.Sunset: RenderSettings.skybox = sunsetSkybox; break;
                        case SkyState.Night: RenderSettings.skybox = nightSkybox; break;
                    }

                    currentSkyState = targetSkyState;
                }

                float t = (transitionTimer - transitionDuration / 2f) / (transitionDuration / 2f);
                exposureValue = Mathf.Lerp(transitionExposure, endingExposure, t);
            }
            else
            {
                isTransitioningSkybox = false;
                transitionTimer = 0f;
                exposureValue = endingExposure;
                Debug.Log("Skybox transition complete.");
            }

            RenderSettings.skybox.SetFloat("_Exposure", exposureValue);
        }
        else
        {
            RenderSettings.skybox.SetFloat("_Exposure", exposureValue);
        }

        // Update skybox rotation based on the in-game time
        skyboxRotation += rotationSpeed * (Time.deltaTime / realSecondsPerGameMinute);

        // Clamp the skybox rotation to stay within 0 to 360 degrees
        if (skyboxRotation >= 360f)
        {
            skyboxRotation -= 360f;
        }

        // Apply the rotation to the skybox
        RenderSettings.skybox.SetFloat("_Rotation", skyboxRotation);

        // === FOG CONTROL ===
        float fadeDuration = fogFadeDuration; // Duration for the fade effect (in seconds)
        float fadeDurationInMinutes = fadeDuration / 60f; // Convert fade duration to minutes

        // Daytime: From Sunrise to Sunset, fog density stays at the sunrise value.
        if (currentTotalMinutes >= sunriseTotalMinutes && currentTotalMinutes < sunsetTotalMinutes)
        {
            // Gradually transition to sunrise fog density
            float fadeFactor = Mathf.InverseLerp(sunriseTotalMinutes, sunriseTotalMinutes + fadeDurationInMinutes, currentTotalMinutes);
            currentFogDensity = Mathf.Lerp(fogDensityAtSunrise, fogDensityAtSunrise, fadeFactor); // Always stays at sunrise density
            currentFogColor = Color.Lerp(Color.clear, Color.clear, fadeFactor); // Always stays clear
        }
        // Sunset: From Sunset to Sunrise, fog density stays at the sunset value.
        else if (currentTotalMinutes >= sunsetTotalMinutes || currentTotalMinutes < sunriseTotalMinutes)
        {
            // Gradually transition to sunset fog density
            float fadeFactor = Mathf.InverseLerp(sunsetTotalMinutes, sunsetTotalMinutes + fadeDurationInMinutes, currentTotalMinutes);
            currentFogDensity = Mathf.Lerp(fogDensityAtSunset, fogDensityAtSunset, fadeFactor); // Always stays at sunset density
            currentFogColor = Color.Lerp(fogColorAtSunset, fogColorAtSunset, fadeFactor); // Always stays at sunset color
        }

        // Apply the calculated fog density and color
        RenderSettings.fogDensity = currentFogDensity;
        RenderSettings.fogColor = currentFogColor;

        DynamicGI.UpdateEnvironment();
    }

        public void UpdateTimeUI()
    {
        timeText.text = $"Day {day}\n{hours:00}:{minutes:00}";
    }

    public void SetTime(int newHours, int newMinutes, bool newDay)
    {
        int previousTotalMinutes = (hours * 60) + minutes;
        int newTotalMinutes = (newHours * 60) + newMinutes;

        hours = Mathf.Clamp(newHours, 0, 23);
        minutes = Mathf.Clamp(newMinutes, 0, 59);

        // If newDay is true, always increment day
        // If newDay is false, only increment day if time wraps past midnight
        if (newDay || newTotalMinutes < previousTotalMinutes)
        {
            day++;
        }

        UpdateLighting();
        UpdateTimeUI();
    }

    public void StopTime()
    {
        isTimeRunning = false;
    }

    public void StartTime()
    {
        isTimeRunning = true;
    }
}
