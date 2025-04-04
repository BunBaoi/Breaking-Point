using FMODUnity;
using FMOD.Studio;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class AudioManager : MonoBehaviour
{
    [Header("Audio States")]
    [SerializeField] private List<AudioState> audioStates;
    [SerializeField] private string initialStateName;
    private Dictionary<string, AudioState> audioStatesDictionary;
    private List<EventInstance> activeSFXInstances = new List<EventInstance>();
    private EventInstance musicInstance;

    private string currentStateName;

    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class AudioState
    {
        public string stateName;
        public EventReference musicEvent;
        public EventReference[] sfxEvents;
        public bool shouldMusicLoop;
        public bool shouldSFXLoop;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        audioStatesDictionary = new Dictionary<string, AudioState>();
        foreach (var state in audioStates)
        {
            audioStatesDictionary[state.stateName] = state;
        }

        // Set the initial state
        ChangeState(initialStateName);
    }

    void Update()
    {
        // TESTING PURPOSES
        if (Input.GetKeyDown(KeyCode.C))
        {
            ChangeState("Level 1 Soundtrack");
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            ChangeState("Test1");
        }
    }

    // Change the audio state based on state name
    public void ChangeState(string newStateName)
    {
        if (audioStatesDictionary.TryGetValue(newStateName, out AudioState newState))
        {
            if (newStateName == currentStateName) return;

            StopCurrentAudio();

            currentStateName = newStateName;

            PlayMusic(newState.musicEvent, newState.shouldMusicLoop);

            PlaySFX(newState.sfxEvents, newState.shouldSFXLoop);
        }
        else
        {
            Debug.LogWarning("State not found: " + newStateName);
        }
    }

    // Play Music event
    private void PlayMusic(EventReference musicEvent, bool shouldLoop)
    {
        if (musicEvent.Guid == Guid.Empty) return;

        musicInstance = RuntimeManager.CreateInstance(musicEvent);
        musicInstance.start();

        StartCoroutine(FadeInAudio(musicInstance, 1.5f, true));

        if (shouldLoop)
        {
            StartCoroutine(LoopAudio(musicInstance, true));
        }
    }

    private void PlaySFX(EventReference[] sfxEvents, bool shouldLoop)
    {
        foreach (EventReference sfx in sfxEvents)
        {
            if (sfx.Guid == Guid.Empty) continue;

            EventInstance sfxInstance = RuntimeManager.CreateInstance(sfx);
            sfxInstance.start();

            activeSFXInstances.Add(sfxInstance);

            StartCoroutine(FadeInAudio(sfxInstance, 1.0f, false));

            if (shouldLoop)
            {
                StartCoroutine(LoopAudio(sfxInstance, false));
            }
        }
    }

    // For looping
    private IEnumerator LoopAudio(EventInstance audioInstance, bool isMusic)
    {
        while (true)
        {
            bool isPlaying = false;
            audioInstance.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state);
            if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
            {
                isPlaying = false;
            }
            else
            {
                isPlaying = true;
            }

            if (!isPlaying)
            {
                // Restart the audio when it finishes
                audioInstance.start();

                if (isMusic)
                {
                    yield return new WaitForSeconds(0.1f); // Adjust delay if needed
                }
            }

            yield return null;
        }
    }

    private void StopCurrentAudio()
    {
        if (musicInstance.isValid())
        {
            StartCoroutine(FadeOutAudio(musicInstance, 2f, true));
            musicInstance.release();
        }

        foreach (var sfxInstance in activeSFXInstances)
        {
            if (sfxInstance.isValid())
            {
                StartCoroutine(FadeOutAudio(sfxInstance, 2f, false));
                sfxInstance.release();
            }
        }

        activeSFXInstances.Clear();
    }

    private IEnumerator FadeOutAudio(EventInstance audioInstance, float fadeDuration, bool isMusic)
    {
        float startVolume = 1.0f;
        float endVolume = 0.0f;
        float elapsedTime = 0.0f;

        while (elapsedTime < fadeDuration)
        {
            float currentVolume = Mathf.Lerp(startVolume, endVolume, elapsedTime / fadeDuration);
            audioInstance.setVolume(currentVolume);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        audioInstance.setVolume(endVolume);
    }

    private IEnumerator FadeInAudio(EventInstance audioInstance, float fadeDuration, bool isMusic)
    {
        float startVolume = 0.0f;
        float endVolume = 1.0f;
        float elapsedTime = 0.0f;

        audioInstance.setVolume(startVolume);

        while (elapsedTime < fadeDuration)
        {
            float currentVolume = Mathf.Lerp(startVolume, endVolume, elapsedTime / fadeDuration);
            audioInstance.setVolume(currentVolume);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        audioInstance.setVolume(endVolume);
    }
}
