using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ShootingGameManager : MonoBehaviour
{
    private const int StartingLives = 3;
    private const int MonsterCount = 8;
    private const int StartDelaySeconds = 10;
    private const float MinimumSpawnDistance = 22f;

    [SerializeField] private float spawnRadius = 16f;
    [SerializeField] private float monsterSpeed = 2.5f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private GameObject winPortalPrefab;
    [SerializeField] private string winPortalSceneName = "World1";

    private GameObject monsterPrefab;
    private Transform playerRoot;
    private Transform playerTarget;
    private Camera playerCamera;
    private bool initialized;
    private bool wavePrepared;
    private bool roundStarted;
    private bool gameOver;
    private int lives = StartingLives;
    private int monstersDefeated;
    private Text statusText;
    private GameObject winPortalObject;
    private ScenePortal winPortalScene;
    private XRSimpleInteractable winPortalInteractable;
    private Button winPortalButton;
    private readonly List<Text> heartLabels = new List<Text>(StartingLives);
    private readonly List<StoneMonsterEnemy> waveMonsters = new List<StoneMonsterEnemy>(MonsterCount);
    private readonly List<StoneMonsterEnemy> activeMonsters = new List<StoneMonsterEnemy>();

    public bool CanShoot => !gameOver;
    public bool IsGameOver => gameOver;
    public Transform PlayerRoot => playerRoot;
    public Transform PlayerTarget => playerTarget;

    private void Awake()
    {
        if (!initialized)
            InitializeFromScene();
    }

    private void InitializeFromScene()
    {
        Transform rigRoot = FindXROriginRoot(transform);
        GunShooter shooter = null;
        foreach (GunShooter candidate in FindObjectsOfType<GunShooter>(true))
        {
            if (candidate.transform == rigRoot || candidate.transform.IsChildOf(rigRoot))
            {
                shooter = candidate;
                break;
            }
        }

        if (shooter == null)
        {
            Debug.LogError("[ShootingGame] No GunShooter was found under the XR Origin.", this);
            return;
        }

        Camera camera = null;
        foreach (Camera candidate in rigRoot.GetComponentsInChildren<Camera>(true))
        {
            if (candidate.gameObject.name == "Main Camera")
            {
                camera = candidate;
                break;
            }
        }
        if (camera == null)
            camera = Camera.main;

        Transform handMount = FindRightHandMount(rigRoot);
        if (handMount != null && shooter.transform.parent != handMount)
            shooter.transform.SetParent(handMount, false);
        else if (handMount == null && Application.isEditor && camera != null && shooter.transform.parent != camera.transform)
            shooter.transform.SetParent(camera.transform, false);
        shooter.gameObject.SetActive(true);

        Initialize(shooter.stoneMonsterPrefab, rigRoot, camera, shooter.muzzlePoint);
    }

    private static Transform FindXROriginRoot(Transform hint)
    {
        for (Transform current = hint; current != null; current = current.parent)
        {
            if (current.GetComponent("XROrigin") != null || current.name.StartsWith("XR Origin", System.StringComparison.Ordinal))
                return current;
        }

        return hint;
    }

    private Transform FindRightHandMount(Transform rigRoot)
    {
        Transform trackedRightHand = null;
        foreach (Transform candidate in rigRoot.GetComponentsInChildren<Transform>(true))
        {
            if (candidate.name == "Right Hand")
            {
                trackedRightHand = candidate;
                if (candidate.gameObject.activeInHierarchy || !Application.isEditor)
                    return candidate;
            }
        }

        return trackedRightHand != null && (!Application.isEditor || trackedRightHand.gameObject.activeInHierarchy)
            ? trackedRightHand
            : null;
    }

    public void Initialize(GameObject prefab, Transform root, Camera camera, Transform gunTarget)
    {
        if (initialized)
            return;

        initialized = true;
        monsterPrefab = prefab;
        playerRoot = root;
        playerCamera = camera;
        // Monsters attack the player body/head, not the moving muzzle. The muzzle
        // remains the ray origin for aiming and firing only.
        playerTarget = camera != null ? camera.transform : root;

        // Hide and prepare all placed wave monsters before the first rendered frame.
        if (monsterPrefab != null && playerRoot != null)
            PrepareWaveMonsters();

        Debug.Log("[ShootingGame] Manager initialized. Prepared " + waveMonsters.Count + " monsters; target is " +
                  (playerTarget != null ? playerTarget.name : "missing") + ".", this);
    }

    private void Start()
    {
        if (!initialized)
            InitializeFromScene();

        Time.timeScale = 1f;
        BuildHud();

        if (playerRoot == null || monsterPrefab == null)
        {
            SetStatus("Shooting game setup is incomplete");
            return;
        }

        ConfigurePlayerHitbox();
        if (!wavePrepared)
            PrepareWaveMonsters();

        StartCoroutine(BeginRound());
        PrepareWinPortal();
    }

    private void PrepareWaveMonsters()
    {
        if (wavePrepared)
            return;

        wavePrepared = true;
        Scene activeScene = SceneManager.GetActiveScene();

        int foundPlacedMonsters = 0;
        foreach (GameObject rootObject in activeScene.GetRootGameObjects())
        {
            if (!IsPlacedMonster(rootObject.name))
                continue;

            foundPlacedMonsters++;
            if (waveMonsters.Count < MonsterCount)
                AddMonsterToWave(rootObject, waveMonsters.Count);
            else
                rootObject.SetActive(false);
        }

        // Fill missing slots if fewer than eight placed monsters were saved in the scene.
        for (int i = waveMonsters.Count; i < MonsterCount; ++i)
        {
            float angle = i * (360f / MonsterCount);
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 position = playerTarget.position + direction * spawnRadius;
            position.y += Random.Range(-1.5f, 2.5f);
            Quaternion facingPlayer = Quaternion.LookRotation(playerTarget.position - position, Vector3.up);
            GameObject monsterObject = Instantiate(monsterPrefab, position, facingPlayer);
            AddMonsterToWave(monsterObject, i);
        }

        Debug.Log("[ShootingGame] Found " + foundPlacedMonsters + " placed monsters; scheduled " +
                  waveMonsters.Count + " total. First launch is in 10s; later launches are 2s apart.", this);
    }

    private static bool IsPlacedMonster(string objectName)
    {
        return objectName == "StoneMonster" ||
               objectName.StartsWith("StoneMonster (", System.StringComparison.Ordinal);
    }

    private void AddMonsterToWave(GameObject monsterObject, int index)
    {
        Vector3 offset = monsterObject.transform.position - playerTarget.position;
        Vector3 direction = new Vector3(offset.x, 0f, offset.z);
        if (direction.sqrMagnitude < 0.01f)
            direction = Quaternion.Euler(0f, index * (360f / MonsterCount), 0f) * Vector3.forward;

        if (offset.magnitude < MinimumSpawnDistance)
        {
            float heightOffset = Mathf.Clamp(offset.y, -2f, 2f);
            monsterObject.transform.position = playerTarget.position + direction.normalized * MinimumSpawnDistance + Vector3.up * heightOffset;
        }

        StoneMonsterEnemy monster = monsterObject.GetComponent<StoneMonsterEnemy>();
        if (monster == null)
            monster = monsterObject.AddComponent<StoneMonsterEnemy>();

        monsterObject.name = "StoneMonsterWave_" + (index + 1);
        monster.Initialize(this, monsterSpeed);
        monsterObject.SetActive(false);
        waveMonsters.Add(monster);
    }

    private IEnumerator BeginRound()
    {
        for (int seconds = StartDelaySeconds; seconds > 0; --seconds)
        {
            SetStatus("Monsters arrive in " + seconds);
            yield return new WaitForSeconds(1f);
        }

        roundStarted = true;
        UpdateProgress();

        for (int i = 0; i < MonsterCount && !gameOver; ++i)
        {
            StoneMonsterEnemy monster = waveMonsters[i];
            monster.gameObject.SetActive(true);
            monster.Launch();
            activeMonsters.Add(monster);
            Debug.Log("[ShootingGame] Monster " + (i + 1) + " launched at " + Time.time.ToString("F1") + "s.", monster);

            if (i == MonsterCount - 1)
                continue;

            int waitSeconds = Mathf.CeilToInt(spawnInterval);
            for (int remaining = waitSeconds; remaining > 0 && !gameOver; --remaining)
            {
                SetStatus("Monster " + (i + 2) + " arrives in " + remaining + "s  |  Defeated: " +
                          monstersDefeated + " / " + MonsterCount);
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("Shooting HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = playerCamera != null ? RenderMode.ScreenSpaceCamera : RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = playerCamera;
        canvas.planeDistance = 1.2f;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject statusObject = new GameObject("Round Status", typeof(RectTransform), typeof(Text));
        statusObject.transform.SetParent(canvas.transform, false);
        RectTransform statusRect = statusObject.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 1f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.pivot = new Vector2(0.5f, 1f);
        statusRect.anchoredPosition = new Vector2(0f, -28f);
        statusRect.sizeDelta = new Vector2(900f, 90f);
        statusText = statusObject.GetComponent<Text>();
        statusText.font = font;
        statusText.fontSize = 42;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = Color.white;
        statusText.horizontalOverflow = HorizontalWrapMode.Overflow;
        statusText.verticalOverflow = VerticalWrapMode.Overflow;
        statusText.raycastTarget = false;
        AddTextShadow(statusText);

        GameObject heartsObject = new GameObject("Lives", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        heartsObject.transform.SetParent(canvas.transform, false);
        RectTransform heartsRect = heartsObject.GetComponent<RectTransform>();
        heartsRect.anchorMin = new Vector2(0.5f, 0f);
        heartsRect.anchorMax = new Vector2(0.5f, 0f);
        heartsRect.pivot = new Vector2(0.5f, 0f);
        heartsRect.anchoredPosition = new Vector2(0f, 26f);
        heartsRect.sizeDelta = new Vector2(360f, 96f);

        HorizontalLayoutGroup layout = heartsObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 12f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < StartingLives; ++i)
        {
            GameObject heartObject = new GameObject("Heart " + (i + 1), typeof(RectTransform), typeof(Text), typeof(LayoutElement));
            heartObject.transform.SetParent(heartsObject.transform, false);
            Text heart = heartObject.GetComponent<Text>();
            heart.font = font;
            heart.fontSize = 78;
            heart.alignment = TextAnchor.MiddleCenter;
            heart.text = "♥";
            heart.color = new Color32(230, 34, 48, 255);
            heart.raycastTarget = false;
            LayoutElement element = heartObject.GetComponent<LayoutElement>();
            element.preferredWidth = 96f;
            element.preferredHeight = 90f;
            AddTextShadow(heart);
            heartLabels.Add(heart);
        }

        UpdateHearts();
    }

    private static void AddTextShadow(Text text)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(2f, -2f);
    }

    private void ConfigurePlayerHitbox()
    {
        Transform hitboxTransform = playerRoot.Find("Player Damage Trigger");
        if (hitboxTransform == null)
        {
            GameObject hitboxObject = new GameObject("Player Damage Trigger");
            hitboxObject.transform.SetParent(playerRoot, false);
            hitboxObject.transform.localPosition = new Vector3(0f, 1f, 0f);
            hitboxObject.layer = 0; // Default, so the damage trigger remains collidable even if the XR rig is Ignore Raycast.
            hitboxTransform = hitboxObject.transform;
        }

        BoxCollider hitbox = hitboxTransform.GetComponent<BoxCollider>();
        if (hitbox == null)
            hitbox = hitboxTransform.gameObject.AddComponent<BoxCollider>();
        hitbox.isTrigger = true;
        hitbox.enabled = true;
        hitbox.center = Vector3.zero;
        hitbox.size = new Vector3(0.9f, 1.8f, 0.9f);
    }

    public void RegisterPlayerContact(StoneMonsterEnemy monster)
    {
        if (!roundStarted || gameOver || monster == null)
            return;

        lives = Mathf.Max(0, lives - 1);
        Debug.Log("[ShootingGame] Player hit by " + monster.name + "; lives remaining: " + lives + ".", monster);
        UpdateHearts();
        if (lives == 0)
        {
            EndGame(false);
            return;
        }

        SetStatus("Hit! Defeated: " + monstersDefeated + " / " + MonsterCount);
    }

    public void RegisterMonsterDefeated(StoneMonsterEnemy monster)
    {
        activeMonsters.Remove(monster);
        if (gameOver)
            return;

        monstersDefeated++;
        Debug.Log("[ShootingGame] " + monster.name + " defeated in one hit. Total: " + monstersDefeated + "/" + MonsterCount + ".", monster);
        UpdateProgress();
        if (monstersDefeated >= MonsterCount)
            EndGame(true);
    }

    private void UpdateProgress()
    {
        SetStatus("Monsters defeated: " + monstersDefeated + " / " + MonsterCount);
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < heartLabels.Count; ++i)
            heartLabels[i].color = i < lives ? new Color32(230, 34, 48, 255) : Color.black;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    private void EndGame(bool won)
    {
        if (gameOver)
            return;

        gameOver = true;
        roundStarted = false;
        StopAllCoroutines();
        SetStatus(won ? "YOU WIN — ALL MONSTERS DEFEATED" : "GAME OVER");

        if (won)
            ShowWinPortal();

        foreach (StoneMonsterEnemy monster in activeMonsters)
        {
            if (monster != null)
                monster.gameObject.SetActive(false);
        }
    }

    private void PrepareWinPortal()
    {
        if (winPortalObject != null)
            return;

        // Reuse a saved portal if present; otherwise spawn the matching anchor
        // prefab used by Altar_Ruins_FREE/DemoScene.
        foreach (GameObject sceneRoot in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform candidate in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != "Portal Teleport Anchor")
                    continue;

                winPortalObject = candidate.gameObject;
                winPortalScene = candidate.GetComponentInChildren<ScenePortal>(true);
                break;
            }

            if (winPortalObject != null)
                break;
        }

        if (winPortalObject == null)
        {
            if (winPortalPrefab == null)
            {
                Debug.LogError("[ShootingGame] Teleport Anchor prefab is not assigned; the win portal cannot be shown.", this);
                return;
            }

            try
            {
                winPortalObject = Instantiate(winPortalPrefab);
            }
            catch (System.Exception exception)
            {
                Debug.LogError("[ShootingGame] Could not instantiate the win portal. The shooting round will continue. " + exception, this);
                return;
            }
            winPortalObject.name = "Portal Teleport Anchor";
        }

        if (winPortalScene == null)
            winPortalScene = winPortalObject.GetComponentInChildren<ScenePortal>(true);
        if (winPortalScene == null)
            winPortalScene = winPortalObject.AddComponent<ScenePortal>();

        winPortalScene.SetTargetScene(winPortalSceneName);

        // The winner sees the new button panel instead of the prefab's default
        // anchor artwork. Its teleport interaction and collider remain intact.
        foreach (Renderer portalRenderer in winPortalObject.GetComponentsInChildren<Renderer>(true))
            portalRenderer.enabled = false;
        foreach (Canvas portalCanvas in winPortalObject.GetComponentsInChildren<Canvas>(true))
            portalCanvas.enabled = false;

        BuildWinPortalButton();
        winPortalObject.SetActive(false);
    }

    private void BuildWinPortalButton()
    {
        GameObject canvasObject = new GameObject(
            "Enter Level 3 Canvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(winPortalObject.transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 200;
        canvas.worldCamera = playerCamera;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 10f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800f, 220f);
        canvasRect.localScale = Vector3.one * 0.00125f;
        canvasRect.localPosition = Vector3.forward * 0.55f;

        GameObject buttonObject = new GameObject(
            "Enter Level 3 Button",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button),
            typeof(BoxCollider), typeof(XRSimpleInteractable));
        buttonObject.transform.SetParent(canvasObject.transform, false);

        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = Vector2.zero;
        buttonRect.anchorMax = Vector2.one;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color32(12, 35, 59, 245);
        Outline border = buttonObject.AddComponent<Outline>();
        border.effectColor = new Color32(69, 218, 255, 255);
        border.effectDistance = new Vector2(5f, -5f);

        winPortalButton = buttonObject.GetComponent<Button>();
        winPortalButton.targetGraphic = background;
        winPortalButton.onClick.AddListener(LoadNextLevelFromPortal);

        BoxCollider hitbox = buttonObject.GetComponent<BoxCollider>();
        hitbox.size = new Vector3(800f, 220f, 32f);
        hitbox.isTrigger = false;

        winPortalInteractable = buttonObject.GetComponent<XRSimpleInteractable>();
        winPortalInteractable.interactionLayers = new InteractionLayerMask { value = -1 };
        winPortalInteractable.selectEntered.AddListener(HandleWinPortalSelected);

        GameObject labelObject = new GameObject("Enter Level 3 Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(24f, 12f);
        labelRect.offsetMax = new Vector2(-24f, -12f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = "Enter Level 3";
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = 82f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.enableAutoSizing = true;
        label.fontSizeMin = 48f;
        label.fontSizeMax = 82f;
        label.raycastTarget = false;
    }

    private void ShowWinPortal()
    {
        if (winPortalObject == null)
            PrepareWinPortal();

        if (winPortalObject == null)
            return;

        Transform view = playerCamera != null ? playerCamera.transform : Camera.main != null ? Camera.main.transform : null;
        if (view == null)
        {
            Debug.LogError("[ShootingGame] Cannot place the win portal because the player camera was not found.", this);
            return;
        }

        Vector3 forward = view.forward.normalized;
        winPortalObject.transform.SetPositionAndRotation(
            view.position + forward * 1.6f,
            Quaternion.LookRotation(-forward, Vector3.up));
        winPortalObject.SetActive(true);
        Debug.Log("[ShootingGame] All eight monsters defeated. Enter Level 3 portal is now visible in front of the player.", winPortalObject);
    }

    private void HandleWinPortalSelected(SelectEnterEventArgs args)
    {
        LoadNextLevelFromPortal();
    }

    private void LoadNextLevelFromPortal()
    {
        if (winPortalScene != null)
            winPortalScene.TriggerSceneLoad();
    }
}
