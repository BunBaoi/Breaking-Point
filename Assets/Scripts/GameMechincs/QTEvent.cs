using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;
using UnityEngine.Events;
using Unity.VisualScripting;
using static QTEMechanic;

// ^^^^^Check if need this USING

public class QTEvent : MonoBehaviour
{
    public static UICharInfo instance;
    [Header("UI")]
    public GameObject UIDisplayKey;
    public TMP_Text KeyText;
    public Button _Button;

    [Header("Event Actions")]
    public UnityEvent OnStart;

    public KeyCode _Key;
    public UI_ReloadButton[] KeyTextBox;
    public PlayerStats playerStats;
    public QTEMechanic qTEMechanic;

    public int WaitingKeyLoad;
    public int CorrectKeyCounter;



    [Header("QTE Timer")]
    public float CountDownTimer;
    public int MaxTimer;

    public int KeyCountDownTime;

    void Start()
    {
        //KeyTextBox = GetComponentsInChildren<UI_ReloadButton>(true);
        CountDownTimer = 8f; // Change QTE STARTING CountDownTimer HERE
    }

    void Update()
    {
        if (qTEMechanic.QTEMechanicScriptActive == true && CountDownTimer >= 0)
        {
            updateTimer();
        }
    }
    
    public void OpenreloadUI()
    {
        foreach (UI_ReloadButton button in KeyTextBox)
        {
            button.gameObject.SetActive(true);

            float randomX = Random.Range(400, 1600);
            float randomY = Random.Range(300, 900);

            button.transform.position = new Vector2(randomX, randomY);
            WaitingKeyLoad++;
        }
    }
    
    // QTE Player Timer
    public void updateTimer()
    {
        CountDownTimer -= Time.deltaTime;

        if (CountDownTimer <= 0 && WaitingKeyLoad == CorrectKeyCounter) // If correct amount key is pressed then resets timer and moves player
        {

            CountDownTimer = 8; // QTE CountDownTimer Update Timer

            WaitingKeyLoad = 0;
            CorrectKeyCounter = 0;

            qTEMechanic.QTEMove(); // Moves Player to Pos
            //qTEMechanic.QTEMechanicScriptActive = false; // Can't remember why this was needed might delete it

        }
        else if(CountDownTimer <= 0 && WaitingKeyLoad != CorrectKeyCounter)
        {
            playerStats.IsAlive = false;
            Debug.Log("Player is dead");
        }
    }
}