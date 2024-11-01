using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QTEvent : MonoBehaviour
{
    public GameObject UIDisplayKey;
    public TMP_Text TMP_Text;

    public Transform Player;

    //public int QTEGen;
    //public int WaitingKey;
    //public int CorrectKey;
    //public int KeyCountDownTime;



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
        //if (CountDownTimer >= 0)
        //{
        //    updateTimer();
        //}
    }

    public void FunctionToCall()
    {
        Debug.Log("Function called");
    }

    public void QTEActive()
    {
        TMP_Text.text = "Hello";
        Debug.Log("Function QTE Active called");
        Destroy(UIDisplayKey);
        Player.transform.position += 2 * Vector3.forward;
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
}
