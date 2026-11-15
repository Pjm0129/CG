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
    [Tooltip("DeviceSpawnPoints 오브젝트를 연결하세요.")]
    [SerializeField] private Transform deviceRoot;

    [Tooltip("점령해야 하는 Device 개수")]
    [SerializeField] private int requiredDeviceCount = 3;

    [Tooltip("Device 완료 후 다음 Device가 나타나는 시간")]
    [SerializeField] private float nextDeviceDelay = 1f;

    [Header("Exit Settings")]
    [SerializeField] private Transform exitPoint;

    [SerializeField] private float exitRadius = 2f;

    private PlayerHealth playerHealth;

    private int currentStage = 1;
    private float stageTimer;

    private int activatedDevices;

    private bool exitReady;
    private bool missionCleared;

    private readonly List<MissionDevice> devices =
        new List<MissionDevice>();

    private readonly HashSet<int> completedDevices =
        new HashSet<int>();

    private MissionDevice currentDevice;

    private Coroutine nextDeviceCoroutine;

    private GameObject missionCanvas;
    private Text missionText;


    public Transform PlayerTransform => transform;


    public bool CanActivateDevices =>
        currentStage == 2 &&
        playerHealth != null &&
        !playerHealth.IsDead &&
        !missionCleared;


    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();

        if (exitPoint != null)
        {
            exitPoint.gameObject.SetActive(false);
        }

        FindMissionDevices();

        HideAllDevices();

        CreateMissionUI();

        UpdateMissionUI();
    }


    private void Update()
    {
        if (playerHealth == null ||
            playerHealth.IsDead ||
            missionCleared)
        {
            return;
        }


        // ----------------------------
        // STAGE 1
        // ----------------------------

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


        // ----------------------------
        // STAGE 3
        // ----------------------------

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

                    if (exitPoint != null)
                    {
                        exitPoint.gameObject.SetActive(true);
                    }
                }
            }


            if (exitReady)
            {
                CheckExit();
            }
        }


        UpdateMissionUI();
    }


    // =====================================================
    // Device 초기 설정
    // =====================================================

    private void FindMissionDevices()
    {
        devices.Clear();


        if (deviceRoot == null)
        {
            Debug.LogError(
                "[MissionManager] Device Root가 설정되지 않았습니다.\n" +
                "Inspector에서 DeviceSpawnPoints를 연결하세요."
            );

            return;
        }


        MissionDevice[] foundDevices =
            deviceRoot.GetComponentsInChildren<MissionDevice>(true);


        foreach (MissionDevice device in foundDevices)
        {
            if (device == null)
            {
                continue;
            }

            devices.Add(device);

            // MissionManager 자동 연결
            device.Initialize(this);
        }


        Debug.Log(
            $"[MissionManager] Device {devices.Count}개 발견"
        );


        if (devices.Count < requiredDeviceCount)
        {
            Debug.LogWarning(
                "[MissionManager] 필요한 Device 수보다 " +
                "배치된 Device 수가 적습니다."
            );
        }
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


    // =====================================================
    // STAGE 2 시작
    // =====================================================

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
    }


    // =====================================================
    // 랜덤 Device 선택
    // =====================================================

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


            // 이미 완료한 Device는 제외
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


    // =====================================================
    // Device 점령 완료
    // =====================================================

    public void RegisterActivatedDevice(
        MissionDevice device)
    {
        if (!CanActivateDevices ||
            device == null)
        {
            return;
        }


        // 현재 활성 Device가 아닌 경우 무시
        if (device != currentDevice)
        {
            return;
        }


        int instanceID =
            device.GetInstanceID();


        // 이미 완료한 Device라면 무시
        if (!completedDevices.Add(instanceID))
        {
            return;
        }


        activatedDevices++;


        Debug.Log(
            $"[MissionManager] Device 완료 " +
            $"{activatedDevices} / {requiredDeviceCount}"
        );


        // 완료된 Device 숨기기
        device.gameObject.SetActive(false);

        currentDevice = null;


        // 목표 개수 완료
        if (activatedDevices >= requiredDeviceCount)
        {
            StartStage3();

            return;
        }


        // 다음 랜덤 Device
        if (nextDeviceCoroutine != null)
        {
            StopCoroutine(nextDeviceCoroutine);
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


    // =====================================================
    // STAGE 3
    // =====================================================

    private void StartStage3()
    {
        currentStage = 3;

        stageTimer = 0f;

        currentDevice = null;


        Debug.Log(
            "[MissionManager] STAGE 3 시작"
        );


        UpdateMissionUI();
    }


    // =====================================================
    // EXIT
    // =====================================================

    private void CheckExit()
    {
        if (exitPoint == null)
        {
            return;
        }


        Vector3 playerPosition =
            transform.position;

        Vector3 exitPosition =
            exitPoint.position;


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
                "GameUI was not found on the player."
            );

            return;
        }


        missionCleared = true;

        gameUI.ShowMissionClear();
    }


    // =====================================================
    // Mission UI
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

        canvas.sortingOrder = 90;


        CanvasScaler scaler =
            missionCanvas.GetComponent<CanvasScaler>();


        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920, 1080);

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

        panelImage.raycastTarget = false;


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


        missionText.fontSize = 30;

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
        {
            return;
        }


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


        else if (currentStage == 2)
        {
            missionText.text =
                "STAGE 2 - ACTIVATE DEVICES\n" +
                "DEVICES: " +
                activatedDevices +
                " / " +
                requiredDeviceCount;
        }


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
                    "REACH THE GREEN EXIT";
            }
        }
    }


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