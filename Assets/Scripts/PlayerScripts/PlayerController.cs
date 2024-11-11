using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static QTEMechanic;

public class PlayerController : MonoBehaviour
{
    public QTEventUI qTEvent;
    public QTEMechanic qTEMechanic;
    public CharacterController controller;
    public PlayerStats playerStats;

    public float objectSpeed = 3;
    private float moveDuration = 5f;
    public Vector3 target = new Vector3(2, 3, 4);

    public float speed = 12f;
    public float gravity = -9.81f;
    public bool IsSprint = false;

    public float playerHeight;

    Vector3 velocity;
    Rigidbody rb;

    //public float maxSlopeAngle;
    //private RaycastHit slopehit;

    void Start()
    {

    }

    void Update()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        controller.Move(move * speed * Time.deltaTime);
        velocity.y += gravity * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime);

        sprint();
        OxyOuputRate();
        QTEControl();
    }

    private void sprint()
    {
        if(Input.GetKey(KeyCode.LeftShift))
        {
            speed = 20f;
            IsSprint = true;
        }
        else
        {
            speed = 12f;
            IsSprint = false;
        }
        
    }

    public void QTEControl()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("Z Key");
            qTEvent.QTEActive(); // Delete QTE UI
            //MoveToPosition(target);
            //qTEMechanic.MoveBlock();
            //StartCoroutine(MoveCube(target));

            qTEMechanic.PositionOfPlayer = PlayerPos.Pos2;

            //transform.position = Vector3.Lerp(transform.position, target, 10);
        }
    }


    public void MoveToPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
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

    public void OxyOuputRate()
    {
        if(Input.GetKeyDown(KeyCode.E))
        {
            playerStats.OxygenTankRefillRate++;
            //Debug.log("Rate Up");
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            playerStats.OxygenTankRefillRate--;
            //Debug.log("Rate Down");
        }
    }

}
