using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        //SceneManager.LoadSceneAsync(1);
        LoadingScreenManager.loadInstance.SwitchToScene(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
