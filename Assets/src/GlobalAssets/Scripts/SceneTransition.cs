using UnityEngine;

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

        SceneTransitionManager.Load(sceneName, false);
    }

    static public void LoadSceneByName(string sceneNamePassedIn)
    {
        SceneTransitionManager.Load(sceneNamePassedIn, false);
    }

    public void LoadSceneWithFade()
    {
        SceneTransitionManager.Load(sceneName, true);
    }

    static public void LoadSceneByNameWithFade(string sceneNamePassedIn)
    {
        SceneTransitionManager.Load(sceneNamePassedIn, true);
    }
}
