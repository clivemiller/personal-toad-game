using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string sceneName = "";
    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneTransition: sceneName is empty.", this);
            return;
        }

        SceneManager.LoadSceneAsync(sceneName);
    }

    static public void LoadSceneByName(string sceneNamePassedIn)
    {
        SceneManager.LoadSceneAsync(sceneNamePassedIn);
    }

    public void LoadSceneWithFade()
    {
        SceneTransitionManager.LoadScene(sceneName);
    }

    static public void LoadSceneByNameWithFade(string sceneNamePassedIn)
    {
        SceneTransitionManager.LoadScene(sceneNamePassedIn);
    }
}
