using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using UnityEngine;

public class QTEMechanic : MonoBehaviour
{

    public GameObject Pos1;
    public GameObject Pos2;
    public GameObject Pos3;

    public float QTEMoveSpeed = 5f;
    public float CHKCounter = 0f;

    public GameObject objectPlayer;
    public Vector3 PlayerPos1;

    public Vector3 target = new Vector3(0, 2, 2);


    // Start is called before the first frame update
    void Start()
    {

    }

    public PlayerPos PositionOfPlayer;
    public enum PlayerPos
    {
        Pos1,
        Pos2,
        Pos3
    }

    public void MoveBlock()
    {
        StartCoroutine(MoveCube(target));



        IEnumerator MoveCube(Vector3 targetPosition)
        {
            Vector3 startPosition = transform.position;
            float timeElapsed = 0;
            CHKCounter++;
            while (timeElapsed < QTEMoveSpeed)
            {
                transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / QTEMoveSpeed);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            transform.position = targetPosition;
            PositionOfPlayer = PlayerPos.Pos2;
            
        }

    }

    public void PlayerCHKP()
    {
        if (PositionOfPlayer == PlayerPos.Pos1)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                PositionOfPlayer = PlayerPos.Pos2;
                CHKCounter++;
                objectPlayer.transform.position = Vector3.MoveTowards(transform.position, target, QTEMoveSpeed * Time.deltaTime);
                Debug.Log("Player pos -> 2");
            }
        }
    }

    public void QTEMoveToTarget()
    {
        //if (Pos1 != null)
        //{
        //    Vector3 targetPosition = Pos1.position;
        //    transform.position = Vector3.MoveTowards(transform.position, targetPosition, QTEMoveSpeed * Time.deltaTime);
        //}
        objectPlayer.transform.position = Vector3.MoveTowards(objectPlayer.transform.position, target, QTEMoveSpeed * Time.deltaTime);
        Debug.Log("QTEMOVE");
    }
}
