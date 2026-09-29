using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Routes hand-pinch / XR-ray UI clicks to the second quiz managers.</summary>
[RequireComponent(typeof(Button))]
public class SecondQuizButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int answerIndex;
    [SerializeField] private SecondQuizManager quizManager;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (quizManager == null)
        {
            quizManager = GetComponentInParent<SecondQuizManager>() ?? FindFirstObjectByType<SecondQuizManager>();
        }
    }

    private void Start()
    {
        button.onClick.RemoveListener(Click);
        button.onClick.AddListener(Click);

        Debug.Log($"[SecondQuizButton] '{name}' ready — answerIndex={answerIndex}, manager='{(quizManager != null ? quizManager.name : "Auto-broadcast")}'");
    }

    private void Click()
    {
        Debug.Log($"[SecondQuizButton] Click() on '{name}' (answerIndex={answerIndex})");
        BroadcastAnswer(answerIndex);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[SecondQuizButton] OnPointerClick() on '{name}' (answerIndex={answerIndex})");
        BroadcastAnswer(answerIndex);
    }

    private void BroadcastAnswer(int index)
    {
        // 1. Direct call to specifically assigned quiz manager
        if (quizManager != null)
        {
            quizManager.SelectAnswer(index);
        }

        // 2. Also broadcast to any active SecondQuizManager in the scene (e.g. standalone manager with assigned door)
        var allManagers = FindObjectsByType<SecondQuizManager>(FindObjectsSortMode.None);
        foreach (var mgr in allManagers)
        {
            if (mgr != null && mgr != quizManager && string.IsNullOrWhiteSpace(mgr.SceneToLoadOnCorrect))
            {
                mgr.SelectAnswer(index);
            }
        }
    }
}


