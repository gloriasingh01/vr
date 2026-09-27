using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShootingGameManager : MonoBehaviour
{
    private const int StartingLives = 3;
    private const int MonsterCount = 8;
    private const int StartDelaySeconds = 10;
    private const float MinimumSpawnDistance = 22f;

    [SerializeField] private float spawnRadius = 16f;
    [SerializeField] private float monsterSpeed = 2.5f;
    [SerializeField] private float spawnInterval = 2f;

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
    private readonly List<Text> heartLabels = new List<Text>(StartingLives);
    private readonly List<StoneMonsterEnemy> waveMonsters = new List<StoneMonsterEnemy>(MonsterCount);
    private readonly List<StoneMonsterEnemy> activeMonsters = new List<StoneMonsterEnemy>();

    public bool CanShoot => roundStarted && !gameOver;
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
        GunShooter shooter = null;
        foreach (GunShooter candidate in FindObjectsOfType<GunShooter>(true))
        {
            if (candidate.transform.root == transform.root)
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
        foreach (Camera candidate in FindObjectsOfType<Camera>(true))
        {
            if (candidate.gameObject.name == "Main Camera" && candidate.transform.root == transform.root)
            {
                camera = candidate;
                break;
            }
        }
        if (camera == null)
            camera = Camera.main;

        Transform handMount = FindRightHandMount();
        if (handMount != null && shooter.transform.parent != handMount)
            shooter.transform.SetParent(handMount, false);
        else if (handMount == null && Application.isEditor && camera != null && shooter.transform.parent != camera.transform)
            shooter.transform.SetParent(camera.transform, false);
        shooter.gameObject.SetActive(true);

        Initialize(shooter.stoneMonsterPrefab, transform.root, camera, shooter.muzzlePoint);
    }

    private Transform FindRightHandMount()
    {
        Transform trackedRightHand = null;
        foreach (Transform candidate in transform.root.GetComponentsInChildren<Transform>(true))
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
        playerTarget = gunTarget != null ? gunTarget : camera != null ? camera.transform : root;

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
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.worldCamera = null;
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

        foreach (StoneMonsterEnemy monster in activeMonsters)
        {
            if (monster != null)
                monster.gameObject.SetActive(false);
        }
    }
}
