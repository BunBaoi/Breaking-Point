using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private CinematicSequence cinematicSequence;

    void Start()
    {
        cinematicSequence.StartCinematic();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
