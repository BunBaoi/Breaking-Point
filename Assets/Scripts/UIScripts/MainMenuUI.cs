using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public static ScriptableObject playerSettings;

    public void Continue()
    {

    }

    public void NewGame()
    {

        SceneManager.LoadScene("GameScene");
        //SceneManager.LoadScene("Testing Level"); // Just for Testing

    }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Quit");
    }
}
