using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using JetBrains.Annotations;

public class QTEvent : MonoBehaviour
{
    public GameObject UIDisplayKey;
    public TMP_Text TMP_Text;

    public Transform Player;
    public float moveObject;
    public Vector3 moveForward = new Vector3(0f, 0f, 2f);

    //public int QTEGen;
    //public int WaitingKey;
    //public int CorrectKey;
    //public int KeyCountDownTime;



    // Timer
    public float CountDownTimer = 100;
    public int MaxTimer;

    void Start()
    {

    }

    void Update()
    {
        if (CountDownTimer >= 0)
        {
            updateTimer();
        }
    }

    // QTE UI
    public void QTEActive()
    {
        TMP_Text.text = "Hello";
        Destroy(UIDisplayKey);
    }

    // QTE Player Timer
    public void updateTimer()
    {
        CountDownTimer -= Time.deltaTime;
        if (CountDownTimer <= 0)
        {
            //playerStats.IsAlive = false;
            Debug.Log("Player Dead");
        }
    }
}
