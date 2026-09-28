using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the first-gate riddle quiz.
/// Correct answer: Moon = index 1  (Sun=0, Moon=1, Mars=2, Jupiter=3)
/// </summary>
public class QuizManager : MonoBehaviour
{
    [Tooltip("0=Sun  1=Moon  2=Mars  3=Jupiter")]
    [SerializeField] private int correctAnswerIndex = 1;

    [Header("UI References")]
    [SerializeField] private GameObject questionCanvas;

    [Header("Answer Buttons (for colour feedback)")]
    [SerializeField] private Button[] answerButtons;

    [Header("First Gate")]
    [SerializeField] private Transform firstDoor;
    [SerializeField] private Vector3 firstDoorOpenOffset = new Vector3(0f, 3f, 0f);
    [SerializeField, Min(0.1f)] private float doorOpeningDuration = 2f;

    [Header("Question Voice (optional)")]
    [SerializeField] private AudioClip firstQuestionVoice;

    [Header("Sound Effects")]
    [SerializeField] private AudioClip correctSound;
    [SerializeField] private AudioClip wrongSound;
    [SerializeField] private AudioClip doorOpenSound;

    // colours
    private static readonly Color NormalColor  = new Color(0.15f, 0.35f, 0.65f, 1f);
    private static readonly Color WrongColor   = new Color(0.70f, 0.12f, 0.12f, 1f);
    private static readonly Color CorrectColor = new Color(0.12f, 0.60f, 0.20f, 1f);

    private AudioSource _audio;
    private bool _answerAccepted;

    [Header("Progression Portal")]
    [SerializeField] private GameObject firstPortal;

    // ────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
        {
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake  = false;
            _audio.spatialBlend = 0f;
        }
    }

    private void Start()
    {
        if (firstQuestionVoice != null && _audio != null)
            _audio.PlayOneShot(firstQuestionVoice);

        // Log wiring state so we can spot missing references instantly
        Debug.Log($"[QuizManager] Ready — canvas={(questionCanvas != null ? questionCanvas.name : "NULL ⚠")} | door={(firstDoor != null ? firstDoor.name : "NULL ⚠")} | correct={correctAnswerIndex}");
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Public API called by QuizButton
    // ────────────────────────────────────────────────────────────────────────

    // Zero-parameter wrappers (for Inspector OnClick() if needed)
    public void SelectAnswer0() => SelectAnswer(0);
    public void SelectAnswer1() => SelectAnswer(1);
    public void SelectAnswer2() => SelectAnswer(2);
    public void SelectAnswer3() => SelectAnswer(3);

    public void SelectAnswer(int index)
    {
        Debug.Log($"[QuizManager] SelectAnswer({index}) called — accepted={_answerAccepted} correct={correctAnswerIndex}");

        if (_answerAccepted) return;

        if (index != correctAnswerIndex)
        {
            Debug.Log("[QuizManager] WRONG answer.");
            if (wrongSound != null && _audio != null) _audio.PlayOneShot(wrongSound);
            StartCoroutine(FlashWrong(index));
            return;
        }

        // ── Correct! ──
        _answerAccepted = true;
        Debug.Log("[QuizManager] CORRECT answer!");
        if (correctSound != null && _audio != null) _audio.PlayOneShot(correctSound);
        StartCoroutine(CorrectSequence(index));
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Feedback coroutines
    // ────────────────────────────────────────────────────────────────────────

    private IEnumerator FlashWrong(int idx)
    {
        SetButtonColor(idx, WrongColor);
        yield return new WaitForSeconds(0.7f);
        SetButtonColor(idx, NormalColor);
    }

    private IEnumerator CorrectSequence(int idx)
    {
        SetButtonColor(idx, CorrectColor);
        yield return new WaitForSeconds(0.9f);

        if (questionCanvas != null)
            questionCanvas.SetActive(false);

        if (firstDoor == null)
        {
            Debug.LogError("[QuizManager] firstDoor is NULL — assign it in the Inspector! Canvas hidden but door won't move.");
            yield break;
        }

        yield return StartCoroutine(OpenDoor());
    }

    private void SetButtonColor(int idx, Color color)
    {
        if (answerButtons == null || idx >= answerButtons.Length || answerButtons[idx] == null) return;
        Image img = answerButtons[idx].GetComponent<Image>();
        if (img) img.color = color;
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Door animation
    // ────────────────────────────────────────────────────────────────────────

    private IEnumerator OpenDoor()
    {
        if (doorOpenSound != null && _audio != null) _audio.PlayOneShot(doorOpenSound);

        Vector3 start   = firstDoor.localPosition;
        Vector3 end     = start + firstDoorOpenOffset;
        float   elapsed = 0f;
        Debug.Log($"[QuizManager] Opening door from {start} to {end}");

        while (elapsed < doorOpeningDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / doorOpeningDuration);
            float s = t * t * (3f - 2f * t);
            firstDoor.localPosition = Vector3.LerpUnclamped(start, end, s);
            yield return null;
        }

        firstDoor.localPosition = end;
        Debug.Log("[QuizManager] Door opened.");
        if (firstPortal != null) firstPortal.SetActive(true);
    }

}
