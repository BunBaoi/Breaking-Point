using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QTEvent : MonoBehaviour
{
    //public PlayerStats playerStats;
    public GameObject DisplayKey;

    public int QTEGen;
    public int WaitingKey;
    public int CorrectKey;
    public int KeyCountDownTime;



    // Timer
    public float CountDownTimer = 100;
    public int MaxTimer;

    void Start()
    {
        //QTEText = GetComponent<TextMeshProUGUI>();
        //QTEActive();
    }

    void Update()
    {
        if (CountDownTimer >= 0)
        {
            updateTimer();
        }
    }

    public void FunctionToCall()
    {
        Debug.Log("Function called");
    }

    public void QTEActive()
    {
        //QTEText.text = "Hello";
        //DisplayKey.GetComponent<TMP_Text.>;
        Debug.Log("Function QTE Active called");
        //Destroy(DisplayKey);

    }

    public void updateTimer()
    {
        CountDownTimer -= Time.deltaTime;
        if (CountDownTimer <= 0)
        {
            //playerStats.IsAlive = false;
            Debug.Log("Player Dead");
        }
    }
    public void printcall()
    {
        print("Print call");
    }
}
