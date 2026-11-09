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

    [Header("Exit Settings")]
    [SerializeField] private Transform exitPoint;
    [SerializeField] private float exitRadius = 2f;

    private PlayerHealth playerHealth;
    private int currentStage = 1;
    private float stageTimer;
    private int activatedDevices;
    private bool exitReady;
    private bool missionCleared;

    private HashSet<int> completedDevices = new HashSet<int>();

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
            exitPoint.gameObject.SetActive(false);

        CreateMissionUI();
        UpdateMissionUI();
    }

    private void Update()
    {
        if (playerHealth == null ||
            playerHealth.IsDead ||
            missionCleared)
            return;

        if (currentStage == 1)
        {
            stageTimer += Time.deltaTime;

            if (stageTimer >= Mathf.Max(0.1f, stage1Duration))
            {
                currentStage = 2;
                stageTimer = 0f;
            }
        }
        else if (currentStage == 3)
        {
            if (!exitReady)
            {
                stageTimer += Time.deltaTime;

                if (stageTimer >= Mathf.Max(0.1f, stage3Duration))
                {
                    exitReady = true;

                    if (exitPoint != null)
                        exitPoint.gameObject.SetActive(true);
                }
            }

            if (exitReady)
                CheckExit();
        }

        UpdateMissionUI();
    }

    public void RegisterActivatedDevice(MissionDevice device)
    {
        if (!CanActivateDevices || device == null)
            return;

        if (!completedDevices.Add(device.GetInstanceID()))
            return;

        activatedDevices++;

        if (activatedDevices >= 3)
        {
            currentStage = 3;
            stageTimer = 0f;
        }

        UpdateMissionUI();
    }

    private void CheckExit()
    {
        if (exitPoint == null)
            return;

        Vector3 playerPosition = transform.position;
        Vector3 exitPosition = exitPoint.position;

        playerPosition.y = 0f;
        exitPosition.y = 0f;

        float distance = Vector3.Distance(
            playerPosition,
            exitPosition
        );

        if (distance <= Mathf.Max(0.1f, exitRadius))
            ClearMission();
    }

    private void ClearMission()
    {
        if (missionCleared)
            return;

        GameUI gameUI = GetComponent<GameUI>();

        if (gameUI == null)
        {
            Debug.LogError("GameUI was not found on the player.");
            return;
        }

        missionCleared = true;
        gameUI.ShowMissionClear();
    }

    private void CreateMissionUI()
    {
        missionCanvas = new GameObject(
            "Mission Canvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler)
        );

        Canvas canvas = missionCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        CanvasScaler scaler =
            missionCanvas.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject(
            "Mission Panel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        panelObject.transform.SetParent(
            missionCanvas.transform,
            false
        );

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0.035f, 0.05f, 0.09f, 0.9f);
        panelImage.raycastTarget = false;

        RectTransform panelRect =
            panelObject.GetComponent<RectTransform>();

        panelRect.anchorMin = new Vector2(0.5f, 1f);
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 1f);
        panelRect.anchoredPosition = new Vector2(0f, -36f);
        panelRect.sizeDelta = new Vector2(900f, 126f);

        GameObject textObject = new GameObject(
            "Mission Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text)
        );

        textObject.transform.SetParent(panelObject.transform, false);

        missionText = textObject.GetComponent<Text>();
        missionText.font =
            Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        missionText.fontSize = 30;
        missionText.alignment = TextAnchor.MiddleCenter;
        missionText.color = Color.white;
        missionText.raycastTarget = false;

        RectTransform textRect = missionText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);
    }

    private void UpdateMissionUI()
    {
        if (currentStage == 1)
        {
            int seconds = Mathf.CeilToInt(
                Mathf.Max(0f, stage1Duration - stageTimer)
            );

            missionText.text =
                "STAGE 1 - SURVIVE\n" +
                "TIME LEFT: " + seconds + "s";
        }
        else if (currentStage == 2)
        {
            missionText.text =
                "STAGE 2 - ACTIVATE DEVICES\n" +
                "DEVICES: " + activatedDevices + " / 3";
        }
        else if (currentStage == 3)
        {
            if (!exitReady)
            {
                int seconds = Mathf.CeilToInt(
                    Mathf.Max(0f, stage3Duration - stageTimer)
                );

                missionText.text =
                    "STAGE 3 - SURVIVE\n" +
                    "EXIT OPENS IN: " + seconds + "s";
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
        if (missionCanvas != null)
            Destroy(missionCanvas);
    }
}