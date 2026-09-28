using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroVideoManager : MonoBehaviour
{
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private string destinationScene = "DemoScene";
    private bool transitioning;

    void Start()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoPlayer == null)
        {
            Debug.LogError("IntroVideoManager requires a VideoPlayer reference.", this);
            return;
        }

        if (videoPlayer.clip == null)
        {
            Debug.LogError("IntroVideoManager: no Video Clip is assigned to the VideoPlayer.", this);
            return;
        }

        // Draw straight to the camera. This avoids depending on the RenderTexture
        // and RawImage chain, which previously failed to create its render target.
        Camera introCamera = Camera.main;
        if (introCamera == null)
        {
            Debug.LogError("IntroVideoManager: no enabled MainCamera exists in the IntroVideo scene.", this);
            return;
        }

        videoPlayer.renderMode = VideoRenderMode.CameraNearPlane;
        videoPlayer.targetCamera = introCamera;
        videoPlayer.targetCameraAlpha = 1f;
        videoPlayer.targetTexture = null;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;

        videoPlayer.loopPointReached += VideoFinished;
        videoPlayer.prepareCompleted += StartVideo;
        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.started += OnVideoStarted;
        videoPlayer.Prepare();
    }

    void StartVideo(VideoPlayer vp)
    {
        Debug.Log($"Intro video prepared ({vp.width}x{vp.height}); starting camera output.", this);
        vp.Play();
    }

    void OnVideoStarted(VideoPlayer vp)
    {
        Debug.Log("Intro video playback started.", this);
    }

    void VideoFinished(VideoPlayer vp)
    {
        if (transitioning)
            return;

        transitioning = true;
        SceneManager.LoadScene(destinationScene);
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"Intro video playback failed: {message}", this);
    }

    void OnDestroy()
    {
        if (videoPlayer == null)
            return;

        videoPlayer.loopPointReached -= VideoFinished;
        videoPlayer.prepareCompleted -= StartVideo;
        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.started -= OnVideoStarted;
    }
}
