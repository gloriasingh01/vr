using System.Collections;
using UnityEngine;
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

    private bool answerAccepted;
    private static readonly Color Normal = new Color(0.15f, 0.35f, 0.65f, 1f);
    private static readonly Color Wrong = new Color(0.70f, 0.12f, 0.12f, 1f);
    private static readonly Color Correct = new Color(0.12f, 0.60f, 0.20f, 1f);

    public void SelectAnswer(int index)
    {
        if (answerAccepted) return;
        if (index != correctAnswerIndex) { StartCoroutine(Flash(index)); return; }
        answerAccepted = true;
        StartCoroutine(Complete(index));
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
        if (questionCanvas != null) questionCanvas.SetActive(false);
        if (doorToOpen == null) yield break;
        Vector3 start = doorToOpen.localPosition;
        Vector3 end = start + doorOpenOffset;
        for (float elapsed = 0f; elapsed < doorOpeningDuration; elapsed += Time.deltaTime)
        {
            float t = Mathf.Clamp01(elapsed / doorOpeningDuration);
            t = t * t * (3f - 2f * t);
            doorToOpen.localPosition = Vector3.LerpUnclamped(start, end, t);
            yield return null;
        }
        doorToOpen.localPosition = end;
        if (portalToActivate != null) portalToActivate.SetActive(true);
    }

    private void SetColor(int index, Color color)
    {
        if (answerButtons == null || index < 0 || index >= answerButtons.Length || answerButtons[index] == null) return;
        Image image = answerButtons[index].GetComponent<Image>();
        if (image != null) image.color = color;
    }
}
