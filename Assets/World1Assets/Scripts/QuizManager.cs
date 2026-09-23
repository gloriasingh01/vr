using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class QuizManager : MonoBehaviour
{
    // The current riddle's correct answer is Moon (Answer2, index 1).
    [SerializeField] private int correctAnswerIndex = 1;

    [Header("First gate")]
    [SerializeField] private GameObject questionCanvas;
    [SerializeField] private Transform firstDoor;
    [SerializeField] private Vector3 firstDoorOpenOffset = new Vector3(0f, 3f, 0f);
    [SerializeField, Min(0.1f)] private float doorOpeningDuration = 2f;

    [Header("Question voice")]
    [SerializeField] private AudioClip firstQuestionVoice;

    private AudioSource voiceSource;
    private bool answerAccepted;
    private XRUIInputModule xrUiInputModule;
    private XRInteractionManager xrInteractionManager;
    private float nextXrRegistrationTime;

    private void Awake()
    {
        voiceSource = GetComponent<AudioSource>();

        if (firstQuestionVoice != null && voiceSource == null)
        {
            voiceSource = gameObject.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        xrUiInputModule = FindAnyObjectByType<XRUIInputModule>();
        xrInteractionManager = FindAnyObjectByType<XRInteractionManager>();

        if (firstQuestionVoice != null && voiceSource != null)
        {
            voiceSource.PlayOneShot(firstQuestionVoice);
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextXrRegistrationTime)
        {
            return;
        }

        nextXrRegistrationTime = Time.unscaledTime + 0.5f;
        RegisterActiveXrInteractors();
    }

    private void RegisterActiveXrInteractors()
    {
        if (xrInteractionManager == null)
        {
            xrInteractionManager = FindAnyObjectByType<XRInteractionManager>();
        }

        if (xrUiInputModule == null)
        {
            xrUiInputModule = FindAnyObjectByType<XRUIInputModule>();
        }

        if (xrInteractionManager == null)
        {
            return;
        }

        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IXRInteractor xrInteractor)
            {
                xrInteractionManager.RegisterInteractor(xrInteractor);
            }

            if (xrUiInputModule != null && xrUiInputModule.isActiveAndEnabled && behaviour is IUIInteractor uiInteractor)
            {
                xrUiInputModule.RegisterInteractor(uiInteractor);
            }
        }
    }

    public void SelectAnswer(int answerIndex)
    {
        if (answerAccepted)
        {
            return;
        }

        if (answerIndex != correctAnswerIndex)
        {
            Debug.Log("WRONG ANSWER! Try again.");
            return;
        }

        if (firstDoor == null)
        {
            Debug.LogError("QuizManager needs the First Door reference before it can open the gate.");
            return;
        }

        answerAccepted = true;

        if (questionCanvas != null)
        {
            questionCanvas.SetActive(false);
        }

        StartCoroutine(OpenFirstDoor());
    }

    private IEnumerator OpenFirstDoor()
    {
        Vector3 closedPosition = firstDoor.localPosition;
        Vector3 openPosition = closedPosition + firstDoorOpenOffset;
        float elapsed = 0f;

        while (elapsed < doorOpeningDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / doorOpeningDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            firstDoor.localPosition = Vector3.LerpUnclamped(closedPosition, openPosition, easedProgress);
            yield return null;
        }

        firstDoor.localPosition = openPosition;
        Debug.Log("First temple gate opened.");
    }
}
