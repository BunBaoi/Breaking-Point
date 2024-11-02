using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QTEMechanic : MonoBehaviour
{

    //public GameObject Pos1;
    //public GameObject Pos2;
    //public GameObject Pos3;

    private float moveDuration = 5f;
    public Vector3 target = new Vector3(0, 0, 2);

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(MoveCube(target));
    }

    IEnumerator MoveCube(Vector3 targetPosition)
    {
        Vector3 startPosition = transform.position;
        float timeElapsed = 0;
        while (timeElapsed < moveDuration)
        {
            transform.position = Vector3.Lerp(startPosition, targetPosition, timeElapsed / moveDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = targetPosition;

    }

    public void QTEMove()
    {

    }

}
