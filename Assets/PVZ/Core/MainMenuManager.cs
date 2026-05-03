using UnityEngine;
using UnityEngine.SceneManagement;

namespace PVZ3D.Core
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] public string firstLevelSceneName = "Farm";

        public void StartGame()
        {
            Debug.Log("Trying to load scene: " + firstLevelSceneName);
            SceneManager.LoadScene(firstLevelSceneName);
        }
        
        //override
        public void StartGame(string sceneName)
        {
            Debug.Log("Trying to load scene: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }

        public void ExitGame()
        {
            #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
            #else
                    Application.Quit();
            #endif
        }
    }
}