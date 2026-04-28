using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public GameObject level1Prefab;
    public GameObject level2APrefab;
    public GameObject level2BPrefab;

    public Transform levelRoot;
    public Transform playerRig;
    public Transform playerStart;

    public GameObject currentLevel;

    private bool isSwitchingLevel = false;

    void Start()
    {
        LoadLevelInstant(level1Prefab);
    }

    public void LoadLevelInstant(GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogWarning("No level prefab assigned.");
            return;
        }

        if (currentLevel != null)
        {
            Destroy(currentLevel);
        }

        currentLevel = Instantiate(prefab, levelRoot);
        ResetPlayerPosition();
    }

    public void ChooseLevel2A()
    {
        if (isSwitchingLevel) return;
        StartCoroutine(SwitchLevel(level2APrefab));
    }

    public void ChooseLevel2B()
    {
        if (isSwitchingLevel) return;
        StartCoroutine(SwitchLevel(level2BPrefab));
    }

    private IEnumerator SwitchLevel(GameObject nextLevel)
    {
        isSwitchingLevel = true;

        Debug.Log("Transitioning to next level...");

        // Simple transition delay for now.
        yield return new WaitForSeconds(0.5f);

        LoadLevelInstant(nextLevel);

        isSwitchingLevel = false;
    }

    private void ResetPlayerPosition()
    {
        if (playerRig == null || playerStart == null) return;

        playerRig.position = playerStart.position;
        playerRig.rotation = playerStart.rotation;
    }

    public void CompleteLevel()
    {
        Debug.Log("Level completed. Choose a route.");
    }

    public void FailLevel()
    {
        Debug.Log("Level failed. Enemy reached the base.");
    }
}