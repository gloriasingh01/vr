using UnityEngine;
using UnityEngine.UI;

/// <summary>Tracks the World 3 mech kills and shows the final score after the last mech.</summary>
public class MechScoreManager : MonoBehaviour
{
    [Min(1)] public int pointsPerKill = 100;
    [Min(1)] public int totalMechs = 7;
    public Text scoreText;

    public int TotalPoints { get; private set; }
    public int MechsDefeated { get; private set; }

    private bool finalScoreShown;

    private void Start()
    {
        if (scoreText == null)
            CreateScoreDisplay();

        RefreshScoreDisplay();
    }

    public void RegisterMechKill()
    {
        if (finalScoreShown)
            return;

        MechsDefeated++;
        TotalPoints += pointsPerKill;
        Debug.Log("MECH KILLED | SCORE = " + TotalPoints);

        if (MechsDefeated >= totalMechs)
        {
            finalScoreShown = true;
            Debug.Log("WORLD 3 FINAL SCORE = " + TotalPoints);
        }

        RefreshScoreDisplay();
    }

    private void RefreshScoreDisplay()
    {
        if (scoreText == null)
            return;

        scoreText.text = finalScoreShown
            ? "FINAL SCORE: " + TotalPoints
            : "MECH SCORE: " + TotalPoints;
    }

    private void CreateScoreDisplay()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("MECH SCORE: No Canvas found; the score will still be logged.");
            return;
        }

        GameObject labelObject = new GameObject("Mech Score", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        labelObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = (RectTransform)labelObject.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(480f, 72f);

        scoreText = labelObject.GetComponent<Text>();
        scoreText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        scoreText.fontSize = 42;
        scoreText.fontStyle = FontStyle.Bold;
        scoreText.color = Color.white;
        scoreText.alignment = TextAnchor.UpperLeft;
        scoreText.raycastTarget = false;
    }
}
