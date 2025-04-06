using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemCheckerForTips : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private Item[] items;
    [SerializeField] private float[] tipIndexes;
    [SerializeField] private TipManager tipManager;
    [SerializeField] private string[] boolNames;
    [SerializeField] private ForBoolManager forBoolManager;

    private bool[] itemTipsShown;

    void Start()
    {
        itemTipsShown = new bool[items.Length];

        // LoadTipStatus();
    }

    void Update()
    {
        // Loop through each item and check if the player has it
        for (int i = 0; i < items.Length; i++)
        {
            if (inventoryManager.HasItem(items[i]) && !itemTipsShown[i])
            {
                // Show the tip related to the item
                int tipNumber = Mathf.FloorToInt(tipIndexes[i]);
                tipManager.ShowTip(tipNumber);

                if (forBoolManager != null)
                {
                    forBoolManager.SetBoolVariable(boolNames[i]);
                }

                itemTipsShown[i] = true;
                // SaveTipStatus();
            }
        }
    }

    private void SaveTipStatus()
    {
        for (int i = 0; i < itemTipsShown.Length; i++)
        {
            PlayerPrefs.SetInt($"TipShown_{i}", itemTipsShown[i] ? 1 : 0);
        }
        PlayerPrefs.Save();
    }

    private void LoadTipStatus()
    {
        for (int i = 0; i < itemTipsShown.Length; i++)
        {
            itemTipsShown[i] = PlayerPrefs.GetInt($"TipShown_{i}", 0) == 1;
        }
    }
}