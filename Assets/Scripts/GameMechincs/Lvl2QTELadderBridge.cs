using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Lvl2QTELadderBridge : MonoBehaviour
{
    public PlayerStats playerStats;


    public GameObject PassBox;
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
        playerStats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        
        //if (WaitingKey == 0)
        //{
        //    QTEGen = Random.Range(1, 3);
        //    CountingDown = 1;
        //    StopAllCoroutines();
        //    StartCoroutine(CountDown());
        //    if (QTEGen == 1)
        //    {
        //        WaitingKey = 1;
        //        DisplayKey.GetComponent<Text>().text = "A";
        //    }
        //    if (QTEGen == 2)
        //    {
        //        WaitingKey = 1;
        //        DisplayKey.GetComponent<Text>().text = "D";
        //    }
        //}

        //if (QTEGen == 1)
        //{
        //    if (Input.anyKeyDown)
        //    {
        //        if(Input.GetButtonDown("A"))
        //        {
        //            CorrectKey = 1;
        //            StartCoroutine(KeyPressing());
        //        }
        //        else
        //        {
        //            CorrectKey = 2;
        //            StartCoroutine(KeyPressing());
                    
        //        }
        //    }
        //}
        //if (QTEGen == 2)
        //{
        //    if (Input.anyKeyDown)
        //    {
        //        if (Input.GetButtonDown("D"))
        //        {
        //            CorrectKey = 1;
        //            StartCoroutine(KeyPressing());
        //        }
        //        else
        //        {
        //            CorrectKey = 2;
        //            StartCoroutine(KeyPressing());
        //        }
        //    }
        //}

        //IEnumerator KeyPressing()
        //{
        //    QTEGen = 3;
        //    if (CorrectKey == 1)
        //    {
        //        CountingDown = 2;
        //        PassBox.GetComponent<TextMesh>().text = "Pass";
        //        yield return new WaitForSeconds(1.5f);
        //        CorrectKey = 0;
        //        PassBox.GetComponent<TextMesh>().text = "";
        //        DisplayKey.GetComponent<TextMesh>().text = "";
        //        yield return new WaitForSeconds(1.5f);
        //        WaitingKey = 0;
        //        CountingDown = 1;
        //    }
        //    if (CorrectKey == 2)
        //    {
        //        CountingDown = 2;
        //        PassBox.GetComponent<TextMesh>().text = "Failed";
        //        yield return new WaitForSeconds(1.5f);
        //        CorrectKey = 0;
        //        PassBox.GetComponent<TextMesh>().text = "";
        //        DisplayKey.GetComponent<TextMesh>().text = "";
        //        yield return new WaitForSeconds(1.5f);
        //        WaitingKey = 0;
        //        CountingDown = 1;
        //    }
        //}

        //IEnumerator CountDown()
        //{
        //    yield return new WaitForSeconds(3.5f);
        //    if (CountingDown == 1)
        //    {
        //        QTEGen = 4;
        //        CountingDown = 2;
        //        PassBox.GetComponent<TextMesh>().text = "Failed";
        //        yield return new WaitForSeconds(1.5f);
        //        CorrectKey = 0;
        //        PassBox.GetComponent<TextMesh>().text = "";
        //        DisplayKey.GetComponent<TextMesh>().text = "";
        //        yield return new WaitForSeconds(1.5f);
        //        WaitingKey = 0;
        //        CountingDown = 1;
        //    }
        //}

        if (CountDownTimer >= 0)
        {
            updateTimer();
        }
    }

    public void QTEActive()
    {

    }

    public void updateTimer()
    {
        CountDownTimer -= Time.deltaTime;
        if (CountDownTimer <= 0)
        {
            playerStats.IsAlive = false;
            Debug.Log("Player Dead");
        }
    }
}
