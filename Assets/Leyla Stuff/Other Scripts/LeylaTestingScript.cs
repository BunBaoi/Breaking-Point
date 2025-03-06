using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeylaTestingScript : MonoBehaviour
{
    [SerializeField] private KeyCode key = KeyCode.P;
    [SerializeField] private string boolName = "";

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(key))
        {
            if (BoolManager.Instance != null)
            {
                BoolManager.Instance.SetBool(boolName, true);
            }
            else
            {
                Debug.LogError("BoolManager.Instance is null.");
            }
        }
    }
}
