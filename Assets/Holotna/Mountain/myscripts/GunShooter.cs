using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

public class GunShooter : MonoBehaviour
{
    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public GameObject stoneMonsterPrefab;
    public Transform muzzlePoint;
    public float shootForce = 35f;

    [Header("Right Hand Pose")]
    [HideInInspector] public bool alignMuzzleToMainCamera;
    [Tooltip("Rotation adjustment from the tracked wrist pose to the rifle grip.")]
    public Vector3 handRotationOffsetEuler;
    [Tooltip("Thumb and index tips must be closer than this distance to fire.")]
    [Min(0.005f)] public float pinchPressDistance = 0.025f;
    [Tooltip("The pinch must open beyond this distance before another shot can fire.")]
    [Min(0.01f)] public float pinchReleaseDistance = 0.045f;
    public bool hideRightHandMesh = true;

    private Transform playerRoot;
    private Transform handMount;
    private Transform attachTransform;
    private NearFarInteractor handInteractor;
    private ShootingGameManager gameManager;
    private Camera aimCamera;
    private XRHandSubsystem handSubsystem;
    private readonly List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>();
    private Vector3 attachLocalPosition;
    private Quaternion attachLocalRotation;
    private bool hasAttachPose;
    private bool wasPinching;
    private bool warnedAboutHandTracking;

    private void Start()
    {
        playerRoot = FindPlayerRigRoot();
        if (playerRoot == null)
        {
            Debug.LogError("[ShootingGame] Could not find the XR Origin; rifle setup stopped.", this);
            return;
        }

        aimCamera = FindAimCamera(playerRoot);
        handMount = FindRightHandMount(playerRoot);
        if (handMount == null)
        {
            Debug.LogError("[ShootingGame] Could not find the Right Hand object under the XR Origin.", this);
            return;
        }

        attachTransform = transform.Find("AttachTransform");
        transform.SetParent(handMount, false);
        transform.localScale = Vector3.one * 0.45f;
        transform.localRotation = Quaternion.identity;

        if (attachTransform != null)
        {
            attachLocalPosition = transform.InverseTransformPoint(attachTransform.position);
            attachLocalRotation = Quaternion.Inverse(transform.rotation) * attachTransform.rotation;
            hasAttachPose = true;
        }
        else
        {
            Debug.LogWarning("[ShootingGame] AttachTransform was not found; the rifle will follow the wrist origin.", this);
        }

        handInteractor = handMount.GetComponentInChildren<NearFarInteractor>(true);
        ConfigureHandRay();
        if (hideRightHandMesh)
            HideRightHandMesh();

        Rigidbody gunBody = GetComponent<Rigidbody>();
        if (gunBody != null)
        {
            gunBody.isKinematic = true;
            gunBody.useGravity = false;
        }

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

    private void LateUpdate()
    {
        if (playerRoot == null || handMount == null)
            return;

        if (handSubsystem == null || !handSubsystem.running)
        {
            handSubsystems.Clear();
            SubsystemManager.GetSubsystems(handSubsystems);
            handSubsystem = handSubsystems.Find(subsystem => subsystem != null && subsystem.running);
        }

        if (handSubsystem == null || !handSubsystem.rightHand.isTracked)
        {
            wasPinching = false;
            if (!warnedAboutHandTracking)
            {
                Debug.LogWarning("[ShootingGame] Waiting for tracked right-hand joints; the rifle will not switch to camera tracking.", this);
                warnedAboutHandTracking = true;
            }
            return;
        }

        XRHand rightHand = handSubsystem.rightHand;
        XRHandJoint wrist = rightHand.GetJoint(XRHandJointID.Wrist);
        XRHandJoint thumbTip = rightHand.GetJoint(XRHandJointID.ThumbTip);
        XRHandJoint indexTip = rightHand.GetJoint(XRHandJointID.IndexTip);
        if (!wrist.TryGetPose(out Pose wristPose))
        {
            wasPinching = false;
            return;
        }

        // XR Hands joint poses are local to the XR Origin tracking space.
        Quaternion trackedHandRotation = playerRoot.rotation * wristPose.rotation;
        Vector3 trackedHandPosition = playerRoot.TransformPoint(wristPose.position);
        Quaternion rifleRotation = trackedHandRotation * Quaternion.Euler(handRotationOffsetEuler);
        if (hasAttachPose)
        {
            rifleRotation *= Quaternion.Inverse(attachLocalRotation);
            transform.rotation = rifleRotation;
            transform.position = trackedHandPosition - rifleRotation * Vector3.Scale(attachLocalPosition, transform.lossyScale);
        }
        else
        {
            transform.SetPositionAndRotation(trackedHandPosition, rifleRotation);
        }

        if (!thumbTip.TryGetPose(out Pose thumbPose) || !indexTip.TryGetPose(out Pose indexPose))
        {
            wasPinching = false;
            return;
        }

        float tipDistance = Vector3.Distance(thumbPose.position, indexPose.position);
        if (wasPinching)
        {
            if (tipDistance > pinchReleaseDistance)
                wasPinching = false;
        }
        else if (tipDistance <= pinchPressDistance)
        {
            wasPinching = true;
            Fire();
        }
    }

    private Transform FindPlayerRigRoot()
    {
        for (Transform current = transform; current != null; current = current.parent)
        {
            if (current.GetComponent("XROrigin") != null || current.name.StartsWith("XR Origin", System.StringComparison.Ordinal))
                return current;
        }

        GameObject rig = GameObject.Find("XR Origin Hands (XR Rig)");
        return rig != null ? rig.transform : null;
    }

    private static Transform FindRightHandMount(Transform root)
    {
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == "Right Hand")
                return candidate;
        }

        return null;
    }

    private static Camera FindAimCamera(Transform root)
    {
        foreach (Camera candidate in root.GetComponentsInChildren<Camera>(true))
        {
            if (candidate.gameObject.name == "Main Camera")
                return candidate;
        }

        return Camera.main;
    }

    private void ConfigureHandRay()
    {
        if (muzzlePoint == null)
        {
            Debug.LogError("[ShootingGame] MuzzlePoint is not assigned; muzzle ray and shooting are unavailable.", this);
            return;
        }

        if (handInteractor != null && handInteractor.farInteractionCaster != null)
            handInteractor.farInteractionCaster.castOrigin = muzzlePoint;
        else if (handInteractor != null)
            Debug.LogWarning("[ShootingGame] NearFarInteractor has no far interaction caster; its ray origin cannot be moved to MuzzlePoint.", this);
        else
            Debug.LogWarning("[ShootingGame] No NearFarInteractor was found beneath Right Hand; the ray visual will still be redirected if present.", this);

        int visualCount = 0;
        foreach (CurveVisualController lineVisual in handMount.GetComponentsInChildren<CurveVisualController>(true))
        {
            lineVisual.overrideLineOrigin = true;
            lineVisual.lineOriginTransform = muzzlePoint;
            visualCount++;
        }

        if (visualCount == 0)
            Debug.LogWarning("[ShootingGame] No CurveVisualController was found under NearFarInteractor; only the interaction cast origin was redirected.", this);
        else
            Debug.Log("[ShootingGame] Right-hand ray origin and line visual now use MuzzlePoint.", this);
    }

    private void HideRightHandMesh()
    {
        foreach (Transform child in handMount.GetComponentsInChildren<Transform>(true))
        {
            string normalizedName = child.name.Replace(" ", string.Empty);
            if (child == transform ||
                (!normalizedName.Contains("RightHandQuestVisual") && !normalizedName.Contains("RightHandAndroidXRVisual")))
                continue;

            foreach (Renderer handRenderer in child.GetComponentsInChildren<Renderer>(true))
                handRenderer.enabled = false;
        }
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

        GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
        bullet.transform.localScale = Vector3.one * 0.1f;

        BulletProjectile projectile = bullet.GetComponent<BulletProjectile>();
        if (projectile == null)
            projectile = bullet.AddComponent<BulletProjectile>();
        projectile.Initialize();

        Rigidbody body = bullet.GetComponent<Rigidbody>();
        if (body == null)
            body = bullet.AddComponent<Rigidbody>();
        if (bullet.GetComponent<Collider>() == null)
            bullet.AddComponent<SphereCollider>();

        IgnoreShooterCollisions(bullet);
        body.isKinematic = false;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.AddForce(muzzlePoint.forward * shootForce, ForceMode.VelocityChange);

        Debug.Log("[ShootingGame] Right-hand pinch fired a projectile from MuzzlePoint.", this);
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
