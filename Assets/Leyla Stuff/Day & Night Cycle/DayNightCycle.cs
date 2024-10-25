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
    [SerializeField] private int hours = 6;
    [SerializeField] private int minutes = 0;
    [SerializeField] private float realSecondsPerGameMinute = 1f; // Every second increases time by 1 in-game minute

    [Header("Light Settings")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Gradient lightColourGradient; // Colour gradient for the light
    [SerializeField] private AnimationCurve lightIntensityCurve; // Intensity curve for the light

    [Header("Ambient Light Settings")]
    [SerializeField] private Gradient ambientColourGradient; // Gradient for ambient colours

    [Header("Sunrise and Sunset Times")]
    [SerializeField] private SunriseSunsetTimes sunriseTime; // Sunrise time
    [SerializeField] private SunriseSunsetTimes sunsetTime; // Sunset time

    [Header("Light Rotation Settings")]
    [SerializeField] private float sunriseRotation = -30f; // Rotation angle for sunrise
    [SerializeField] private float sunsetRotation = 210f; // Rotation angle for sunset
    [SerializeField] private float nightRotation = 290f; // Rotation angle for straight up at night

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;

    private float timer = 0f;
    [SerializeField] private bool isTimeRunning = true;

    private void Start()
    {
        UpdateTimeUI();
        UpdateLighting(); // Initialise lighting based on the starting time
    }

    private void Update()
    {
        if (isTimeRunning)
        {
            timer += Time.deltaTime;
            if (timer >= realSecondsPerGameMinute) // Check if time to progress by 1 in-game minute
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
        minutes++; // Increase time by 1 in-game minute every real second

        if (minutes >= 60)
        {
            minutes = 0;
            hours++;
            if (hours >= 24)
            {
                hours = 0;
            }
        }
    }

    private void UpdateLighting()
    {
        // Calculate the total minutes since midnight
        int currentTotalMinutes = hours * 60 + minutes;

        // Total minutes for sunrise and sunset
        int sunriseTotalMinutes = sunriseTime.hour * 60 + sunriseTime.minute;
        int sunsetTotalMinutes = sunsetTime.hour * 60 + sunsetTime.minute;

        // Update directional light based on time of day
        if (directionalLight != null)
        {
            directionalLight.color = lightColourGradient.Evaluate(currentTotalMinutes / 1440f); // 1440 minutes in a day
            directionalLight.intensity = lightIntensityCurve.Evaluate(currentTotalMinutes / 1440f);

            // Determine the light rotation
            float lightRotation;

            if (currentTotalMinutes < sunriseTotalMinutes)
            {
                // Before sunrise: set to sunrise rotation
                lightRotation = sunriseRotation;
            }
            else if (currentTotalMinutes < sunsetTotalMinutes)
            {
                // Between sunrise and sunset: interpolate
                float t = Mathf.InverseLerp(sunriseTotalMinutes, sunsetTotalMinutes, currentTotalMinutes);
                lightRotation = Mathf.Lerp(sunriseRotation, sunsetRotation, t);
            }
            else
            {
                // After sunset: continue rotating toward night rotation
                // Here we calculate how far it is past sunset and adjust accordingly
                float t = Mathf.InverseLerp(sunsetTotalMinutes, 1440, currentTotalMinutes);
                lightRotation = Mathf.Lerp(sunsetRotation, nightRotation, t);
            }

            // Apply the rotation to the directional light
            directionalLight.transform.rotation = Quaternion.Euler(new Vector3(lightRotation, 170f, 0));
        }

        // Update ambient light color based on time of day percentage
        RenderSettings.ambientLight = ambientColourGradient.Evaluate(currentTotalMinutes / 1440f);
    }

    private void UpdateTimeUI()
    {
        // Update the time text in HH:MM format
        timeText.text = $"{hours:00}:{minutes:00}";
    }

    // Set time
    public void SetTime(int newHours, int newMinutes)
    {
        hours = Mathf.Clamp(newHours, 0, 23);
        minutes = Mathf.Clamp(newMinutes, 0, 59);
        UpdateLighting();
        UpdateTimeUI();
    }

    // Stop time
    public void StopTime()
    {
        isTimeRunning = false;
    }

    // Start time
    public void StartTime()
    {
        isTimeRunning = true;
    }
}
