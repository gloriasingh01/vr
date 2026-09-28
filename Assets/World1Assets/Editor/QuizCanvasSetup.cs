#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// GameObject ▶ Quiz ▶ Setup Quiz Canvas
/// Deletes any old QuestionCanvas and builds a fresh one.
/// </summary>
public static class QuizCanvasSetup
{
    [MenuItem("GameObject/Quiz/Setup Quiz Canvas", false, 10)]
    static void CreateQuizCanvas(MenuCommand cmd)
    {
        // ── 0. Remove old canvas ─────────────────────────────────────────────
        GameObject old = GameObject.Find("QuestionCanvas");
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old);
            Debug.Log("[QuizSetup] Old QuestionCanvas removed.");
        }

        // ── 1. EventSystem ───────────────────────────────────────────────────
        EnsureXREventSystem();

        // ── 2. Find camera ───────────────────────────────────────────────────
        Camera mainCam = Camera.main ?? Object.FindFirstObjectByType<Camera>();

        // ── 3. Canvas ────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("QuestionCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create QuizCanvas");

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        if (mainCam != null) canvas.worldCamera = mainCam;

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta  = new Vector2(200f, 140f);   // 2 m × 1.4 m at scale 0.01
        canvasRT.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        canvasGO.transform.position = new Vector3(0f, 1.6f, 3f);  // move in scene

        canvasGO.AddComponent<TrackedDeviceGraphicRaycaster>();
        canvasGO.AddComponent<CanvasScaler>();

        // ── 4. Background ────────────────────────────────────────────────────
        GameObject bg = MakePanel(canvasGO, "Background",
            Vector2.zero, new Vector2(200f, 140f),
            new Color(0.05f, 0.05f, 0.15f, 0.93f));

        // ── 5. Question text ─────────────────────────────────────────────────
        MakeTMP(bg, "QuestionText",
            new Vector2(0f, 38f), new Vector2(182f, 72f),
            "An inscription appears:\n" +
            "\"I touch no ocean, yet command its rise.\n" +
            "I shine with borrowed fire across your skies.\n" +
            "I change my face, though I never turn away.\n" +
            "What am I?\"",
            9.5f);

        // ── 6. Answer buttons ────────────────────────────────────────────────
        string[]  labels = { "Sun", "Moon", "Mars", "Jupiter" };
        Vector2[] pos    = {
            new Vector2(-52f, -38f),
            new Vector2( 52f, -38f),
            new Vector2(-52f, -62f),
            new Vector2( 52f, -62f),
        };

        QuizManager qm      = Object.FindFirstObjectByType<QuizManager>();
        Button[]    buttons = new Button[labels.Length];

        for (int i = 0; i < labels.Length; i++)
            buttons[i] = MakeAnswerButton(bg, labels[i], i, pos[i], qm);

        // ── 7. Link references to QuizManager ───────────────────────────────
        if (qm != null)
        {
            SerializedObject so = new SerializedObject(qm);
            so.FindProperty("questionCanvas").objectReferenceValue = canvasGO;

            SerializedProperty arr = so.FindProperty("answerButtons");
            arr.arraySize = buttons.Length;
            for (int i = 0; i < buttons.Length; i++)
                arr.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(qm);
            Debug.Log("[QuizSetup] QuizManager wired up.");
        }
        else
        {
            Debug.LogWarning("[QuizSetup] QuizManager not found in scene. Wire it manually.");
        }

        Selection.activeGameObject = canvasGO;
        Debug.Log("[QuizSetup] Done! Move QuestionCanvas in front of the door, then assign First Door in QuizManager.");
    }

    // ────────────────────────────────────────────────────────────────────────

    static void EnsureXREventSystem()
    {
        EventSystem es = Object.FindFirstObjectByType<EventSystem>();
        if (es != null)
        {
            if (es.GetComponent<StandaloneInputModule>() != null &&
                es.GetComponent<XRUIInputModule>() == null)
            {
                Undo.DestroyObjectImmediate(es.GetComponent<StandaloneInputModule>());
                Undo.AddComponent<XRUIInputModule>(es.gameObject);
                Debug.Log("[QuizSetup] EventSystem upgraded to XRUIInputModule.");
            }
            return;
        }
        GameObject esGO = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<XRUIInputModule>();
        Debug.Log("[QuizSetup] EventSystem created with XRUIInputModule.");
    }

    static GameObject MakePanel(GameObject parent, string name,
                                 Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        return go;
    }

    static void MakeTMP(GameObject parent, string name,
                         Vector2 anchoredPos, Vector2 size,
                         string text, float fontSize)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text            = text;
        tmp.fontSize        = fontSize;
        tmp.alignment       = TextAlignmentOptions.Center;
        tmp.color           = Color.white;
        tmp.textWrappingMode = TextWrappingModes.Normal;
    }

    static Button MakeAnswerButton(GameObject parent, string label, int index,
                                    Vector2 anchoredPos, QuizManager qm)
    {
        // Root
        var go = new GameObject("Answer_" + label, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta        = new Vector2(88f, 22f);
        rt.anchoredPosition = anchoredPos;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.15f, 0.35f, 0.65f, 1f);

        Button btn = go.AddComponent<Button>();
        ColorBlock cb       = btn.colors;
        cb.normalColor      = new Color(0.15f, 0.35f, 0.65f, 1f);
        cb.highlightedColor = new Color(0.30f, 0.58f, 0.95f, 1f);
        cb.pressedColor     = new Color(0.05f, 0.18f, 0.42f, 1f);
        cb.selectedColor    = cb.highlightedColor;
        btn.colors          = cb;
        btn.targetGraphic   = img;

        // Label
        var textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(go.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 8f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        // ── QuizButton component – serializable, survives scene save ─────────
        QuizButton qb = go.AddComponent<QuizButton>();
        SerializedObject soQB = new SerializedObject(qb);
        soQB.FindProperty("answerIndex").intValue = index;
        if (qm != null)
            soQB.FindProperty("quizManager").objectReferenceValue = qm;
        soQB.ApplyModifiedProperties();
        EditorUtility.SetDirty(qb);

        return btn;
    }
}
#endif
