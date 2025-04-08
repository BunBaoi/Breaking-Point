using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private CinematicSequence chapter1CinematicSequence;
    [SerializeField] private CinematicSequence chapter2CinematicSequence;
    [SerializeField] private CinematicSequence chapter3CinematicSequence;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Clear all PlayerPrefs
            // PlayerPrefs.DeleteAll();
        }
    }

    void Start()
    {
      
    }

    void Update()
    {
        
    }

    public void LoadLevel1Cinematic()
    {
        chapter1CinematicSequence.StartCinematic();
    }

    public void LoadLevel2Cinematic()
    {
        chapter2CinematicSequence.StartCinematic();
    }

    public void LoadLevel3Cinematic()
    {
        chapter3CinematicSequence.StartCinematic();
    }
}
