using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QTEMechanic : MonoBehaviour
{

    public GameObject Pos1;
    public GameObject Pos2;
    public GameObject Pos3;

    private float MoTSpeed = 2f; // Player Move Speed
    public float CHKCounter = 0f;

    public QTEvent qTEvent;
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
        PlayerPos3
    }

    public bool CHKPos1;
    public bool CHKPos2;
    public bool CHKPos3;

    public void QTEMove()
    {
        switch (PositionOfPlayer)
        {
            case PlayerPos.PlayerPosIdle:
                {
                //Debug.Log("Player Position 1");
                if(CHKPos1 == false) // 
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
                if(CHKPos2 == false) // 
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

                        PositionOfPlayer = PlayerPos.PlayerPos3;
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
                        Vector3 target = Pos3.transform.position;

                        PositionOfPlayer = PlayerPos.PlayerPos3;
                        CHKCounter++;
                        StartCoroutine(MoveCube(target));

                        CHKPos3 = true;
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
            qTEvent.OpenreloadUI(); // PLAYING TWICE UPON QTE COMPLETION AND MOVE COMPLETION
            QTEMechanicScriptActive = true; // KEY TO ACTIVATINE TIMER ASODNAFSJNJAFNSONJFOSNJOSJFNONAJFSNJFSNJ
        }


        Debug.Log("QTEMOVE Active");
    }
}
