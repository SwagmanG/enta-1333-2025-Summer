using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        //Calls the loading for the main game scene.
        LoadingScreenManager.LoadInstance.SwitchToScene(1);
    }

    public void QuitGame()
    {
        //Quits the game, only works in builds.
        Application.Quit();
    }
}
