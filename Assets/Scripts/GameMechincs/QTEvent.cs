using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;
using UnityEngine.Events;
using Unity.VisualScripting;
using static QTEMechanic;

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

    //public int QTEGen;
    public int WaitingKeyLoad;
    public int CorrectKeyCounter;
    //public int KeyCountDownTime;

    public PlayerStats playerStats;
    public QTEMechanic qTEMechanic;

    [Header("QTE Timer")]
    public float CountDownTimer;
    public int MaxTimer;

    //public int QTEGen;
    public int KeyCountDownTime;

    void Start()
    {
        CountDownTimer = 5;
        KeyTextBox = GetComponentsInChildren<UI_ReloadButton>(true);
    }

    void Update()
    {
        if (qTEMechanic.QTEMechanicScriptActive == true)
        {
            if (CountDownTimer >= 0)
            {
                updateTimer();
            }
        }


        //if(Input.GetKeyDown(_Key))
        //{
        //    // Click on Button
        //    _Button.onClick.Invoke();
        //}

        if (Input.GetKeyUp(KeyCode.R))
        {
            OpenreloadUI();
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
            //print("Counter KeyTextBox " + WaitingKeyLoad++);
            WaitingKeyLoad++;
        }


    }

    // QTE Player Timer
    public void updateTimer()
    {
        CountDownTimer -= Time.deltaTime;
        // WORKING CODE 

        if (CountDownTimer <= 0 && WaitingKeyLoad == CorrectKeyCounter)
        {
            //KeyChecker(); // Checks key before starting to reload 

            CountDownTimer = 5; // QTE CountDownTimer
            //playerStats.IsAlive = false;

            WaitingKeyLoad = 0;
            //Debug.Log("Reset Waiting Key Load " + WaitingKeyLoad);
            CorrectKeyCounter = 0;
            //Debug.Log("Reset Correct Key Counter " + CorrectKeyCounter);

            //OpenreloadUI();
            qTEMechanic.QTEMove(); // Check player need to move when QTE success 
            qTEMechanic.QTEMechanicScriptActive = false;

        }
        //*/
        /*
        // TEMPORALY CODE //
        if (CountDownTimer <= 0)
        {
            CountDownTimer = 10;
            //playerStats.IsAlive = false;

            WaitingKeyLoad = 0;
            Debug.Log("Reset Waiting Key Load " + WaitingKeyLoad);
            CorrectKeyCounter = 0;
            Debug.Log("Reset Correct Key Counter " + CorrectKeyCounter);

            OpenreloadUI();
            //qTEMechanic.QTEMove(); // Check player need to move when QTE success
        }
        /*
        else if(CountDownTimer <= 0 && CorrectKeyCounter != WaitingKeyLoad)
        {
            Debug.Log("Player failed QTE");
        }
        */
    }
}