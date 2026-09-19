using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UltimateXR.Animation.Interpolation;
using UltimateXR.Avatar;
using UltimateXR.Avatar.Controllers;
using UltimateXR.Core.Caching;
using UltimateXR.Core.Components.Singleton;
using UltimateXR.Extensions.System.Threading;
using UltimateXR.Extensions.Unity;
using UltimateXR.Extensions.Unity.Math;
using UltimateXR.Extensions.Unity.Render;
using UltimateXR.Locomotion;
using UltimateXR.Manipulation;
using UltimateXR.Mechanics.Weapons;
using UltimateXR.UI.UnityInputModule;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateXR.Core
{
    /// <summary>
    /// Main manager in the UltimateXR framework.
    /// </summary>
    public sealed class UxrManager : UxrSingleton<UxrManager>
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] private UxrPostUpdateMode _postUpdateMode = UxrPostUpdateMode.LateUpdate;
        [SerializeField] private bool _usePrecaching = true;
        [SerializeField] private int _precacheFrameCount = 50;

        #endregion

        #region Public Types & Data

        public const string SdkUnityInputSystem = "Unity Input System";
        public const string SdkOculus = "Oculus";
        public const string SdkSteamVR = "SteamVR";
        public const string SdkWindowsMixedReality = "Windows Mixed Reality";
        public const string SdkUltraleap = "Ultraleap";
        public const string SdkWaveXR = "WaveXR";
        public const string SdkPicoXR = "PicoXR";
        public const string SdkMagicLeap = "Magic Leap";

        public static event Action PrecachingStarting;
        public static event Action PrecachingFinished;
        public static event Action AvatarsUpdating;
        public static event Action AvatarsUpdated;
        public static event Action<UxrUpdateStage> StageUpdating;
        public static event Action<UxrUpdateStage> StageUpdated;
        public static event EventHandler<UxrAvatarMoveEventArgs> AvatarMoving;
        public static event EventHandler<UxrAvatarMoveEventArgs> AvatarMoved;

        public bool IsPrecaching => _precacheCoroutine != null;

        public UxrPostUpdateMode PostUpdateMode
        {
            get => _postUpdateMode;
            set => _postUpdateMode = value;
        }

        public bool UsePrecaching
        {
            get => _usePrecaching;
            set => _usePrecaching = value;
        }

        public int PrecacheFrameCount
        {
            get => _precacheFrameCount;
            set => _precacheFrameCount = value;
        }

        public Color TeleportFadeColor { get; set; } = default;

        #endregion

        #region Public Methods

        public static void LogMissingAvatarInHierarchyError(Component component)
        {
            Debug.LogError(
                $"{component.GetType().Name} requires to be part of an {nameof(UxrAvatar)} in order to work correctly. GameObject is {component.GetPathUnderScene()}.");
        }

        public static void LogMissingAvatarInScene(Component component)
        {
            Debug.LogError(
                $"{component.GetType().Name} requires an avatar in the scene to work correctly. GameObject is {component.GetPathUnderScene()}.");
        }

        public void TranslateAvatar(UxrAvatar avatar, Vector3 translation, bool propagateEvents = true)
        {
            MoveAvatarTo(
                avatar,
                avatar.CameraFloorPosition + translation,
                avatar.ProjectedCameraForward,
                propagateEvents);
        }

        public void MoveAvatarTo(UxrAvatar avatar, Vector3 newFloorPosition, bool propagateEvents = true)
        {
            MoveAvatarTo(
                avatar,
                newFloorPosition,
                avatar.ProjectedCameraForward,
                propagateEvents);
        }

        public void MoveAvatarTo(
            UxrAvatar avatar,
            Vector3 newFloorPosition,
            Vector3 newForward,
            bool propagateEvents = true)
        {
            Transform avatarTransform = avatar.transform;

            Vector3 oldPosition = avatarTransform.position;
            Quaternion oldRotation = avatarTransform.rotation;

            Vector3 newPosition = oldPosition;
            Quaternion newRotation = oldRotation;

            TransformExt.ApplyAlignment(
                ref newPosition,
                ref newRotation,
                avatar.CameraFloorPosition,
                Quaternion.LookRotation(avatar.ProjectedCameraForward),
                newFloorPosition,
                Quaternion.LookRotation(newForward),
                true,
                true);

            if (propagateEvents)
            {
                OnAvatarMoving(
                    new UxrAvatarMoveEventArgs(
                        avatar,
                        oldPosition,
                        oldRotation,
                        newPosition,
                        newRotation));
            }

            avatarTransform.SetPositionAndRotation(newPosition, newRotation);

            if (propagateEvents)
            {
                OnAvatarMoved(
                    new UxrAvatarMoveEventArgs(
                        avatar,
                        oldPosition,
                        oldRotation,
                        newPosition,
                        newRotation));
            }
        }

        public void MoveAvatarTo(
            UxrAvatar avatar,
            Transform destination,
            bool propagateEvents = true)
        {
            if (avatar && destination)
            {
                MoveAvatarTo(
                    avatar,
                    destination.position,
                    destination.forward,
                    propagateEvents);
            }
        }

        public void MoveAvatarTo(
            UxrAvatar avatar,
            float floorLevel,
            bool propagateEvents = true)
        {
            if (avatar)
            {
                Vector3 newPosition = avatar.CameraFloorPosition;
                newPosition.y = floorLevel;

                MoveAvatarTo(
                    avatar,
                    newPosition,
                    propagateEvents);
            }
        }

        public void RotateAvatar(
            UxrAvatar avatar,
            float degrees,
            bool propagateEvents = true)
        {
            Transform avatarTransform = avatar.transform;

            MoveAvatarTo(
                avatar,
                avatar.CameraFloorPosition,
                avatar.ProjectedCameraForward.GetRotationAround(
                    avatarTransform.up,
                    degrees),
                propagateEvents);
        }

        public void TeleportLocalAvatar(
            Vector3 newFloorPosition,
            Quaternion newRotation,
            UxrTranslationType translationType = UxrTranslationType.Immediate,
            float transitionSeconds = UxrConstants.TeleportTranslationSeconds,
            Action teleportedCallback = null,
            Action<bool> finishedCallback = null,
            bool propagateEvents = true)
        {
            if (_teleportCoroutine != null)
            {
                StopCoroutine(_teleportCoroutine);
            }

            bool hasFinished = false;

            _teleportCoroutine = StartCoroutine(
                TeleportLocalAvatarCoroutine(
                    newFloorPosition,
                    newRotation,
                    translationType,
                    transitionSeconds,
                    teleportedCallback,
                    () => hasFinished = true,
                    propagateEvents));

            finishedCallback?.Invoke(hasFinished);
        }

        public void TeleportLocalAvatarRelative(
            Transform referenceTransform,
            bool parentToReference,
            Vector3 newFloorPosition,
            Quaternion newRotation,
            UxrTranslationType translationType = UxrTranslationType.Immediate,
            float transitionSeconds = UxrConstants.TeleportTranslationSeconds,
            Action teleportedCallback = null,
            Action<bool> finishedCallback = null,
            bool propagateEvents = true)
        {
            if (_teleportCoroutine != null)
            {
                StopCoroutine(_teleportCoroutine);
            }

            Vector3 newRelativeFloorPosition =
                referenceTransform != null
                    ? referenceTransform.InverseTransformPoint(newFloorPosition)
                    : newFloorPosition;

            Quaternion newRelativeRotation =
                referenceTransform != null
                    ? Quaternion.Inverse(referenceTransform.rotation) * newRotation
                    : newRotation;

            bool hasFinished = false;

            _teleportCoroutine = StartCoroutine(
                TeleportLocalAvatarRelativeCoroutine(
                    referenceTransform,
                    parentToReference,
                    newRelativeFloorPosition,
                    newRelativeRotation,
                    translationType,
                    transitionSeconds,
                    teleportedCallback,
                    () => hasFinished = true,
                    propagateEvents));

            finishedCallback?.Invoke(hasFinished);
        }

        public async Task TeleportLocalAvatarAsync(
            Vector3 newFloorPosition,
            Quaternion newRotation,
            UxrTranslationType translationType = UxrTranslationType.Immediate,
            float transitionSeconds = UxrConstants.TeleportTranslationSeconds,
            Action teleportedCallback = null,
            CancellationToken ct = default,
            bool propagateEvents = true)
        {
            bool finished = false;

            Coroutine teleportCoroutine = StartCoroutine(
                TeleportLocalAvatarCoroutine(
                    newFloorPosition,
                    newRotation,
                    translationType,
                    transitionSeconds,
                    teleportedCallback,
                    () => finished = true,
                    propagateEvents));

            await TaskExt.WaitUntil(() => finished, ct);

            if (ct.IsCancellationRequested)
            {
                StopCoroutine(teleportCoroutine);
            }
        }

        public async Task TeleportLocalAvatarRelativeAsync(
            Transform referenceTransform,
            bool parentToReference,
            Vector3 newFloorPosition,
            Quaternion newRotation,
            UxrTranslationType translationType = UxrTranslationType.Immediate,
            float transitionSeconds = UxrConstants.TeleportTranslationSeconds,
            Action teleportedCallback = null,
            CancellationToken ct = default,
            bool propagateEvents = true)
        {
            Vector3 newRelativeFloorPosition =
                referenceTransform != null
                    ? referenceTransform.InverseTransformPoint(newFloorPosition)
                    : newFloorPosition;

            Quaternion newRelativeRotation =
                referenceTransform != null
                    ? Quaternion.Inverse(referenceTransform.rotation) * newRotation
                    : newRotation;

            bool hasFinished = false;

            Coroutine teleportCoroutine = StartCoroutine(
                TeleportLocalAvatarRelativeCoroutine(
                    referenceTransform,
                    parentToReference,
                    newRelativeFloorPosition,
                    newRelativeRotation,
                    translationType,
                    transitionSeconds,
                    teleportedCallback,
                    () => hasFinished = true,
                    propagateEvents));

            await TaskExt.WaitUntil(() => hasFinished, ct);

            if (ct.IsCancellationRequested)
            {
                StopCoroutine(teleportCoroutine);
            }
        }

        public void RotateLocalAvatar(
            float degrees,
            UxrRotationType rotationType = UxrRotationType.Immediate,
            float transitionSeconds = UxrConstants.TeleportRotationSeconds,
            Action rotatedCallback = null,
            Action<bool> finishedCallback = null,
            bool propagateEvents = true)
        {
            if (_teleportCoroutine != null)
            {
                StopCoroutine(_teleportCoroutine);
            }

            bool hasFinished = false;

            _teleportCoroutine = StartCoroutine(
                RotateLocalAvatarCoroutine(
                    degrees,
                    rotationType,
                    transitionSeconds,
                    rotatedCallback,
                    () => hasFinished = true,
                    propagateEvents));

            finishedCallback?.Invoke(hasFinished);
        }

        public async Task RotateLocalAvatarAsync(
            float degrees,
            UxrRotationType rotationType = UxrRotationType.Immediate,
            float transitionSeconds = UxrConstants.TeleportRotationSeconds,
            Action rotatedCallback = null,
            CancellationToken ct = default,
            bool propagateEvents = true)
        {
            bool finished = false;

            Coroutine rotateCoroutine = StartCoroutine(
                RotateLocalAvatarCoroutine(
                    degrees,
                    rotationType,
                    transitionSeconds,
                    rotatedCallback,
                    () => finished = true,
                    propagateEvents));

            await TaskExt.WaitUntil(() => finished, ct);

            if (ct.IsCancellationRequested)
            {
                StopCoroutine(rotateCoroutine);
            }
        }

        #endregion

        #region Unity

        protected override void Awake()
        {
            base.Awake();

            UxrAvatar.GlobalEnabled += Avatar_Enabled;
            SceneManager.sceneLoaded += SceneManager_SceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_SceneUnloaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            UxrAvatar.GlobalEnabled -= Avatar_Enabled;
            SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            SceneManager.sceneUnloaded -= SceneManager_SceneUnloaded;

            DestroyPrecachedInstances();
        }

        protected override void Start()
        {
            SetupCanvases();
        }

        private void Update()
        {
            OnUpdating();
            OnUpdatingStage(UxrUpdateStage.Update);

            foreach (UxrAvatarController avatarController in LocalAvatarControllers)
            {
                OnAvatarUpdating(
                    avatarController.Avatar,
                    new UxrAvatarUpdateEventArgs(
                        avatarController.Avatar,
                        UxrUpdateStage.Update));

                ((IUxrAvatarControllerUpdater)avatarController).UpdateAvatar();

                OnAvatarUpdated(
                    avatarController.Avatar,
                    new UxrAvatarUpdateEventArgs(
                        avatarController.Avatar,
                        UxrUpdateStage.Update));
            }

            OnStageUpdated(UxrUpdateStage.Update);

            if (PostUpdateMode == UxrPostUpdateMode.Update)
            {
                PostUpdate();
            }
        }

        private void LateUpdate()
        {
            if (PostUpdateMode == UxrPostUpdateMode.LateUpdate)
            {
                PostUpdate();
            }
        }

        #endregion

        #region Coroutines

        public IEnumerator TeleportLocalAvatarCoroutine(
            Vector3 newFloorPosition,
            Quaternion newRotation,
            UxrTranslationType translationType = UxrTranslationType.Immediate,
            float transitionSeconds = UxrConstants.TeleportTranslationSeconds,
            Action teleportedCallback = null,
            Action finishedCallback = null,
            bool propagateEvents = true)
        {
            yield return TeleportLocalAvatarRelativeCoroutine(
                null,
                false,
                newFloorPosition,
                newRotation,
                translationType,
                transitionSeconds,
                teleportedCallback,
                finishedCallback,
                propagateEvents);
        }

        public IEnumerator TeleportLocalAvatarRelativeCoroutine(
            Transform referenceTransform,
            bool parentToReference,
            Vector3 newRelativeFloorPosition,
            Quaternion newRelativeRotation,
            UxrTranslationType translationType = UxrTranslationType.Immediate,
            float transitionSeconds = UxrConstants.TeleportTranslationSeconds,
            Action teleportedCallback = null,
            Action finishedCallback = null,
            bool propagateEvents = true)
        {
            if (UxrAvatar.LocalAvatar)
            {
                Vector3 oldFloorPosition =
                    UxrAvatar.LocalAvatar.CameraFloorPosition;

                Quaternion oldFloorRotation =
                    Quaternion.LookRotation(
                        UxrAvatar.LocalAvatar.ProjectedCameraForward);

                Quaternion inverseReferenceRotation =
                    referenceTransform != null
                        ? Quaternion.Inverse(referenceTransform.rotation)
                        : Quaternion.identity;

                Matrix4x4 inverseReferenceMatrix =
                    referenceTransform != null
                        ? referenceTransform.localToWorldMatrix.inverse
                        : Matrix4x4.identity;

                Vector3 oldRelativePosition =
                    inverseReferenceMatrix * oldFloorPosition;

                Quaternion oldRelativeRotation =
                    inverseReferenceRotation * oldFloorRotation;

                void TranslateAvatarInternal(float t = 1.0f)
                {
                    Vector3 newPos =
                        Vector3.Lerp(
                            oldRelativePosition,
                            newRelativeFloorPosition,
                            t);

                    Quaternion newRot = oldRelativeRotation;

                    if (Mathf.Approximately(t, 1.0f))
                    {
                        newRot = newRelativeRotation;
                    }

                    if (referenceTransform != null)
                    {
                        newPos =
                            referenceTransform.TransformPoint(newPos);

                        newRot =
                            referenceTransform.rotation * newRot;
                    }

                    MoveAvatarTo(
                        UxrAvatar.LocalAvatar,
                        newPos,
                        newRot * Vector3.forward,
                        propagateEvents);
                }

                switch (translationType)
                {
                    case UxrTranslationType.Immediate:

                        TranslateAvatarInternal();
                        teleportedCallback?.Invoke();

                        break;

                    case UxrTranslationType.Fade:

                        yield return UxrAvatar.LocalAvatar.CameraFade
                            .StartFadeCoroutine(
                                transitionSeconds * 0.5f,
                                TeleportFadeColor.WithAlpha(0.0f),
                                TeleportFadeColor.WithAlpha(1.0f));

                        TranslateAvatarInternal();
                        teleportedCallback?.Invoke();

                        yield return null;

                        yield return UxrAvatar.LocalAvatar.CameraFade
                            .StartFadeCoroutine(
                                transitionSeconds * 0.5f,
                                TeleportFadeColor.WithAlpha(1.0f),
                                TeleportFadeColor.WithAlpha(0.0f));

                        break;

                    case UxrTranslationType.Smooth:

                        yield return this.LoopCoroutine(
                            transitionSeconds,
                            TranslateAvatarInternal,
                            UxrEasing.Linear,
                            true);

                        break;
                }

                if (parentToReference && referenceTransform != null)
                {
                    UxrAvatar.LocalAvatar.transform.SetParent(
                        referenceTransform);
                }
            }

            _teleportCoroutine = null;
            finishedCallback?.Invoke();
        }

        public IEnumerator RotateLocalAvatarCoroutine(
            float degrees,
            UxrRotationType rotationType = UxrRotationType.Immediate,
            float transitionSeconds = UxrConstants.TeleportRotationSeconds,
            Action rotatedCallback = null,
            Action finishedCallback = null,
            bool propagateEvents = true)
        {
            if (UxrAvatar.LocalAvatar)
            {
                void RotateAvatarInternal(float t = 1.0f)
                {
                    Transform avatarTransform =
                        UxrAvatar.LocalAvatar.transform;

                    Vector3 initialForward =
                        UxrAvatar.LocalAvatar.ProjectedCameraForward;

                    MoveAvatarTo(
                        UxrAvatar.LocalAvatar,
                        UxrAvatar.LocalAvatar.CameraFloorPosition,
                        initialForward.GetRotationAround(
                            avatarTransform.up,
                            degrees * t),
                        propagateEvents);
                }

                switch (rotationType)
                {
                    case UxrRotationType.Immediate:

                        RotateAvatarInternal();
                        rotatedCallback?.Invoke();

                        break;

                    case UxrRotationType.Fade:

                        yield return UxrAvatar.LocalAvatar.CameraFade
                            .StartFadeCoroutine(
                                transitionSeconds * 0.5f,
                                TeleportFadeColor.WithAlpha(0.0f),
                                TeleportFadeColor.WithAlpha(1.0f));

                        RotateAvatarInternal();
                        rotatedCallback?.Invoke();

                        yield return null;

                        yield return UxrAvatar.LocalAvatar.CameraFade
                            .StartFadeCoroutine(
                                transitionSeconds * 0.5f,
                                TeleportFadeColor.WithAlpha(1.0f),
                                TeleportFadeColor.WithAlpha(0.0f));

                        break;

                    case UxrRotationType.Smooth:

                        yield return this.LoopCoroutine(
                            transitionSeconds,
                            RotateAvatarInternal,
                            UxrEasing.Linear,
                            true);

                        break;
                }
            }

            _teleportCoroutine = null;
            finishedCallback?.Invoke();
        }

        private IEnumerator PrecacheCoroutine(
            Action onStarting = null,
            Action onFinished = null)
        {
            UxrAvatar avatar = UxrAvatar.LocalAvatar;

            while (avatar == null)
            {
                yield return null;
                avatar = UxrAvatar.LocalAvatar;
            }

            onStarting?.Invoke();

            DestroyPrecachedInstances();

            // UNITY 6 FIX:
            // Use EntityId instead of int.
            _dynamicInstances =
                new Dictionary<EntityId, GameObject>();

            for (int sceneIndex = 0;
                 sceneIndex < SceneManager.sceneCount;
                 ++sceneIndex)
            {
                Scene scene =
                    SceneManager.GetSceneAt(sceneIndex);

                AddScenePrecachedInstances(
                    _dynamicInstances,
                    scene,
                    avatar);
            }

            AddScenePrecachedInstances(
                _dynamicInstances,
                Instance.gameObject.scene,
                avatar);

            for (int frame = 0;
                 frame < _precacheFrameCount;
                 ++frame)
            {
                avatar.CameraFade.EnableFadeColor(
                    Color.black,
                    1.0f);

                yield return null;
            }

            DestroyPrecachedInstances();

            onFinished?.Invoke();

            float startFadeTime = Time.time;
            float fadeDuration = 0.5f;

            while (Time.time - startFadeTime < fadeDuration)
            {
                avatar.CameraFade.EnableFadeColor(
                    Color.black,
                    1.0f -
                    (Time.time - startFadeTime) /
                    fadeDuration);

                yield return null;
            }

            avatar.CameraFade.DisableFadeColor();

            _precacheCoroutine = null;
        }

        #endregion

        #region Event Handling Methods

        private void Avatar_Enabled(UxrAvatar avatar)
        {
            if (avatar.AvatarMode == UxrAvatarMode.Local)
            {
                if (UxrPointerInputModule.Instance != null &&
                    UxrPointerInputModule.Instance.AutoAssignEventCamera)
                {
                    foreach (UxrCanvas canvas in UxrCanvas.AllComponents)
                    {
                        if (canvas.UnityCanvas)
                        {
                            canvas.UnityCanvas.worldCamera =
                                avatar.CameraComponent;
                        }
                    }
                }

                TryPrecaching();
            }
        }

        private void SceneManager_SceneLoaded(
            Scene scene,
            LoadSceneMode loadSceneMode)
        {
            SetupCanvases();
        }

        private void SceneManager_SceneUnloaded(Scene scene)
        {
            SetupCanvases();
        }

        #endregion

        #region Event Trigger Methods

        private void OnPrecachingStarting()
        {
            PrecachingStarting?.Invoke();
        }

        private void OnPrecachingFinished()
        {
            PrecachingFinished?.Invoke();
        }

        private void OnAvatarMoving(UxrAvatarMoveEventArgs args)
        {
            AvatarMoving?.Invoke(this, args);
        }

        private void OnAvatarMoved(UxrAvatarMoveEventArgs args)
        {
            AvatarMoved?.Invoke(this, args);
            args.Avatar.NotifyAvatarMoved(args);
        }

        private void OnUpdating()
        {
            AvatarsUpdating?.Invoke();
        }

        private void OnUpdated()
        {
            AvatarsUpdated?.Invoke();
        }

        private void OnUpdatingStage(UxrUpdateStage stage)
        {
            StageUpdating?.Invoke(stage);
        }

        private void OnStageUpdated(UxrUpdateStage stage)
        {
            StageUpdated?.Invoke(stage);
        }

        private void OnAvatarUpdating(
            UxrAvatar avatar,
            UxrAvatarUpdateEventArgs e)
        {
            avatar.RaiseAvatarUpdating(e);
        }

        private void OnAvatarUpdated(
            UxrAvatar avatar,
            UxrAvatarUpdateEventArgs e)
        {
            avatar.RaiseAvatarUpdated(e);
        }

        #endregion

        #region Private Methods

        private void PostUpdate()
        {
            OnUpdatingStage(
                UxrUpdateStage.AvatarUsingTracking);

            foreach (UxrAvatar avatar in UxrAvatar.EnabledComponents)
            {
                if (avatar.AvatarMode == UxrAvatarMode.Local &&
                    avatar.AvatarController is
                    { enabled: true } avatarController)
                {
                    OnAvatarUpdating(
                        avatar,
                        new UxrAvatarUpdateEventArgs(
                            avatar,
                            UxrUpdateStage.AvatarUsingTracking));

                    ((IUxrAvatarControllerUpdater)
                        avatarController)
                        .UpdateAvatarUsingTrackingDevices();

                    OnAvatarUpdated(
                        avatar,
                        new UxrAvatarUpdateEventArgs(
                            avatar,
                            UxrUpdateStage.AvatarUsingTracking));
                }
            }

            OnStageUpdated(
                UxrUpdateStage.AvatarUsingTracking);

            OnUpdatingStage(
                UxrUpdateStage.Manipulation);

            foreach (UxrAvatarController avatarController
                     in LocalAvatarControllers)
            {
                OnAvatarUpdating(
                    avatarController.Avatar,
                    new UxrAvatarUpdateEventArgs(
                        avatarController.Avatar,
                        UxrUpdateStage.Manipulation));
            }

            UxrGrabManager.Instance.UpdateManager();
            UxrWeaponManager.Instance.UpdateManager();

            foreach (UxrAvatarController avatarController
                     in LocalAvatarControllers)
            {
                ((IUxrAvatarControllerUpdater)
                    avatarController)
                    .UpdateAvatarManipulation();
            }

            foreach (UxrAvatarController avatarController
                     in LocalAvatarControllers)
            {
                OnAvatarUpdated(
                    avatarController.Avatar,
                    new UxrAvatarUpdateEventArgs(
                        avatarController.Avatar,
                        UxrUpdateStage.Manipulation));
            }

            OnStageUpdated(
                UxrUpdateStage.Manipulation);

            OnUpdatingStage(
                UxrUpdateStage.Animation);

            foreach (UxrAvatar avatar in UxrAvatar.EnabledComponents)
            {
                if (avatar.AvatarMode == UxrAvatarMode.Local &&
                    avatar.AvatarController is
                    { enabled: true } avatarController)
                {
                    OnAvatarUpdating(
                        avatar,
                        new UxrAvatarUpdateEventArgs(
                            avatar,
                            UxrUpdateStage.Animation));

                    ((IUxrAvatarControllerUpdater)
                        avatarController)
                        .UpdateAvatarAnimation();

                    OnAvatarUpdated(
                        avatar,
                        new UxrAvatarUpdateEventArgs(
                            avatar,
                            UxrUpdateStage.Animation));
                }
                else if (avatar.AvatarMode ==
                         UxrAvatarMode.UpdateExternally)
                {
                    OnAvatarUpdating(
                        avatar,
                        new UxrAvatarUpdateEventArgs(
                            avatar,
                            UxrUpdateStage.Animation));

                    avatar.UpdateHandPoseTransforms();

                    OnAvatarUpdated(
                        avatar,
                        new UxrAvatarUpdateEventArgs(
                            avatar,
                            UxrUpdateStage.Animation));
                }
            }

            OnStageUpdated(
                UxrUpdateStage.Animation);

            OnUpdatingStage(
                UxrUpdateStage.PostProcess);

            foreach (UxrAvatarController avatarController
                     in LocalAvatarControllers)
            {
                OnAvatarUpdating(
                    avatarController.Avatar,
                    new UxrAvatarUpdateEventArgs(
                        avatarController.Avatar,
                        UxrUpdateStage.PostProcess));

                ((IUxrAvatarControllerUpdater)
                    avatarController)
                    .UpdateAvatarPostProcess();

                avatarController.Avatar.AvatarRigInfo.UpdateInfo();

                OnAvatarUpdated(
                    avatarController.Avatar,
                    new UxrAvatarUpdateEventArgs(
                        avatarController.Avatar,
                        UxrUpdateStage.PostProcess));
            }

            OnStageUpdated(
                UxrUpdateStage.PostProcess);

            OnUpdated();
        }

        private void AddScenePrecachedInstances(
            Dictionary<EntityId, GameObject> dynamicInstances,
            Scene scene,
            UxrAvatar avatar)
        {
            for (int rootIndex = 0;
                 rootIndex < scene.rootCount;
                 ++rootIndex)
            {
                MonoBehaviour[] behaviours =
                    scene.GetRootGameObjects()[rootIndex]
                        .GetComponentsInChildren<MonoBehaviour>(true);

                for (int behaviourIndex = 0;
                     behaviourIndex < behaviours.Length;
                     ++behaviourIndex)
                {
                    if (behaviours[behaviourIndex]
                        is IUxrPrecacheable)
                    {
                        IUxrPrecacheable precacheable =
                            behaviours[behaviourIndex]
                            as IUxrPrecacheable;

                        foreach (GameObject precachedInstance
                                 in precacheable.PrecachedInstances)
                        {
                            if (precachedInstance != null &&
                                dynamicInstances.ContainsKey(
                                    precachedInstance.GetEntityId()) == false)
                            {
                                GameObject dynamicInstance =
                                    Instantiate(
                                        precachedInstance,
                                        avatar.CameraTransform.position +
                                        avatar.CameraTransform.forward *
                                        5.0f,
                                        avatar.CameraTransform.rotation,
                                        Instance.transform);

                                dynamicInstances.Add(
                                    precachedInstance.GetEntityId(),
                                    dynamicInstance);

                                AudioSource[] audioSources =
                                    dynamicInstance
                                        .GetComponentsInChildren<AudioSource>(
                                            true);

                                for (int audio = 0;
                                     audio < audioSources.Length;
                                     ++audio)
                                {
                                    audioSources[audio].enabled = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DestroyPrecachedInstances()
        {
            if (_dynamicInstances != null)
            {
                foreach (
                    KeyValuePair<EntityId, GameObject>
                    dynamicInstancePair
                    in _dynamicInstances)
                {
                    if (dynamicInstancePair.Value != null)
                    {
                        Destroy(dynamicInstancePair.Value);
                    }
                }

                _dynamicInstances.Clear();
            }
        }

        private void TryPrecaching()
        {
            if (_precacheCoroutine != null)
            {
                StopCoroutine(_precacheCoroutine);
            }

            if (UsePrecaching)
            {
                _precacheCoroutine =
                    StartCoroutine(
                        PrecacheCoroutine(
                            OnPrecachingStarting,
                            OnPrecachingFinished));
            }
        }

        private void SetupCanvases()
        {
            if (UxrPointerInputModule.Instance)
            {
                foreach (
                    Canvas canvas
                    in ComponentExt
                        .GetAllComponentsInOpenScenes<Canvas>(true))
                {
                    if (canvas.renderMode ==
                            RenderMode.WorldSpace &&
                        canvas.GetComponent<UxrIgnoreCanvas>() == null)
                    {
                        if (!canvas.TryGetComponent<UxrCanvas>(
                                out var canvasXR))
                        {
                            if (UxrPointerInputModule.Instance
                                .AutoEnableOnWorldCanvases)
                            {
                                canvasXR =
                                    canvas.gameObject
                                        .AddComponent<UxrCanvas>();

                                canvasXR.SetupCanvas(
                                    UxrPointerInputModule.Instance);
                            }
                        }

                        if (canvasXR &&
                            UxrPointerInputModule.Instance
                                .AutoAssignEventCamera &&
                            UxrAvatar.LocalAvatar)
                        {
                            canvas.worldCamera =
                                UxrAvatar.LocalAvatarCamera;
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Types & Data

        private IEnumerable<UxrAvatarController>
            LocalAvatarControllers
        {
            get
            {
                foreach (
                    UxrAvatar avatar
                    in UxrAvatar.EnabledComponents)
                {
                    if (avatar.AvatarMode ==
                            UxrAvatarMode.Local &&
                        avatar.AvatarController != null &&
                        avatar.AvatarController.enabled)
                    {
                        yield return avatar.AvatarController;
                    }
                }
            }
        }

        private Coroutine _precacheCoroutine;

        // UNITY 6 FIX:
        // EntityId replaces the old int instance ID.
        private Dictionary<EntityId, GameObject> _dynamicInstances;

        private Coroutine _teleportCoroutine;

        #endregion
    }
}