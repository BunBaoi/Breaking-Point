using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QTEMechanicScript : MonoBehaviour
{
    [Header("QTE Position")]
    public GameObject Pos1;
    public GameObject Pos2;
    public GameObject Pos3;
    public GameObject Pos4;
    public GameObject Pos5;
    public GameObject Pos6;
    public GameObject Pos7;
    public GameObject Pos8;
    public GameObject Pos9;
    public GameObject Pos10;
    public GameObject Pos11;
    public GameObject Pos12;
    public GameObject Pos13;

    [Header("QTE Active/InActive")]
    public GameObject Pos_STOP;


    public float MoTSpeed = 2f; // Player Move Speed
    public float CHKCounter = 0f;

    public QTEvent qTEvent;
    public Transform objectPlayer;
    public PlayerStats playerStats;
    public PlayerMovement playerMovement;

    public PlayerPos PositionOfPlayer;
    public bool QTEMechanicScriptActive;

    public void Start()
    {
        PositionOfPlayer = PlayerPos.PlayerPosIdle;

    }

    public enum PlayerPos
    {
        PlayerPosIdle,
        PlayerPos1,
        PlayerPos2,
        PlayerPos3,
        PlayerPos4,
        PlayerPos5,
        PlayerPos6,
        PlayerPos7,
        PlayerPos8,
        PlayerPos9,
        PlayerPos10,
        PlayerPos11,
        PlayerPos12,
        PlayerPos13
    }

    [Header("Position Cleared")]
    public bool CHKPos1;
    public bool CHKPos2;
    public bool CHKPos3;
    public bool CHKPos4;
    public bool CHKPos5;
    public bool CHKPos6;
    public bool CHKPos7;
    public bool CHKPos8;
    public bool CHKPos9;
    public bool CHKPos10;
    public bool CHKPos11;
    public bool CHKPos12;
    public bool CHKPos13;

    public void QTEMove()
    {
        switch (PositionOfPlayer)
        {
            case PlayerPos.PlayerPosIdle: // Move player to first position of QTE
                if (CHKPos1 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos1.transform.position; // Update target position to Pos 1
                    PositionOfPlayer = PlayerPos.PlayerPos1; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                }
                break;

            case PlayerPos.PlayerPos1:
                if (CHKPos2 == false) // 
                {
                    // Player move to Target position
                    Vector3 target = Pos2.transform.position;// Update target position to Pos 2
                    PositionOfPlayer = PlayerPos.PlayerPos2; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target)); // Move to -> "target"
                    CHKPos1 = true;
                }
                break;

            case PlayerPos.PlayerPos2:

                if (CHKPos3 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos3.transform.position; // Update target position to Pos 3
                    PositionOfPlayer = PlayerPos.PlayerPos3;  // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos2 = true;
                }
                break;

            case PlayerPos.PlayerPos3:
                //Debug.Log("Player Position 3");
                if (CHKPos4 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos4.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos4; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos3 = true;
                }
                break;

            case PlayerPos.PlayerPos4:
                //Debug.Log("Player Position 3");
                if (CHKPos5 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos5.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos5; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos4 = true;
                }
                break;

            case PlayerPos.PlayerPos5:
                //Debug.Log("Player Position 3");
                if (CHKPos6 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos6.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos6; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos5 = true;
                }
                break;

            case PlayerPos.PlayerPos6:
                //Debug.Log("Player Position 3");
                if (CHKPos7 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos7.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos7; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos6 = true;
                }
                break;

            case PlayerPos.PlayerPos7:
                //Debug.Log("Player Position 3");
                if (CHKPos8 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos8.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos8; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos7 = true;
                }
                break;

            case PlayerPos.PlayerPos8:
                //Debug.Log("Player Position 3");
                if (CHKPos9 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos9.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos9; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos8 = true;
                }
                break;

            case PlayerPos.PlayerPos9:
                //Debug.Log("Player Position 3");
                if (CHKPos9 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos10.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos10; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos9 = true;
                }
                break;
            case PlayerPos.PlayerPos10:
                //Debug.Log("Player Position 3");
                if (CHKPos10 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos11.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos11; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos10 = true;
                }
                break;
            case PlayerPos.PlayerPos11:
                //Debug.Log("Player Position 3");
                if (CHKPos11 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos12.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos12; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos11 = true;
                }
                break;
            case PlayerPos.PlayerPos12:
                //Debug.Log("Player Position 3");
                if (CHKPos12 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos13.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos13; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos12 = true;
                }
                break;
            case PlayerPos.PlayerPos13:
                //Debug.Log("Player Position 3");
                if (CHKPos13 == false)
                {
                    // Player move to Target position
                    /*
                    Vector3 target = Pos14.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos14; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    */
                    CHKPos13 = true;
                    
                }
                break;
        }
    }

}
