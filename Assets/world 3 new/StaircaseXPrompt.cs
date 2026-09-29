using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public class StaircaseXPrompt : MonoBehaviour
{
    [Header("Route")]
    public Transform teleportPointDown;
    public Transform teleportPointUp;
    public XROrigin xrOrigin;
    public Camera playerCamera;

    [Header("8BitDo X button (reference only; input is Gamepad/buttonWest)")]
    [Tooltip("Keep the existing XRI Left Interaction / Select reference. Teleport activation is read only from Gamepad buttonWest.")]
    public InputActionReference xSelectActionReference;

    [Header("Down trigger")]
    public Vector3 triggerSize = new Vector3(32f, 24f, 32f);
    public Vector3 triggerCenter = new Vector3(0f, 12f, 0f);
    public bool bidirectionalTeleport;

    [Header("Prompt and transition")]
    public Canvas promptCanvas;
    public CanvasGroup promptCanvasGroup;
    public RectTransform xButtonVisual;
    public Graphic xButtonGraphic;
    public CanvasGroup fadeCanvasGroup;
    public Font hudFont;
    [Min(0.05f)] public float fadeDuration = 0.35f;
    [Min(0.1f)] public float promptDistance = 1.45f; // Retained for existing scene serialization.
    [Min(0.1f)] public float worldPromptHeight = 1.45f;

    private readonly HashSet<Collider> playerColliders = new HashSet<Collider>();
    private BoxCollider trigger;
    private BoxCollider upTrigger;
    private bool promptWanted;
    private bool promptAtDown;
    private bool transitionRunning;
    private bool mustExitTeleportAreas;
    private Text promptText;
    private Vector3 buttonBaseScale = Vector3.one;
    private int teleportStartedFrame = -1;

    private static readonly Color Cyan = new Color32(82, 232, 255, 255);

    private void Awake()
    {
        ResolveReferences();
        ConfigureTrigger();
        CreatePromptUi();
        CreateFadeUi();
    }

    private void Start()
    {
        // Keep the serialized action reference for scene compatibility, but do not
        // subscribe to it. Teleport input is read only from Gamepad.buttonWest below.
        InputAction referencedAction = xSelectActionReference != null ? xSelectActionReference.action : null;
        if (referencedAction == null || referencedAction.name != "Select" ||
            referencedAction.actionMap == null || referencedAction.actionMap.name != "XRI Left Interaction")
        {
            Debug.LogError("STAIRCASE: X Select Action Reference must be XRI Left Interaction/Select.");
        }
        else
        {
            Debug.Log("STAIRCASE: XRI Left Interaction/Select reference verified; teleport uses Gamepad/buttonWest only.");
        }
    }

    private void OnDisable()
    {
        promptWanted = false;
        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = 0f;
    }

    private void OnDestroy()
    {
        if (promptCanvas != null)
            Destroy(promptCanvas.gameObject);
    }

    private void Update()
    {
        if (transitionRunning || xrOrigin == null)
            return;

        Vector3 playerPosition = playerCamera != null ? playerCamera.transform.position : xrOrigin.transform.position;
        bool insideDown = trigger != null && trigger.enabled && IsPlayerInsideTrigger(trigger, playerPosition);
        bool insideUp = bidirectionalTeleport && upTrigger != null && upTrigger.enabled &&
            IsPlayerInsideTrigger(upTrigger, playerPosition);

        if (mustExitTeleportAreas && !insideDown && !insideUp)
            mustExitTeleportAreas = false;

        bool wasPromptWanted = promptWanted;
        bool newPromptWanted = !mustExitTeleportAreas && (insideDown || insideUp);
        bool newPromptAtDown = insideDown || !insideUp;
        if (newPromptWanted != promptWanted || (newPromptWanted && newPromptAtDown != promptAtDown))
        {
            promptWanted = newPromptWanted;
            promptAtDown = newPromptAtDown;
            if (promptWanted)
            {
                PlacePromptAtPoint();
                Debug.Log(promptAtDown
                    ? "STAIRCASE: Player entered the DOWN trigger; PRESS X TO GO UP prompt shown."
                    : "STAIRCASE: Player reached the UP point; PRESS X TO GO DOWN prompt shown.");
            }
            else if (wasPromptWanted)
            {
                Debug.Log("STAIRCASE: Player left the teleport area; prompt hidden.");
            }
        }

        if (promptWanted)
            PlacePromptAtPoint();

        if (promptCanvasGroup != null)
        {
            float targetAlpha = promptWanted ? 1f : 0f;
            promptCanvasGroup.alpha = Mathf.MoveTowards(promptCanvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 5f);
            promptCanvasGroup.interactable = false;
            promptCanvasGroup.blocksRaycasts = false;
        }

        if (xButtonVisual != null && promptWanted)
        {
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.2f) * 0.035f;
            xButtonVisual.localScale = buttonBaseScale * pulse;
        }

        // 8BitDo's X face button is Gamepad buttonWest. A rising edge avoids
        // repeated teleports while the button is held.
        if (promptWanted && Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            TryStartTeleport();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null || xrOrigin == null || other.GetComponentInParent<XROrigin>() != xrOrigin)
            return;

        playerColliders.Add(other);
        Debug.Log("STAIRCASE: Player entered the DOWN Box Collider trigger.");
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null || xrOrigin == null || other.GetComponentInParent<XROrigin>() != xrOrigin)
            return;

        playerColliders.Remove(other);
    }

    private void TryStartTeleport()
    {
        if (!promptWanted || mustExitTeleportAreas || transitionRunning || teleportStartedFrame == Time.frameCount)
            return;

        teleportStartedFrame = Time.frameCount;
        Debug.Log("STAIRCASE: Gamepad X (buttonWest) pressed inside teleport area.");
        StartCoroutine(Teleport(promptAtDown));
    }

    private void ResolveReferences()
    {
        if (teleportPointDown == null)
            teleportPointDown = transform;

        if (teleportPointUp == null)
        {
            GameObject point = GameObject.Find("teleport point X UP");
            if (point != null)
                teleportPointUp = point.transform;
        }

        if (xrOrigin == null)
            xrOrigin = FindFirstObjectByType<XROrigin>();

        if (playerCamera == null && xrOrigin != null)
            playerCamera = xrOrigin.GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;
    }

    private void ConfigureTrigger()
    {
        trigger = GetComponent<BoxCollider>();
        if (trigger == null)
            trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.enabled = true;
        trigger.center = triggerCenter;
        trigger.size = triggerSize;

        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
            body = gameObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.constraints = RigidbodyConstraints.FreezeAll;

        if (bidirectionalTeleport && teleportPointUp != null && teleportPointUp != transform)
        {
            upTrigger = teleportPointUp.GetComponent<BoxCollider>();
            if (upTrigger == null)
                upTrigger = teleportPointUp.gameObject.AddComponent<BoxCollider>();
            upTrigger.isTrigger = true;
            upTrigger.enabled = true;
            upTrigger.center = triggerCenter;
            upTrigger.size = triggerSize;

            Rigidbody upBody = teleportPointUp.GetComponent<Rigidbody>();
            if (upBody == null)
                upBody = teleportPointUp.gameObject.AddComponent<Rigidbody>();
            upBody.isKinematic = true;
            upBody.useGravity = false;
            upBody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private bool IsPlayerInsideTrigger(BoxCollider box, Vector3 playerPosition)
    {
        if (ContainsPoint(box, playerPosition))
            return true;

        // Some XR rigs move their camera independently of the Origin transform.
        return xrOrigin != null && ContainsPoint(box, xrOrigin.transform.position);
    }

    private static bool ContainsPoint(BoxCollider box, Vector3 worldPosition)
    {
        Vector3 localPosition = box.transform.InverseTransformPoint(worldPosition);
        return new Bounds(box.center, box.size).Contains(localPosition);
    }

    private void PlacePromptAtPoint()
    {
        if (promptCanvas == null)
            return;

        Transform anchor = promptAtDown ? teleportPointDown : teleportPointUp;
        if (anchor == null)
            return;

        if (promptText != null)
            promptText.text = promptAtDown ? "PRESS X TO GO UP" : "PRESS X TO GO DOWN";

        promptCanvas.transform.position = anchor.position + Vector3.up * worldPromptHeight;
        if (playerCamera != null)
        {
            Vector3 facing = playerCamera.transform.position - promptCanvas.transform.position;
            facing.y = 0f;
            if (facing.sqrMagnitude > 0.001f)
                promptCanvas.transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
        }
    }

    private IEnumerator Teleport(bool fromDown)
    {
        Transform destination = fromDown ? teleportPointUp : teleportPointDown;
        if (destination == null || xrOrigin == null)
        {
            Debug.LogError("STAIRCASE: Missing a teleport point or XR Origin reference.");
            yield break;
        }

        transitionRunning = true;
        promptWanted = false;
        if (promptCanvasGroup != null)
            promptCanvasGroup.alpha = 0f;
        Debug.Log(fromDown ? "STAIRCASE: X pressed; teleporting DOWN to UP." : "STAIRCASE: X pressed; teleporting UP to DOWN.");

        yield return FadeTo(fadeCanvasGroup, 1f, fadeDuration);
        xrOrigin.MoveCameraToWorldLocation(destination.position);
        playerColliders.Clear();
        mustExitTeleportAreas = true;
        Debug.Log(fromDown ? "STAIRCASE: XR Origin moved to Teleport Point X UP." : "STAIRCASE: XR Origin moved to Teleport Point X DOWN.");
        yield return new WaitForSecondsRealtime(0.08f);
        yield return FadeTo(fadeCanvasGroup, 0f, fadeDuration);

        transitionRunning = false;
    }

    private IEnumerator FadeTo(CanvasGroup group, float target, float duration)
    {
        if (group == null)
            yield break;

        float start = group.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.SmoothStep(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        group.alpha = target;
    }

    private void CreatePromptUi()
    {
        if (promptCanvas != null)
            return;

        GameObject canvasObject = new GameObject("Staircase X World Prompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        promptCanvas = canvasObject.GetComponent<Canvas>();
        promptCanvas.renderMode = RenderMode.WorldSpace;
        promptCanvas.worldCamera = playerCamera;
        promptCanvas.overrideSorting = true;
        promptCanvas.sortingOrder = 40;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(560f, 128f);
        canvasRect.localScale = Vector3.one * 0.0017f;

        promptCanvasGroup = canvasObject.GetComponent<CanvasGroup>();
        promptCanvasGroup.alpha = 0f;
        promptCanvasGroup.interactable = false;
        promptCanvasGroup.blocksRaycasts = false;

        GameObject panelObject = new GameObject("Prompt Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panelObject.transform.SetParent(canvasRect, false);
        RectTransform panelRect = (RectTransform)panelObject.transform;
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panel = panelObject.GetComponent<Image>();
        panel.color = new Color(0.012f, 0.024f, 0.042f, 0.88f);
        panel.raycastTarget = false;
        Outline outline = panelObject.GetComponent<Outline>();
        outline.effectColor = new Color(Cyan.r, Cyan.g, Cyan.b, 0.95f);
        outline.effectDistance = new Vector2(2f, 2f);
        outline.useGraphicAlpha = true;

        GameObject textObject = new GameObject("Direction Prompt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(panelRect, false);
        RectTransform textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10f, 4f);
        textRect.offsetMax = new Vector2(-10f, -4f);
        promptText = textObject.GetComponent<Text>();
        promptText.font = hudFont != null ? hudFont : Resources.GetBuiltinResource<Font>("Arial.ttf");
        promptText.text = "PRESS X TO GO UP";
        promptText.fontSize = 38;
        promptText.fontStyle = FontStyle.Bold;
        promptText.color = Color.white;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
        promptText.verticalOverflow = VerticalWrapMode.Overflow;
        promptText.raycastTarget = false;
    }

    private void CreateFadeUi()
    {
        if (fadeCanvasGroup != null || playerCamera == null)
            return;

        GameObject fadeObject = new GameObject("Staircase VR Fade", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        fadeObject.transform.SetParent(playerCamera.transform, false);
        fadeObject.transform.localPosition = new Vector3(0f, 0f, 0.28f);
        fadeObject.transform.localRotation = Quaternion.identity;
        fadeObject.transform.localScale = new Vector3(0.0022f, 0.0022f, 0.0022f);

        Canvas canvas = fadeObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = playerCamera;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

        RectTransform rect = fadeObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(1920f, 1080f);
        fadeCanvasGroup = fadeObject.GetComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
        fadeCanvasGroup.interactable = false;

        GameObject black = new GameObject("Black Fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        black.transform.SetParent(rect, false);
        RectTransform blackRect = (RectTransform)black.transform;
        blackRect.anchorMin = Vector2.zero;
        blackRect.anchorMax = Vector2.one;
        blackRect.offsetMin = Vector2.zero;
        blackRect.offsetMax = Vector2.zero;
        Image image = black.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
    }
}
