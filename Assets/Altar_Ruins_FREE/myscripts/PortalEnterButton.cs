using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(TMP_Text))]
public sealed class PortalEnterButton : MonoBehaviour
{
    private const string LinkId = "world1-enter";
    private const string LinkStart = "<link=\"world1-enter\"><u><b><color=#78E8FF>";
    private const string LinkEnd = "</color></b></u></link>";

    [SerializeField] private ScenePortal destinationPortal;
    [SerializeField, Min(0f)] private float hitPadding = 0.08f;

    private TMP_Text label;
    private XRSimpleInteractable buttonInteractable;
    private bool sceneRequested;

    private void OnValidate()
    {
        label = GetComponent<TMP_Text>();
        EnsureEnterLink();
    }

    private void Awake()
    {
        label = GetComponent<TMP_Text>();
        EnsureEnterLink();
        if (destinationPortal == null)
        {
            GameObject portalRoot = GameObject.Find("Portal Teleport Anchor");
            if (portalRoot != null)
                destinationPortal = portalRoot.GetComponentInChildren<ScenePortal>(true);
        }
    }

    private void Start()
    {
        if (label == null)
            label = GetComponent<TMP_Text>();

        if (destinationPortal == null)
        {
            Debug.LogError("[PortalEnterButton] Could not find the ScenePortal under 'Portal Teleport Anchor'.", this);
            return;
        }

        label.ForceMeshUpdate();
        if (!TryGetEnterBounds(out Bounds bounds))
        {
            Debug.LogError("[PortalEnterButton] The TMP label has no visible 'Enter' link to make clickable.", this);
            return;
        }

        Transform hitTarget = transform.Find("Enter Button Hit Target");
        if (hitTarget == null)
        {
            GameObject hitObject = new GameObject("Enter Button Hit Target");
            hitTarget = hitObject.transform;
            hitTarget.SetParent(transform, false);
        }

        hitTarget.localPosition = bounds.center;
        hitTarget.localRotation = Quaternion.identity;
        hitTarget.localScale = Vector3.one;

        BoxCollider hitbox = hitTarget.GetComponent<BoxCollider>();
        if (hitbox == null)
            hitbox = hitTarget.gameObject.AddComponent<BoxCollider>();

        float padding = Mathf.Max(hitPadding, label.fontSize * 0.01f);
        hitbox.center = Vector3.zero;
        hitbox.size = new Vector3(
            Mathf.Max(bounds.size.x + padding * 2f, 0.05f),
            Mathf.Max(bounds.size.y + padding * 2f, 0.05f),
            Mathf.Max(bounds.size.z + 0.08f, 0.08f));

        buttonInteractable = hitTarget.GetComponent<XRSimpleInteractable>();
        if (buttonInteractable == null)
            buttonInteractable = hitTarget.gameObject.AddComponent<XRSimpleInteractable>();
        buttonInteractable.interactionLayers = new InteractionLayerMask { value = -1 };
        buttonInteractable.selectEntered.AddListener(HandlePressed);

        Debug.Log("[PortalEnterButton] Enter is ready to load World1 through Portal Teleport Anchor.", this);
    }

    private void OnDestroy()
    {
        if (buttonInteractable != null)
            buttonInteractable.selectEntered.RemoveListener(HandlePressed);
    }

    private void EnsureEnterLink()
    {
        if (label == null || label.text.Contains("<link=\"" + LinkId + "\""))
            return;

        string currentText = label.text;
        int enterIndex = currentText.IndexOf("Enter", StringComparison.OrdinalIgnoreCase);
        if (enterIndex < 0)
            return;

        label.text = currentText.Insert(enterIndex + 5, LinkEnd).Insert(enterIndex, LinkStart);
        label.ForceMeshUpdate();
    }

    private bool TryGetEnterBounds(out Bounds bounds)
    {
        TMP_TextInfo textInfo = label.textInfo;
        for (int linkIndex = 0; linkIndex < textInfo.linkCount; ++linkIndex)
        {
            TMP_LinkInfo link = textInfo.linkInfo[linkIndex];
            if (link.GetLinkID() != LinkId)
                continue;

            Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            bool foundVisibleCharacter = false;

            int first = link.linkTextfirstCharacterIndex;
            int end = first + link.linkTextLength;
            for (int i = first; i < end && i < textInfo.characterCount; ++i)
            {
                TMP_CharacterInfo character = textInfo.characterInfo[i];
                if (!character.isVisible)
                    continue;

                min = Vector3.Min(min, character.bottomLeft);
                max = Vector3.Max(max, character.topRight);
                foundVisibleCharacter = true;
            }

            if (foundVisibleCharacter)
            {
                bounds = new Bounds((min + max) * 0.5f, max - min);
                return true;
            }
        }

        bounds = default;
        return false;
    }

    private void HandlePressed(SelectEnterEventArgs args)
    {
        if (sceneRequested || destinationPortal == null)
            return;

        sceneRequested = true;
        destinationPortal.TriggerSceneLoad();
    }
}
