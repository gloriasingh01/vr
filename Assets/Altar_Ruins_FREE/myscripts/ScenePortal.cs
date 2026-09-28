using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("Portal Configuration")]
    [Tooltip("The exact name of the destination scene.")]
    [SerializeField] private string targetSceneName;

    private bool isTransitioning = false;

    public void SetTargetScene(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(sceneName))
            targetSceneName = sceneName;
    }

    // Called automatically by OnTriggerEnter OR by Teleport Anchor Events
    public void TriggerSceneLoad()
    {
        if (!isTransitioning)
        {
            StartCoroutine(LoadSceneAsync());
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Triggers when the player physically walks into the collider
        if (other.GetComponentInParent<Unity.XR.CoreUtils.XROrigin>() != null || other.CompareTag("Player"))
        {
            TriggerSceneLoad();
        }
    }

    private IEnumerator LoadSceneAsync()
    {
        isTransitioning = true;
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
