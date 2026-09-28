using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Attach this to each answer button.
/// Works with XR Ray Interactor (via TrackedDeviceGraphicRaycaster)
/// AND with mouse clicks in the Unity editor.
/// </summary>
[RequireComponent(typeof(Button))]
public class QuizButton : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("0=Sun  1=Moon  2=Mars  3=Jupiter")]
    [SerializeField] public int answerIndex;

    [SerializeField] private QuizManager quizManager;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();

        if (quizManager == null)
            quizManager = FindFirstObjectByType<QuizManager>();
    }

    private void Start()
    {
        // Wire the Unity Button.onClick as well (works even without EventSystem tricks)
        _button.onClick.RemoveListener(OnButtonClicked);
        _button.onClick.AddListener(OnButtonClicked);
    }

    // Called by Unity UI Button.onClick
    private void OnButtonClicked()
    {
        Debug.Log($"[QuizButton] '{gameObject.name}' clicked → answerIndex={answerIndex}");
        quizManager?.SelectAnswer(answerIndex);
    }

    // Called by IPointerClickHandler → works with mouse AND XR ray pointer
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[QuizButton] OnPointerClick '{gameObject.name}' → answerIndex={answerIndex}");
        quizManager?.SelectAnswer(answerIndex);
    }
}
