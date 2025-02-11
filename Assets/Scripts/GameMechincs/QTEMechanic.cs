using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QTEMechanic : MonoBehaviour
{

    public GameObject Pos1;
    public GameObject Pos2;
    public GameObject Pos3;
    public GameObject Pos4;

    private float MoTSpeed = 2f; // Player Move Speed
    public float CHKCounter = 0f;

    public QTEvent qTEvent;
    public PlayerController playerController;
    public GameObject objectPlayer;

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
        PlayerPos4
    }

    [Header("Position Cleared")]
    public bool CHKPos1;
    public bool CHKPos2;
    public bool CHKPos3;
    public bool CHKPos4;

    public void QTEMove()
    {
        switch (PositionOfPlayer)
        {
            case PlayerPos.PlayerPosIdle:
                {
                    //Debug.Log("Player Position 1");
                    if (CHKPos1 == false) // 
                    {
                        // Player move to Target position
                        Vector3 target = Pos1.transform.position;
                        PositionOfPlayer = PlayerPos.PlayerPos1; // Update Switch
                        CHKCounter++;
                        StartCoroutine(MoveCube(target));
                        CHKPos1 = true;

                    }
                    break;
                }
            case PlayerPos.PlayerPos1:
                //Debug.Log("Player Position 1");
                if (CHKPos2 == false) // 
                {
                    // Player move to Target position
                    Vector3 target = Pos2.transform.position;
                    PositionOfPlayer = PlayerPos.PlayerPos2; // Update Switch
                    CHKCounter++;
                    StartCoroutine(MoveCube(target));
                    CHKPos1 = true;
                }
                break;

            case PlayerPos.PlayerPos2:
                //Debug.Log("Player Position 2");
                if (CHKPos3 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos3.transform.position;
                    PositionOfPlayer = PlayerPos.PlayerPos3;  // Update Switch
                    CHKCounter++;
                    StartCoroutine(MoveCube(target));

                    CHKPos2 = true;
                }
                break;

            case PlayerPos.PlayerPos3:
                //Debug.Log("Player Position 3");
                if (CHKPos3 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos4.transform.position; // Update Target Position
                    PositionOfPlayer = PlayerPos.PlayerPos4; // Update Switch
                    CHKCounter++;
                    StartCoroutine(MoveCube(target));

                    CHKPos3 = true;
                }
                break;
            case PlayerPos.PlayerPos4: // QTE End transition to player free movement
                // DOUBLE TEST IF CODE UNDERNEATH IS NECCESARY!!
                if (CHKPos4 == false)
                {

                    CHKPos4 = true;

                }
                break;

        }

        IEnumerator MoveCube(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position;
            float timeElapsed = 0;
            while (timeElapsed < MoTSpeed)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / MoTSpeed);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPosition;
            //Debug.Log("Position arrived");

            if (PositionOfPlayer != PlayerPos.PlayerPos4)
            {
                qTEvent.OpenreloadUI(); // PLAYING TWICE UPON QTE COMPLETION AND MOVE COMPLETION
                QTEMechanicScriptActive = true; // KEY TO ACTIVATINE TIMER 
            }
            else
            {
                QTEMechanicScriptActive = false;
                CHKPos4 = true;
                playerController.canMove = true;
                Debug.Log("Player Movement Unlocked");
            }

        }


        Debug.Log("QTEMOVE Active");
    }
}
