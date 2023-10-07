using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public enum NextScene
    {
        Level1Scene,
        Level2Scene,
        Level3Scene
    }

    public NextScene nextScene;

    private void OnTriggerEnter(Collider transit)
    {
        if (transit.CompareTag("Player"))
        {
            LoadNextScene();
        }
    }

    private void LoadNextScene()
    {
        string sceneToLoad = "";

        // Determine the scene to load based on the enum selection
        switch (nextScene)
        {
            case NextScene.Level1Scene:
                sceneToLoad = "Level1Scene";
                break;
            case NextScene.Level2Scene:
                sceneToLoad = "Level2Scene";
                break;
            case NextScene.Level3Scene:
                sceneToLoad = "Level3Scene";
                break;
        }

        SceneManager.LoadScene(sceneToLoad);
    }
}