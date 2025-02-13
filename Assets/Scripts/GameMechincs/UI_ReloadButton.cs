using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ReloadButton : MonoBehaviour
{

    public KeyCode _Key;
    public QTEvent _QTEvent;
    public GameObject qTEventUI;

    void Start()
    {
        _QTEvent = qTEventUI.GetComponent<QTEvent>();

        gameObject.SetActive(!gameObject.activeSelf);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(_Key))
        {
            TestBench();
            _QTEvent.CorrectKeyCounter++;
        }
    }

    public void TestBench()
    {
        //Debug.Log("Button Press");
        gameObject.SetActive(false);
    }
}
