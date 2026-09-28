using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class GunShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public GameObject stoneMonsterPrefab;
    public Transform muzzlePoint;
    public float shootForce = 35f;

    [Header("XR Hand Mount")]
    [Tooltip("When enabled, the muzzle follows the main camera's forward direction while the rifle stays positioned under its hand parent.")]
    public bool alignMuzzleToMainCamera = true;
    [Tooltip("Hide the right-hand mesh so the rifle visually replaces it.")]
    public bool hideRightHandMesh = true;

    private XRGrabInteractable grabInteractable;
    private Camera aimCamera;
    private Transform handMount;
    private Transform playerRoot;
    private Transform attachTransform;
    private NearFarInteractor handInteractor;
    private ShootingGameManager gameManager;
    private bool cameraMountFallback;

    void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
    }

    void OnEnable()
    {
        // Listen for the select/activation event (trigger press while holding)
        if (grabInteractable != null)
            grabInteractable.activated.AddListener(Shoot);
    }

    void OnDisable()
    {
        if (grabInteractable != null)
            grabInteractable.activated.RemoveListener(Shoot);
    }

    void Start()
    {
        playerRoot = FindPlayerRigRoot();
        FindAimCamera(playerRoot);
        attachTransform = transform.Find("AttachTransform");

        if (playerRoot == null)
        {
            Debug.LogError("[ShootingGame] Could not find the XR Origin or Main Camera. The wave manager was not started.", this);
            return;
        }

        handMount = FindRightHandMount(playerRoot);
        if (handMount != null)
        {
            if (transform.parent != handMount)
                transform.SetParent(handMount, false);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.45f;
            cameraMountFallback = aimCamera != null && handMount == aimCamera.transform;
            if (cameraMountFallback)
            {
                transform.localPosition = new Vector3(0.28f, -0.24f, 0.72f);
                Debug.Log("[ShootingGame] No tracked right hand/controller is active; mounting the rifle in the camera view for preview.", this);
            }
            else if (attachTransform != null)
            {
                transform.localPosition = -Vector3.Scale(attachTransform.localPosition, transform.localScale);
            }
        }
        else if (handMount == null)
        {
            Debug.LogError("[ShootingGame] Could not find 'Right Hand' beneath the XR Origin. The rifle could not mount to the hand.", this);
        }

        ConfigureHandRay();
        if (hideRightHandMesh)
            HideRightHandMesh();

        // This rifle is permanently hand-mounted; physics/grab state must not detach it from tracking.
        Rigidbody gunBody = GetComponent<Rigidbody>();
        if (gunBody != null)
        {
            gunBody.isKinematic = true;
            gunBody.useGravity = false;
        }
        if (grabInteractable != null)
            grabInteractable.enabled = false;

        gameManager = playerRoot.GetComponent<ShootingGameManager>();
        if (gameManager == null)
            gameManager = playerRoot.gameObject.AddComponent<ShootingGameManager>();
        if (stoneMonsterPrefab == null)
        {
            Debug.LogError("[ShootingGame] Stone Monster Prefab is not assigned on GunShooter.", this);
            return;
        }

        gameManager.Initialize(stoneMonsterPrefab, playerRoot, aimCamera, muzzlePoint);
    }

    void LateUpdate()
    {
        if (handInteractor != null && handInteractor.activateInput.ReadWasPerformedThisFrame() &&
            (grabInteractable == null || !grabInteractable.isSelected))
            Fire();

        // Let the camera-mounted preview rifle be tested in the Editor without an XR controller.
        if (cameraMountFallback && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            Fire();

        if (!alignMuzzleToMainCamera || muzzlePoint == null)
            return;

        if (aimCamera == null)
            FindAimCamera(FindPlayerRigRoot());

        if (aimCamera != null)
        {
            Quaternion aimCorrection = Quaternion.FromToRotation(muzzlePoint.forward, aimCamera.transform.forward);
            transform.rotation = aimCorrection * transform.rotation;

            // Keep the rifle's attach point planted at the right hand while its aim follows the head camera.
            if (!cameraMountFallback && handMount != null && attachTransform != null)
                transform.position += handMount.position - attachTransform.position;
        }

    }

    private Transform FindRightHandMount(Transform playerRoot)
    {
        if (playerRoot == null)
            return null;

        Transform trackedRightHand = null;
        foreach (Transform candidate in playerRoot.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == "Right Hand")
            {
                trackedRightHand = candidate;
                if (candidate.gameObject.activeInHierarchy || !Application.isEditor)
                    return candidate;
            }
        }

        if (trackedRightHand != null && !Application.isEditor)
            return trackedRightHand;

        foreach (Camera candidate in playerRoot.GetComponentsInChildren<Camera>(true))
        {
            if (candidate.gameObject.name == "Main Camera")
                return candidate.transform;
        }

        return null;
    }

    private Transform FindPlayerRigRoot()
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.GetComponent("XROrigin") != null || current.name.StartsWith("XR Origin", System.StringComparison.Ordinal))
                return current;
        }

        GameObject rig = GameObject.Find("XR Origin Hands (XR Rig)");
        if (rig != null)
            return rig.transform;

        return aimCamera != null ? aimCamera.transform : null;
    }

    private void ConfigureHandRay()
    {
        if (handMount == null || muzzlePoint == null)
            return;

        handInteractor = handMount.GetComponentInChildren<NearFarInteractor>(true);
        if (handInteractor == null)
            return;

        if (handInteractor.farInteractionCaster != null)
            handInteractor.farInteractionCaster.castOrigin = muzzlePoint;

        foreach (CurveVisualController lineVisual in handInteractor.GetComponentsInChildren<CurveVisualController>(true))
        {
            lineVisual.overrideLineOrigin = true;
            lineVisual.lineOriginTransform = muzzlePoint;
        }
    }

    private void FindAimCamera(Transform playerRoot)
    {
        if (playerRoot != null)
        {
            foreach (Camera rigCamera in playerRoot.GetComponentsInChildren<Camera>(true))
            {
                if (rigCamera.gameObject.name == "Main Camera")
                {
                    aimCamera = rigCamera;
                    return;
                }
            }
        }

        aimCamera = Camera.main;
    }

    private void HideRightHandMesh()
    {
        Transform hand = transform.parent;
        if (hand == null)
            return;

        foreach (Transform child in hand.GetComponentsInChildren<Transform>(true))
        {
            string normalizedName = child.name.Replace(" ", string.Empty);
            if (child == transform ||
                (!normalizedName.Contains("RightHandQuestVisual") && !normalizedName.Contains("RightHandAndroidXRVisual")))
                continue;

            foreach (Renderer handRenderer in child.GetComponentsInChildren<Renderer>(true))
                handRenderer.enabled = false;
        }
    }

    private void Shoot(ActivateEventArgs args)
    {
        Fire();
    }

    private void Fire()
    {
        if (gameManager != null && !gameManager.CanShoot)
            return;

        if (bulletPrefab == null || muzzlePoint == null)
        {
            Debug.LogError("[ShootingGame] Cannot fire: assign both Bullet Prefab and MuzzlePoint on GunShooter.", this);
            return;
        }

        // Spawn at the muzzle and travel along its (camera-aligned) forward axis.
        GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        bullet.transform.localScale = Vector3.one * 0.1f;

        BulletProjectile projectile = bullet.GetComponent<BulletProjectile>();
        if (projectile == null)
            projectile = bullet.AddComponent<BulletProjectile>();
        projectile.Initialize();

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null)
            rb = bullet.AddComponent<Rigidbody>();
        if (bullet.GetComponent<Collider>() == null)
            bullet.AddComponent<SphereCollider>();
        IgnoreShooterCollisions(bullet);
        rb.isKinematic = false;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.AddForce(muzzlePoint.forward * shootForce, ForceMode.VelocityChange);

        Debug.Log("[ShootingGame] Fired projectile from MuzzlePoint.", this);
        Destroy(bullet, 3f);
    }

    private void IgnoreShooterCollisions(GameObject bullet)
    {
        Collider[] bulletColliders = bullet.GetComponentsInChildren<Collider>(true);
        if (playerRoot != null)
        {
            foreach (Collider playerCollider in playerRoot.GetComponentsInChildren<Collider>(true))
            {
                if (playerCollider == null || !playerCollider.enabled)
                    continue;

                foreach (Collider bulletCollider in bulletColliders)
                {
                    if (bulletCollider != null && bulletCollider.enabled)
                        Physics.IgnoreCollision(bulletCollider, playerCollider, true);
                }
            }
        }

        foreach (Collider gunCollider in GetComponentsInChildren<Collider>(true))
        {
            if (gunCollider == null || !gunCollider.enabled)
                continue;

            foreach (Collider bulletCollider in bulletColliders)
            {
                if (bulletCollider != null && bulletCollider.enabled)
                    Physics.IgnoreCollision(bulletCollider, gunCollider, true);
            }
        }
    }
}
