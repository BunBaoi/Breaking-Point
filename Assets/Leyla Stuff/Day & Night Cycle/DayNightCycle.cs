using UnityEngine;
using TMPro;

[System.Serializable]
public class SunriseSunsetTimes
{
    public int hour;
    public int minute;
}

public class DayNightCycle : MonoBehaviour
{
    [Header("Time Settings")]
    public int day = 1; // Track in-game days
    public int hours = 6;
    public int minutes = 0;
    [SerializeField] private float realSecondsPerGameMinute = 1f; // Every second increases time by 1 in-game minute

    [Header("Light Settings")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Gradient lightColourGradient;
    [SerializeField] private AnimationCurve lightIntensityCurve;

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
    [SerializeField] private Material daySkybox;
    [SerializeField] private Material sunsetSkybox;
   [SerializeField] private Material nightSkybox;
    [SerializeField] private float transitionDuration = 3f;
    [SerializeField] private float rotationSpeed = 1f;
    private float skyboxRotation = 0f;

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

    private void Start()
    {
        UpdateTimeUI();
        UpdateLighting();
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
                UpdateLighting();

                // Only update UI if the minutes are divisible by 10
                if (minutes % 10 == 0)
                {
                    UpdateTimeUI();
                }
            }
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

        if (directionalLight != null)
        {
            directionalLight.color = lightColourGradient.Evaluate(currentTotalMinutes / 1440f);
            directionalLight.intensity = lightIntensityCurve.Evaluate(currentTotalMinutes / 1440f);

            float lightRotation;
            if (currentTotalMinutes < sunriseTotalMinutes)
            {
                lightRotation = sunriseRotation;
            }
            else if (currentTotalMinutes < sunsetTotalMinutes)
            {
                float t = Mathf.InverseLerp(sunriseTotalMinutes, sunsetTotalMinutes, currentTotalMinutes);
                lightRotation = Mathf.Lerp(sunriseRotation, sunsetRotation, t);
            }
            else
            {
                float t = Mathf.InverseLerp(sunsetTotalMinutes, 1440, currentTotalMinutes);
                lightRotation = Mathf.Lerp(sunsetRotation, nightRotation, t);
            }

            directionalLight.transform.rotation = Quaternion.Euler(new Vector3(lightRotation, 170f, 0));
        }

        RenderSettings.ambientLight = ambientColourGradient.Evaluate(currentTotalMinutes / 1440f);

        // === SKYBOX SWITCHING ===
        if (currentTotalMinutes < sunriseTotalMinutes || currentTotalMinutes >= sunsetEndTotalMinutes)
        {
            // Night (before sunrise or after sunset and after the sunset period)
            RenderSettings.skybox = nightSkybox;
        }
        else if (currentTotalMinutes >= sunriseTotalMinutes && currentTotalMinutes < sunsetTotalMinutes)
        {
            // Day (between sunrise and sunset)
            RenderSettings.skybox = daySkybox;
        }
        else if (currentTotalMinutes >= sunsetTotalMinutes && currentTotalMinutes < sunsetEndTotalMinutes)
        {
            // Sunset (for the 30 minutes after sunset)
            RenderSettings.skybox = sunsetSkybox;
        }

        DynamicGI.UpdateEnvironment();

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
