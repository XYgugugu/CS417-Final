using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SceneRoutePortal : MonoBehaviour
{
    [Header("Route")]
    [SerializeField] public string targetSceneName = "Level2";

    [Header("Transition (Optional)")]
    [SerializeField] private LevelTransition levelTransition;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnSelected);
        }
        else
        {
            Debug.LogWarning("SceneRoutePortal needs an XR Simple Interactable on " + gameObject.name);
        }

        if (levelTransition == null)
        {
            levelTransition = FindObjectOfType<LevelTransition>();
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnSelected);
        }
    }

    private void OnSelected(SelectEnterEventArgs args)
    {
        LoadScene();
    }

    public void LoadScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("SceneRoutePortal: targetSceneName is not set.");
            return;
        }

        if (levelTransition != null)
        {
            levelTransition.FadeAndLoadScene(targetSceneName);
        }
        else
        {
            SceneManager.LoadScene(targetSceneName);
        }

        Debug.Log($"SceneRoutePortal: Loading scene {targetSceneName}.");
    }
}
