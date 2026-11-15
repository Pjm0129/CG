using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerHealth))]
public class MissionManager : MonoBehaviour
{
    [Header("Stage Settings")]
    [SerializeField] private float stage1Duration = 20f;
    [SerializeField] private float stage3Duration = 30f;

    [Header("Device Mission Settings")]
    [Tooltip("DeviceSpawnPoints 부모 오브젝트")]
    [SerializeField] private Transform deviceRoot;

    [Tooltip("점령해야 하는 Device 개수")]
    [SerializeField] private int requiredDeviceCount = 3;

    [Tooltip("Device 완료 후 다음 Device가 나타날 때까지 시간")]
    [SerializeField] private float nextDeviceDelay = 1f;

    [Header("Exit Settings")]
    [Tooltip("탈출 포탈 후보 4개를 등록하세요.")]
    [SerializeField] private Transform[] exitPoints;

    [Tooltip("플레이어가 포탈에 접근했을 때 탈출로 인정할 거리")]
    [SerializeField] private float exitRadius = 2f;


    // =========================
    // Player / Stage
    // =========================

    private PlayerHealth playerHealth;

    private int currentStage = 1;

    private float stageTimer;

    private bool exitReady;

    private bool missionCleared;


    // =========================
    // Device
    // =========================

    private int activatedDevices;

    private readonly List<MissionDevice> devices =
        new List<MissionDevice>();

    private readonly HashSet<int> completedDevices =
        new HashSet<int>();

    private MissionDevice currentDevice;

    private Coroutine nextDeviceCoroutine;


    // =========================
    // Exit
    // =========================

    private Transform activeExitPoint;


    // =========================
    // UI
    // =========================

    private GameObject missionCanvas;

    private Text missionText;


    // =========================
    // Properties
    // =========================

    public Transform PlayerTransform => transform;


    public bool CanActivateDevices =>
        currentStage == 2 &&
        playerHealth != null &&
        !playerHealth.IsDead &&
        !missionCleared;


    // =========================
    // Start
    // =========================

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();

        // 모든 Exit 비활성화
        HideAllExitPoints();

        // Device 검색
        FindMissionDevices();

        // 모든 Device 비활성화
        HideAllDevices();

        // UI 생성
        CreateMissionUI();

        UpdateMissionUI();
    }


    // =========================
    // Update
    // =========================

    private void Update()
    {
        if (playerHealth == null ||
            playerHealth.IsDead ||
            missionCleared)
        {
            return;
        }


        // ==================================
        // STAGE 1 : 생존
        // ==================================

        if (currentStage == 1)
        {
            stageTimer += Time.deltaTime;

            if (stageTimer >= Mathf.Max(
                    0.1f,
                    stage1Duration))
            {
                StartStage2();
            }
        }


        // ==================================
        // STAGE 3 : 생존 후 Exit
        // ==================================

        else if (currentStage == 3)
        {
            if (!exitReady)
            {
                stageTimer += Time.deltaTime;

                if (stageTimer >= Mathf.Max(
                        0.1f,
                        stage3Duration))
                {
                    exitReady = true;

                    // 4개 Exit 중 랜덤 1개 활성화
                    ActivateRandomExit();
                }
            }

            if (exitReady)
            {
                CheckExit();
            }
        }


        UpdateMissionUI();
    }


    // =========================================================
    // DEVICE 초기 설정
    // =========================================================

    private void FindMissionDevices()
    {
        devices.Clear();


        if (deviceRoot == null)
        {
            Debug.LogError(
                "[MissionManager] Device Root가 없습니다. " +
                "Inspector에서 DeviceSpawnPoints를 연결하세요."
            );

            return;
        }


        MissionDevice[] foundDevices =
            deviceRoot.GetComponentsInChildren<MissionDevice>(
                true
            );


        foreach (MissionDevice device in foundDevices)
        {
            if (device == null)
            {
                continue;
            }

            devices.Add(device);

            device.Initialize(this);
        }


        Debug.Log(
            $"[MissionManager] Device {devices.Count}개 등록 완료"
        );
    }


    private void HideAllDevices()
    {
        foreach (MissionDevice device in devices)
        {
            if (device != null)
            {
                device.gameObject.SetActive(false);
            }
        }

        currentDevice = null;
    }


    // =========================================================
    // STAGE 2
    // =========================================================

    private void StartStage2()
    {
        currentStage = 2;

        stageTimer = 0f;

        activatedDevices = 0;

        completedDevices.Clear();

        ActivateRandomDevice();


        Debug.Log(
            "[MissionManager] STAGE 2 시작"
        );


        UpdateMissionUI();
    }


    // =========================================================
    // 랜덤 Device
    // =========================================================

    private void ActivateRandomDevice()
    {
        if (currentStage != 2)
        {
            return;
        }


        List<MissionDevice> candidates =
            new List<MissionDevice>();


        foreach (MissionDevice device in devices)
        {
            if (device == null)
            {
                continue;
            }


            // 이미 완료한 위치는 제외
            if (completedDevices.Contains(
                    device.GetInstanceID()))
            {
                continue;
            }


            candidates.Add(device);
        }


        if (candidates.Count == 0)
        {
            Debug.LogWarning(
                "[MissionManager] 활성화 가능한 Device가 없습니다."
            );

            return;
        }


        int randomIndex =
            Random.Range(
                0,
                candidates.Count
            );


        currentDevice =
            candidates[randomIndex];


        currentDevice.ResetDevice();

        currentDevice.gameObject.SetActive(true);


        Debug.Log(
            "[MissionManager] 랜덤 Device 활성화 : " +
            currentDevice.name
        );
    }


    // =========================================================
    // Device 점령 완료
    // =========================================================

    public void RegisterActivatedDevice(
        MissionDevice device)
    {
        if (!CanActivateDevices ||
            device == null)
        {
            return;
        }


        // 현재 선택된 Device만 인정
        if (device != currentDevice)
        {
            return;
        }


        int instanceID =
            device.GetInstanceID();


        // 중복 완료 방지
        if (!completedDevices.Add(instanceID))
        {
            return;
        }


        activatedDevices++;


        Debug.Log(
            $"[MissionManager] Device 완료 : " +
            $"{activatedDevices} / {requiredDeviceCount}"
        );


        // 완료된 Device 숨김
        device.gameObject.SetActive(false);

        currentDevice = null;


        // 필요한 Device 모두 완료
        if (activatedDevices >= requiredDeviceCount)
        {
            StartStage3();

            return;
        }


        // 다음 Device 생성
        if (nextDeviceCoroutine != null)
        {
            StopCoroutine(
                nextDeviceCoroutine
            );
        }


        nextDeviceCoroutine =
            StartCoroutine(
                SpawnNextDeviceRoutine()
            );


        UpdateMissionUI();
    }


    private IEnumerator SpawnNextDeviceRoutine()
    {
        yield return new WaitForSeconds(
            Mathf.Max(
                0f,
                nextDeviceDelay
            )
        );


        if (currentStage == 2 &&
            !missionCleared &&
            playerHealth != null &&
            !playerHealth.IsDead)
        {
            ActivateRandomDevice();
        }


        nextDeviceCoroutine = null;
    }


    // =========================================================
    // STAGE 3
    // =========================================================

    private void StartStage3()
    {
        currentStage = 3;

        stageTimer = 0f;

        exitReady = false;

        currentDevice = null;


        HideAllDevices();


        Debug.Log(
            "[MissionManager] STAGE 3 시작"
        );


        UpdateMissionUI();
    }


    // =========================================================
    // EXIT
    // =========================================================

    private void HideAllExitPoints()
    {
        activeExitPoint = null;


        if (exitPoints == null)
        {
            return;
        }


        foreach (Transform point in exitPoints)
        {
            if (point != null)
            {
                point.gameObject.SetActive(false);
            }
        }
    }


    private void ActivateRandomExit()
    {
        if (exitPoints == null ||
            exitPoints.Length == 0)
        {
            Debug.LogError(
                "[MissionManager] Exit Points가 등록되지 않았습니다."
            );

            return;
        }


        // 먼저 전부 OFF
        HideAllExitPoints();


        // null이 아닌 Exit만 후보 등록
        List<Transform> candidates =
            new List<Transform>();


        foreach (Transform point in exitPoints)
        {
            if (point != null)
            {
                candidates.Add(point);
            }
        }


        if (candidates.Count == 0)
        {
            Debug.LogError(
                "[MissionManager] 사용 가능한 Exit Point가 없습니다."
            );

            return;
        }


        int randomIndex =
            Random.Range(
                0,
                candidates.Count
            );


        activeExitPoint =
            candidates[randomIndex];


        activeExitPoint.gameObject.SetActive(true);


        Debug.Log(
            "[MissionManager] 랜덤 Exit 활성화 : " +
            activeExitPoint.name
        );
    }


    private void CheckExit()
    {
        if (activeExitPoint == null)
        {
            return;
        }


        Vector3 playerPosition =
            transform.position;


        Vector3 exitPosition =
            activeExitPoint.position;


        // 높이 무시
        playerPosition.y = 0f;

        exitPosition.y = 0f;


        float distance =
            Vector3.Distance(
                playerPosition,
                exitPosition
            );


        if (distance <= Mathf.Max(
                0.1f,
                exitRadius))
        {
            ClearMission();
        }
    }


    // =========================================================
    // Mission Clear
    // =========================================================

    private void ClearMission()
    {
        if (missionCleared)
        {
            return;
        }


        GameUI gameUI =
            GetComponent<GameUI>();


        if (gameUI == null)
        {
            Debug.LogError(
                "[MissionManager] GameUI가 Player에 없습니다."
            );

            return;
        }


        missionCleared = true;


        Debug.Log(
            "[MissionManager] MISSION CLEAR"
        );


        gameUI.ShowMissionClear();
    }


    // =========================================================
    // UI
    // =========================================================

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
            missionCanvas.GetComponent<CanvasScaler>();


        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;


        scaler.referenceResolution =
            new Vector2(
                1920,
                1080
            );


        scaler.matchWidthOrHeight =
            0.5f;


        // -------------------------
        // Panel
        // -------------------------

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
            panelObject.GetComponent<RectTransform>();


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
                126f
            );


        // -------------------------
        // Text
        // -------------------------

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


    // =========================================================
    // UI 갱신
    // =========================================================

    private void UpdateMissionUI()
    {
        if (missionText == null)
        {
            return;
        }


        // STAGE 1
        if (currentStage == 1)
        {
            int seconds =
                Mathf.CeilToInt(
                    Mathf.Max(
                        0f,
                        stage1Duration -
                        stageTimer
                    )
                );


            missionText.text =
                "STAGE 1 - SURVIVE\n" +
                "TIME LEFT: " +
                seconds +
                "s";
        }


        // STAGE 2
        else if (currentStage == 2)
        {
            missionText.text =
                "STAGE 2 - ACTIVATE DEVICES\n" +
                "DEVICES: " +
                activatedDevices +
                " / " +
                requiredDeviceCount;
        }


        // STAGE 3
        else if (currentStage == 3)
        {
            if (!exitReady)
            {
                int seconds =
                    Mathf.CeilToInt(
                        Mathf.Max(
                            0f,
                            stage3Duration -
                            stageTimer
                        )
                    );


                missionText.text =
                    "STAGE 3 - SURVIVE\n" +
                    "EXIT OPENS IN: " +
                    seconds +
                    "s";
            }
            else
            {
                missionText.text =
                    "EXIT OPEN\n" +
                    "REACH THE PORTAL";
            }
        }
    }


    // =========================================================
    // Destroy
    // =========================================================

    private void OnDestroy()
    {
        if (nextDeviceCoroutine != null)
        {
            StopCoroutine(
                nextDeviceCoroutine
            );
        }


        if (missionCanvas != null)
        {
            Destroy(
                missionCanvas
            );
        }
    }
}