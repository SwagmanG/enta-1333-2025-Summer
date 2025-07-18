using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingScreenManager : MonoBehaviour
{
    public static LoadingScreenManager loadInstance;

    public GameObject loadingScreenObject;

    public Slider progressBar;

    private void Awake()
    {
        if (loadInstance != null && loadInstance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            loadInstance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    public void SwitchToScene(int SceneID)
    {
        loadingScreenObject.SetActive(true);
        progressBar.value = 0;
        StartCoroutine(SwitchSceneAsync(SceneID));
    }

    IEnumerator SwitchSceneAsync(int SceneID)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(SceneID);
        while (!asyncLoad.isDone)
        {
            progressBar.value = asyncLoad.progress;
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);

        loadingScreenObject.SetActive(false);
    }
}

