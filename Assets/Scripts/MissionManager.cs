using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class MissionManager : MonoBehaviour
{
    [System.Serializable]
    public class LevelSettings
    {
        [Header("Level")]
        public int requiredDevices = 3;

        [Tooltip("이 레벨에서 Device를 모두 활성화해야 하는 제한시간")]
        public float timeLimit = 30f;
    }

    [Header("Level Settings")]
    [SerializeField]
    private List<LevelSettings> levels =
        new List<LevelSettings>()
        {
            new LevelSettings
            {
                requiredDevices = 3,
                timeLimit = 30f
            },

            new LevelSettings
            {
                requiredDevices = 5,
                timeLimit = 35f
            },

            new LevelSettings
            {
                requiredDevices = 7,
                timeLimit = 40f
            }
        };

    [Header("Timeout Penalty")]
    [SerializeField]
    private int timeoutDamage = 1;

    [SerializeField]
    private float penaltyMessageDuration = 1f;

    [Header("Level Transition")]
    [SerializeField]
    private float levelTransitionDuration = 1.5f;

    [Header("Exit Settings")]
    [Tooltip("여러 포털을 자식으로 가지고 있는 ExitPoint 부모 오브젝트")]
    [SerializeField]
    private Transform exitPoint;

    [SerializeField]
    private float exitRadius = 2f;

    [Header("Device Settings")]
    [Tooltip("비워두면 Scene에 있는 MissionDevice를 자동으로 찾습니다.")]
    [SerializeField]
    private List<MissionDevice> devices =
        new List<MissionDevice>();

    private PlayerHealth playerHealth;

    private int currentLevelIndex;
    private int activatedDevices;

    private float levelTimer;

    private bool missionCleared;
    private bool exitReady;
    private bool levelTransitioning;
    private bool penaltyActive;

    private readonly HashSet<int>
        completedDevices =
            new HashSet<int>();

    private readonly List<MissionDevice>
        currentLevelDevices =
            new List<MissionDevice>();

    private readonly List<Transform>
        exitPortals =
            new List<Transform>();

    private Transform activeExitPortal;

    private GameObject missionCanvas;
    private Text missionText;

    // =====================================================
    // PUBLIC
    // =====================================================

    public Transform PlayerTransform => transform;

    public bool CanActivateDevices =>
        !missionCleared &&
        !exitReady &&
        !levelTransitioning &&
        !penaltyActive &&
        playerHealth != null &&
        !playerHealth.IsDead;

    public int CurrentLevel =>
        currentLevelIndex + 1;

    public float RemainingTime =>
        GetCurrentTimeLimit() -
        levelTimer;

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        playerHealth =
            GetComponent<PlayerHealth>();

        SetupExitPortals();

        FindDevices();

        CreateMissionUI();

        StartLevel(0);
    }

    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (
            playerHealth == null ||
            playerHealth.IsDead ||
            missionCleared
        )
        {
            return;
        }

        if (exitReady)
        {
            CheckExit();

            UpdateMissionUI();

            return;
        }

        if (
            levelTransitioning ||
            penaltyActive
        )
        {
            return;
        }

        levelTimer +=
            Time.deltaTime;

        if (
            levelTimer >=
            GetCurrentTimeLimit()
        )
        {
            StartCoroutine(
                TimeoutPenaltyRoutine()
            );

            return;
        }

        UpdateMissionUI();
    }

    // =====================================================
    // EXIT PORTAL SETUP
    // =====================================================

    private void SetupExitPortals()
    {
        exitPortals.Clear();

        activeExitPortal =
            null;

        if (exitPoint == null)
        {
            Debug.LogWarning(
                "[MissionManager] ExitPoint가 연결되지 않았습니다."
            );

            return;
        }

        // 부모는 활성화 상태로 유지
        // 실제 포털 자식들만 꺼둔다.
        exitPoint.gameObject.SetActive(
            true
        );

        for (
            int i = 0;
            i < exitPoint.childCount;
            i++
        )
        {
            Transform portal =
                exitPoint.GetChild(i);

            if (portal == null)
                continue;

            exitPortals.Add(
                portal
            );

            portal.gameObject.SetActive(
                false
            );
        }

        Debug.Log(
            $"[MissionManager] Exit Portal {exitPortals.Count}개 등록"
        );
    }

    // =====================================================
    // FIND DEVICES
    // =====================================================

    private void FindDevices()
    {
        if (
            devices != null &&
            devices.Count > 0
        )
        {
            SetupDeviceReferences();

            return;
        }

        devices =
            new List<MissionDevice>(
                FindObjectsByType<MissionDevice>(
                    FindObjectsSortMode.None
                )
            );

        SetupDeviceReferences();

        Debug.Log(
            $"[MissionManager] Device {devices.Count}개 발견"
        );
    }

    private void SetupDeviceReferences()
    {
        for (
            int i = 0;
            i < devices.Count;
            i++
        )
        {
            if (devices[i] == null)
                continue;

            devices[i].SetMissionManager(
                this
            );
        }
    }

    // =====================================================
    // START LEVEL
    // =====================================================

    private void StartLevel(
        int levelIndex)
    {
        if (
            levels == null ||
            levels.Count == 0
        )
        {
            Debug.LogError(
                "[MissionManager] Level Settings가 없습니다."
            );

            return;
        }

        currentLevelIndex =
            Mathf.Clamp(
                levelIndex,
                0,
                levels.Count - 1
            );

        activatedDevices =
            0;

        levelTimer =
            0f;

        completedDevices.Clear();

        SelectDevicesForCurrentLevel();

        UpdateMissionUI();

        Debug.Log(
            $"[MissionManager] LEVEL {CurrentLevel} 시작 / " +
            $"Device {GetRequiredDeviceCount()}개 / " +
            $"{GetCurrentTimeLimit()}초"
        );
    }

    // =====================================================
    // DEVICE SELECTION
    // =====================================================

    private void SelectDevicesForCurrentLevel()
    {
        currentLevelDevices.Clear();

        List<MissionDevice> available =
            new List<MissionDevice>();

        for (
            int i = 0;
            i < devices.Count;
            i++
        )
        {
            MissionDevice device =
                devices[i];

            if (device == null)
                continue;

            device.gameObject.SetActive(
                true
            );

            device.ResetDevice();

            available.Add(
                device
            );
        }

        Shuffle(
            available
        );

        int requiredCount =
            Mathf.Min(
                GetRequiredDeviceCount(),
                available.Count
            );

        if (
            requiredCount <
            GetRequiredDeviceCount()
        )
        {
            Debug.LogWarning(
                $"[MissionManager] 필요한 Device는 " +
                $"{GetRequiredDeviceCount()}개이지만 " +
                $"Scene에는 {available.Count}개만 있습니다."
            );
        }

        for (
            int i = 0;
            i < available.Count;
            i++
        )
        {
            bool selected =
                i < requiredCount;

            available[i]
                .SetAvailableForLevel(
                    selected
                );

            if (selected)
            {
                currentLevelDevices.Add(
                    available[i]
                );
            }
        }
    }

    // =====================================================
    // DEVICE ACTIVATION
    // =====================================================

    public void RegisterActivatedDevice(
        MissionDevice device)
    {
        if (
            !CanActivateDevices ||
            device == null
        )
        {
            return;
        }

        if (
            !currentLevelDevices.Contains(
                device
            )
        )
        {
            return;
        }

        if (
            !completedDevices.Add(
                device.GetInstanceID()
            )
        )
        {
            return;
        }

        activatedDevices++;

        Debug.Log(
            $"[MissionManager] LEVEL {CurrentLevel} " +
            $"Device {activatedDevices} / " +
            $"{GetRequiredDeviceCount()}"
        );

        if (
            activatedDevices >=
            GetRequiredDeviceCount()
        )
        {
            StartCoroutine(
                CompleteLevelRoutine()
            );
        }

        UpdateMissionUI();
    }

    // =====================================================
    // LEVEL COMPLETE
    // =====================================================

    private IEnumerator CompleteLevelRoutine()
    {
        if (levelTransitioning)
            yield break;

        levelTransitioning =
            true;

        if (missionText != null)
        {
            missionText.color =
                Color.white;

            missionText.text =
                $"LEVEL {CurrentLevel} COMPLETE";
        }

        yield return new WaitForSeconds(
            levelTransitionDuration
        );

        int nextLevelIndex =
            currentLevelIndex + 1;

        if (
            nextLevelIndex <
            levels.Count
        )
        {
            levelTransitioning =
                false;

            StartLevel(
                nextLevelIndex
            );
        }
        else
        {
            levelTransitioning =
                false;

            OpenRandomExit();
        }
    }

    // =====================================================
    // TIMEOUT
    // =====================================================

    private IEnumerator TimeoutPenaltyRoutine()
    {
        if (penaltyActive)
            yield break;

        penaltyActive =
            true;

        // 시간만 초기화
        // 이미 활성화한 Device 진행도는 유지
        levelTimer =
            0f;

        if (missionText != null)
        {
            missionText.color =
                new Color(
                    1f,
                    0.25f,
                    0.25f
                );

            missionText.text =
                "TIME OVER!\n" +
                $"HP -{timeoutDamage}";
        }

        if (
            playerHealth != null &&
            !playerHealth.IsDead
        )
        {
            playerHealth.TakeDamage(
                timeoutDamage
            );
        }

        Debug.Log(
            $"[MissionManager] LEVEL {CurrentLevel} 시간 초과 / " +
            $"HP -{timeoutDamage}"
        );

        yield return new WaitForSeconds(
            penaltyMessageDuration
        );

        penaltyActive =
            false;

        UpdateMissionUI();
    }

    // =====================================================
    // RANDOM EXIT
    // =====================================================

    private void OpenRandomExit()
    {
        exitReady =
            true;

        DisableAllDevices();

        // 혹시 기존 포털이 켜져 있다면 모두 끔
        for (
            int i = 0;
            i < exitPortals.Count;
            i++
        )
        {
            if (exitPortals[i] == null)
                continue;

            exitPortals[i]
                .gameObject
                .SetActive(false);
        }

        if (
            exitPortals.Count > 0
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    exitPortals.Count
                );

            activeExitPortal =
                exitPortals[randomIndex];

            activeExitPortal
                .gameObject
                .SetActive(true);

            Debug.Log(
                $"[MissionManager] EXIT OPEN → " +
                $"{activeExitPortal.name}"
            );
        }
        else if (
            exitPoint != null
        )
        {
            // 자식 포털이 없는 경우
            // 기존 방식으로 ExitPoint 자체 사용
            activeExitPortal =
                exitPoint;

            exitPoint.gameObject.SetActive(
                true
            );

            Debug.LogWarning(
                "[MissionManager] ExitPoint 자식 포털이 없어 " +
                "ExitPoint 자체를 탈출 위치로 사용합니다."
            );
        }
        else
        {
            Debug.LogError(
                "[MissionManager] 사용할 Exit이 없습니다."
            );
        }

        UpdateMissionUI();
    }

    // =====================================================
    // DISABLE DEVICES
    // =====================================================

    private void DisableAllDevices()
    {
        for (
            int i = 0;
            i < devices.Count;
            i++
        )
        {
            if (devices[i] == null)
                continue;

            devices[i]
                .SetAvailableForLevel(
                    false
                );
        }
    }

    // =====================================================
    // CHECK EXIT
    // =====================================================

    private void CheckExit()
    {
        if (
            activeExitPortal == null
        )
        {
            return;
        }

        Vector3 playerPosition =
            transform.position;

        Vector3 exitPosition =
            activeExitPortal.position;

        playerPosition.y =
            0f;

        exitPosition.y =
            0f;

        float distance =
            Vector3.Distance(
                playerPosition,
                exitPosition
            );

        if (
            distance <=
            Mathf.Max(
                0.1f,
                exitRadius
            )
        )
        {
            ClearMission();
        }
    }

    // =====================================================
    // CLEAR MISSION
    // =====================================================

    private void ClearMission()
    {
        if (missionCleared)
            return;

        GameUI gameUI =
            GetComponent<GameUI>();

        if (gameUI == null)
        {
            Debug.LogError(
                "GameUI was not found on the player."
            );

            return;
        }

        missionCleared =
            true;

        gameUI.ShowMissionClear();
    }

    // =====================================================
    // LEVEL INFO
    // =====================================================

    private int GetRequiredDeviceCount()
    {
        if (
            levels == null ||
            levels.Count == 0
        )
        {
            return 0;
        }

        return Mathf.Max(
            1,
            levels[currentLevelIndex]
                .requiredDevices
        );
    }

    private float GetCurrentTimeLimit()
    {
        if (
            levels == null ||
            levels.Count == 0
        )
        {
            return 30f;
        }

        return Mathf.Max(
            1f,
            levels[currentLevelIndex]
                .timeLimit
        );
    }

    // =====================================================
    // UI
    // =====================================================

    private void CreateMissionUI()
    {
        missionCanvas =
            new GameObject(
                "Mission Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler)
            );

        Canvas canvas =
            missionCanvas.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            90;

        CanvasScaler scaler =
            missionCanvas
                .GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode
                .ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(
                1920,
                1080
            );

        scaler.matchWidthOrHeight =
            0.5f;

        GameObject panelObject =
            new GameObject(
                "Mission Panel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        panelObject.transform.SetParent(
            missionCanvas.transform,
            false
        );

        Image panelImage =
            panelObject.GetComponent<Image>();

        panelImage.color =
            new Color(
                0.035f,
                0.05f,
                0.09f,
                0.9f
            );

        panelImage.raycastTarget =
            false;

        RectTransform panelRect =
            panelObject
                .GetComponent<RectTransform>();

        panelRect.anchorMin =
            new Vector2(
                0.5f,
                1f
            );

        panelRect.anchorMax =
            new Vector2(
                0.5f,
                1f
            );

        panelRect.pivot =
            new Vector2(
                0.5f,
                1f
            );

        panelRect.anchoredPosition =
            new Vector2(
                0f,
                -36f
            );

        panelRect.sizeDelta =
            new Vector2(
                900f,
                150f
            );

        GameObject textObject =
            new GameObject(
                "Mission Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

        textObject.transform.SetParent(
            panelObject.transform,
            false
        );

        missionText =
            textObject.GetComponent<Text>();

        missionText.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        missionText.fontSize =
            30;

        missionText.alignment =
            TextAnchor.MiddleCenter;

        missionText.color =
            Color.white;

        missionText.raycastTarget =
            false;

        RectTransform textRect =
            missionText.rectTransform;

        textRect.anchorMin =
            Vector2.zero;

        textRect.anchorMax =
            Vector2.one;

        textRect.offsetMin =
            new Vector2(
                20f,
                10f
            );

        textRect.offsetMax =
            new Vector2(
                -20f,
                -10f
            );
    }

    private void UpdateMissionUI()
    {
        if (missionText == null)
            return;

        if (exitReady)
        {
            missionText.color =
                Color.white;

            missionText.text =
                "EXIT OPEN\n" +
                "REACH THE PORTAL";

            return;
        }

        int seconds =
            Mathf.CeilToInt(
                Mathf.Max(
                    0f,
                    RemainingTime
                )
            );

        missionText.text =
            $"LEVEL {CurrentLevel} - ACTIVATE DEVICES\n" +
            $"DEVICES: {activatedDevices} / " +
            $"{GetRequiredDeviceCount()}    " +
            $"TIME LEFT: {seconds}s";

        if (seconds <= 5)
        {
            missionText.color =
                new Color(
                    1f,
                    0.3f,
                    0.3f
                );
        }
        else
        {
            missionText.color =
                Color.white;
        }
    }

    // =====================================================
    // DEVICE SHUFFLE
    // =====================================================

    private void Shuffle(
        List<MissionDevice> list)
    {
        for (
            int i =
                list.Count - 1;
            i > 0;
            i--
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            MissionDevice temp =
                list[i];

            list[i] =
                list[randomIndex];

            list[randomIndex] =
                temp;
        }
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    private void OnDestroy()
    {
        if (missionCanvas != null)
        {
            Destroy(
                missionCanvas
            );
        }
    }
}