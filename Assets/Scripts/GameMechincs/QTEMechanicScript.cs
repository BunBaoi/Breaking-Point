using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static QTEMechanic;

public class QTEMechanicScript : MonoBehaviour
{
    [Header("QTE Position")]
    public GameObject Pos1;
    public GameObject Pos2;
    public GameObject Pos3;
    public GameObject Pos4;

    private float MoTSpeed = 2f; // Player Move Speed
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
            case PlayerPos.PlayerPosIdle: // Move player to first position of QTE
                if (CHKPos1 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos1.transform.position; // Update target position to Pos 1
                    PositionOfPlayer = PlayerPos.PlayerPos1; // Update Switch
                    CHKCounter++;
                    StartCoroutine(MoveCube(target)); // 27/02/25: Target is the Checkpoint POS

                    CHKPos1 = true;

                }
                break;

            case PlayerPos.PlayerPos1:
                if (CHKPos2 == false) // 
                {
                    // Player move to Target position
                    Vector3 target = Pos2.transform.position;// Update target position to Pos 2
                    PositionOfPlayer = PlayerPos.PlayerPos2; // Update Switch
                    CHKCounter++;
                    StartCoroutine(MoveCube(target)); // Move to -> "target"
                    CHKPos1 = true;
                }
                break;

            case PlayerPos.PlayerPos2:
                //Debug.Log("Player Position 2");
                if (CHKPos3 == false)
                {
                    // Player move to Target position
                    Vector3 target = Pos3.transform.position; // Update target position to Pos 3
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
                    Vector3 target = Pos4.transform.position; // Update target position to Pos 4
                    PositionOfPlayer = PlayerPos.PlayerPos4; // Update Switch
                    CHKCounter++;
                    StartCoroutine(MoveCube(target));

                    CHKPos3 = true;
                }
                break;


        }


        // MAJOR TESTING FOR THE IENUMERTATOR 
        //
        // 27/02/25
        // The QTE bridge is moving to the position instead of player
        // Need to change one of the properties to move the player instead of the the bridge
        IEnumerator MoveCube(Vector3 targetPosition) // targetPosition = Player <-
        {
            Vector3 startPosition = objectPlayer.position;
            float timeElapsed = 0;
            Debug.Log(startPosition); // The start position is where the game object starts and leave off from. From testing the qte object moves starts and moves from the player to "targeted position"
            Debug.Log("Checkpoint Pos" + targetPosition); // "target" = "targetPosition"
            
            while (timeElapsed < MoTSpeed)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / MoTSpeed); // "startPosition" -> "targetPosition" + speed overtime
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            if (PositionOfPlayer != PlayerPos.PlayerPos4)
            {
                qTEvent.OpenreloadUI(); // PLAYING TWICE UPON QTE COMPLETION AND MOVE COMPLETION // UPDATE may not need to be fixed
                QTEMechanicScriptActive = true; // KEY TO ACTIVATINE TIMER 
            }
            else
            {
                QTEMechanicScriptActive = false;
                playerStats.QTEState = false;
                CHKPos4 = true;
                playerMovement.canMove = true;
                Debug.Log("Player Movement Unlocked");
            }
            

        }


        //Debug.Log("QTEMOVE Active");
    }

}
