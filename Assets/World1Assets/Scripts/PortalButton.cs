using UnityEngine;
using UnityEngine.UI;
using Unity.XR.CoreUtils;

/// <summary>World-space UI portal activated by the same hand-pinch ray used for quiz answers.</summary>
[RequireComponent(typeof(Button))]
public class PortalButton : MonoBehaviour
{
    [SerializeField] private Transform destination;
    [SerializeField] private GameObject portalVisual;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.RemoveListener(Travel);
        button.onClick.AddListener(Travel);
    }

    public void Configure(Transform target)
    {
        destination = target;
    }

    public void Travel()
    {
        if (destination == null)
        {
            Debug.LogError("[PortalButton] Destination is not assigned.");
            return;
        }

        var origin = FindFirstObjectByType<XROrigin>();
        var cameraTransform = Camera.main != null ? Camera.main.transform : null;
        if (origin == null || cameraTransform == null)
        {
            Debug.LogError("[PortalButton] XR Origin or Main Camera is missing.");
            return;
        }

        Vector3 headOffset = cameraTransform.position - origin.transform.position;
        headOffset.y = 0f;
        origin.transform.position = destination.position - headOffset;

        Vector3 flatCameraForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 flatDestinationForward = Vector3.ProjectOnPlane(destination.forward, Vector3.up).normalized;
        if (flatCameraForward.sqrMagnitude > 0.001f && flatDestinationForward.sqrMagnitude > 0.001f)
        {
            float yaw = Vector3.SignedAngle(flatCameraForward, flatDestinationForward, Vector3.up);
            origin.transform.RotateAround(cameraTransform.position, Vector3.up, yaw);
        }

        if (portalVisual != null)
            portalVisual.SetActive(false);
        else
            gameObject.SetActive(false);

        Debug.Log("[PortalButton] Player moved to " + destination.name + ".");
    }
}