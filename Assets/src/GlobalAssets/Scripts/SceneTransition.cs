using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public string sceneName;
    public void LoadScene()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("SceneTransition: sceneName is empty.", this);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    public void LoadSceneByName(string sceneNamePassedIn)
    {
        if (string.IsNullOrWhiteSpace(sceneNamePassedIn))
        {
            Debug.LogWarning("SceneTransition: sceneName is empty.", this);
            return;
        }

        SceneManager.LoadScene(sceneNamePassedIn);
    }
}
