using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Controls the second, placeable sequence quiz and its assigned door.</summary>
public class SecondQuizManager : MonoBehaviour
{
    [SerializeField] private int correctAnswerIndex = 0;
    [SerializeField] private GameObject questionCanvas;
    [SerializeField] private Button[] answerButtons;
    [SerializeField] private Transform doorToOpen;
    [SerializeField] private Vector3 doorOpenOffset = new Vector3(0f, 3f, 0f);
    [SerializeField, Min(0.1f)] private float doorOpeningDuration = 2f;
    [SerializeField] private GameObject portalToActivate;
    [Tooltip("Optional scene to load after the correct answer. Leave blank for a normal quiz.")]
    [SerializeField] private string sceneToLoadOnCorrect;

    public string SceneToLoadOnCorrect => sceneToLoadOnCorrect;

    private bool answerAccepted;

    private static readonly Color Normal = new Color(0.15f, 0.35f, 0.65f, 1f);
    private static readonly Color Wrong = new Color(0.70f, 0.12f, 0.12f, 1f);
    private static readonly Color Correct = new Color(0.12f, 0.60f, 0.20f, 1f);

    private void Start()
    {
        answerAccepted = false; // reset every Play so re-entering Play Mode works cleanly
        Debug.Log($"[SecondQuizManager] Ready — door={(doorToOpen != null ? doorToOpen.name : "NULL ⚠")} | " +
                  $"canvas={(questionCanvas != null ? questionCanvas.name : "NULL")} | " +
                  $"buttons={answerButtons?.Length ?? 0} | correctIndex={correctAnswerIndex}");
    }

    /// <summary>Called by SecondQuizButton when the player selects an answer.</summary>
    public void SelectAnswer(int index)
    {
        Debug.Log($"[SecondQuizManager] SelectAnswer({index}) — accepted={answerAccepted} correct={correctAnswerIndex}");
        if (answerAccepted) return;

        if (index != correctAnswerIndex)
        {
            Debug.Log("[SecondQuizManager] WRONG answer.");
            StartCoroutine(Flash(index));
            return;
        }

        Debug.Log("[SecondQuizManager] CORRECT answer! Starting door sequence.");
        answerAccepted = true;
        StartCoroutine(Complete(index));
    }

    /// <summary>Resets the quiz so it can be answered again (useful during testing).</summary>
    public void ResetQuiz()
    {
        answerAccepted = false;
        Debug.Log("[SecondQuizManager] Quiz reset.");
    }

    private IEnumerator Flash(int index)
    {
        SetColor(index, Wrong);
        yield return new WaitForSeconds(0.7f);
        SetColor(index, Normal);
    }

    private IEnumerator Complete(int index)
    {
        SetColor(index, Correct);
        yield return new WaitForSeconds(0.9f);

        // ── Load scene if configured ──────────────────────────────────────────
        if (!string.IsNullOrWhiteSpace(sceneToLoadOnCorrect))
        {
            if (questionCanvas != null) questionCanvas.SetActive(false);
            SceneManager.LoadScene(sceneToLoadOnCorrect, LoadSceneMode.Single);
            yield break;
        }

        // ── Open door FIRST (before hiding canvas) ───────────────────────────
        // IMPORTANT: questionCanvas.SetActive(false) must come AFTER the door
        // animation finishes. If the canvas GameObject is also the parent of
        // this MonoBehaviour, SetActive(false) would kill the coroutine early.
        if (doorToOpen == null)
        {
            Debug.LogError("[SecondQuizManager] doorToOpen is NULL — assign it in the Inspector! Door won't open.");
            if (questionCanvas != null) questionCanvas.SetActive(false);
            yield break;
        }

        Vector3 start = doorToOpen.localPosition;
        Vector3 end   = start + doorOpenOffset;
        Debug.Log($"[SecondQuizManager] Opening door '{doorToOpen.name}' from {start} to {end}");

        for (float elapsed = 0f; elapsed < doorOpeningDuration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / doorOpeningDuration);
            t = t * t * (3f - 2f * t);
            doorToOpen.localPosition = Vector3.LerpUnclamped(start, end, t);
            yield return null;
        }

        doorToOpen.localPosition = end;
        Debug.Log("[SecondQuizManager] Door fully opened.");

        // ── Hide canvas AFTER door is open ────────────────────────────────────
        if (questionCanvas != null) questionCanvas.SetActive(false);

        if (portalToActivate != null) portalToActivate.SetActive(true);
    }

    private void SetColor(int index, Color color)
    {
        if (answerButtons == null || index < 0 || index >= answerButtons.Length || answerButtons[index] == null) return;
        Image image = answerButtons[index].GetComponent<Image>();
        if (image != null) image.color = color;
    }
}
