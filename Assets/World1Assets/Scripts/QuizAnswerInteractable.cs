using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(XRSimpleInteractable))]
public class QuizAnswerInteractable : MonoBehaviour
{
    [SerializeField] private QuizManager quizManager;
    [SerializeField] private int answerIndex;

    private XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();

        if (quizManager == null)
        {
            quizManager = FindFirstObjectByType<QuizManager>();
        }
    }

    private void OnEnable()
    {
        if (interactable == null)
        {
            interactable = GetComponent<XRSimpleInteractable>();
        }

        interactable.activated.AddListener(OnActivated);
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.activated.RemoveListener(OnActivated);
        }
    }

    public void Configure(QuizManager manager, int index)
    {
        quizManager = manager;
        answerIndex = index;
    }

    private void OnActivated(ActivateEventArgs args)
    {
        quizManager?.SelectAnswer(answerIndex);
    }
}
