using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForTipManager : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CallShowTip(int index)
    {
        TipManager.Instance.ShowTip(index);
    }
}
