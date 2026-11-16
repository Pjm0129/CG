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

    [Header("Difficulty Managers")]
    [SerializeField]
    private TurretFireManager turretFireManager;

    [SerializeField]
    private HazardZoneManager hazardZoneManager;

    [Header("Timeout Penalty")]
    [SerializeField]
    private int timeoutDamage = 1;

    [SerializeField]
    private float penaltyMessageDuration = 1f;

    [Header("Level Transition")]
    [SerializeField]
    private float levelTransitionDuration = 1.5f;

    [Header("Exit Settings")]
    [SerializeField]
    private Transform exitPoint;

    [SerializeField]
    private float exitRadius = 2f;

    [Header("Device Settings")]
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

    public Transform PlayerTransform =>
        transform;

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

        FindDifficultyManagers();

        SetupExitPortals();

        FindDevices();

        CreateMissionUI();

        StartLevel(
            0
        );
    }

    // =====================================================
    // FIND MANAGERS
    // =====================================================

    private void FindDifficultyManagers()
    {
        if (turretFireManager == null)
        {
            turretFireManager =
                FindFirstObjectByType
                <TurretFireManager>();
        }

        if (hazardZoneManager == null)
        {
            hazardZoneManager =
                FindFirstObjectByType
                <HazardZoneManager>();
        }
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
                "[MissionManager] " +
                "Level Settings가 없습니다."
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

        ApplyDifficulty();

        UpdateMissionUI();

        Debug.Log(
            $"[MissionManager] LEVEL {CurrentLevel} 시작 / " +
            $"Devices {GetRequiredDeviceCount()} / " +
            $"Time {GetCurrentTimeLimit()}초"
        );
    }

    // =====================================================
    // DIFFICULTY
    // =====================================================

    private void ApplyDifficulty()
    {
        if (turretFireManager != null)
        {
            turretFireManager
                .ApplyLevelDifficulty(
                    CurrentLevel
                );
        }

        if (hazardZoneManager != null)
        {
            hazardZoneManager
                .ApplyLevelDifficulty(
                    CurrentLevel
                );
        }
    }

    // =====================================================
    // DEVICES
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
            $"[MissionManager] " +
            $"Device {devices.Count}개 발견"
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

            devices[i]
                .SetMissionManager(
                    this
                );
        }
    }

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

        ShuffleDevices(
            available
        );

        int requiredCount =
            Mathf.Min(
                GetRequiredDeviceCount(),
                available.Count
            );

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

        yield return new WaitForSeconds(
            penaltyMessageDuration
        );

        penaltyActive =
            false;

        UpdateMissionUI();
    }

    // =====================================================
    // EXIT
    // =====================================================

    private void SetupExitPortals()
    {
        exitPortals.Clear();

        activeExitPortal =
            null;

        if (exitPoint == null)
            return;

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
    }

    private void OpenRandomExit()
    {
        exitReady =
            true;

        DisableAllDevices();

        for (
            int i = 0;
            i < exitPortals.Count;
            i++
        )
        {
            if (exitPortals[i] != null)
            {
                exitPortals[i]
                    .gameObject
                    .SetActive(false);
            }
        }

        if (
            exitPortals.Count > 0
        )
        {
            int index =
                Random.Range(
                    0,
                    exitPortals.Count
                );

            activeExitPortal =
                exitPortals[index];

            activeExitPortal
                .gameObject
                .SetActive(true);
        }
        else if (exitPoint != null)
        {
            activeExitPortal =
                exitPoint;

            exitPoint.gameObject.SetActive(
                true
            );
        }

        UpdateMissionUI();
    }

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

    private void CheckExit()
    {
        if (activeExitPortal == null)
            return;

        Vector3 playerPosition =
            transform.position;

        Vector3 exitPosition =
            activeExitPortal.position;

        playerPosition.y = 0f;
        exitPosition.y = 0f;

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
    // LEVEL DATA
    // =====================================================

    private int GetRequiredDeviceCount()
    {
        return Mathf.Max(
            1,
            levels[currentLevelIndex]
                .requiredDevices
        );
    }

    private float GetCurrentTimeLimit()
    {
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
    // SHUFFLE
    // =====================================================

    private void ShuffleDevices(
        List<MissionDevice> list)
    {
        for (
            int i =
                list.Count - 1;
            i > 0;
            i--
        )
        {
            int index =
                Random.Range(
                    0,
                    i + 1
                );

            MissionDevice temp =
                list[i];

            list[i] =
                list[index];

            list[index] =
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