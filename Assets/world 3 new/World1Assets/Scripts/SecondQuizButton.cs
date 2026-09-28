using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Routes hand-pinch / XR-ray UI clicks to the second quiz only.</summary>
[RequireComponent(typeof(Button))]
public class SecondQuizButton : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private int answerIndex;
    [SerializeField] private SecondQuizManager quizManager;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    private void Start()
    {
        button.onClick.RemoveListener(Click);
        button.onClick.AddListener(Click);
    }

    private void Click() => quizManager?.SelectAnswer(answerIndex);
    public void OnPointerClick(PointerEventData eventData) => quizManager?.SelectAnswer(answerIndex);
}
