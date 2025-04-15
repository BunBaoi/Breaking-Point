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
        PlayerPos7
    }

    [Header("Position Cleared")]
    public bool CHKPos1;
    public bool CHKPos2;
    public bool CHKPos3;
    public bool CHKPos4;
    public bool CHKPos5;
    public bool CHKPos6;
    public bool CHKPos7;

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
                if (CHKPos3 == false)
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
                if (CHKPos4 == false)
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
                if (CHKPos4 == false)
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
                if (CHKPos4 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos7.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos7; // Update Switch
                    CHKCounter++;
                    StartCoroutine(playerStats.MoveCube(target));
                    CHKPos6 = true;
                }
                break;
        }
    }

}
