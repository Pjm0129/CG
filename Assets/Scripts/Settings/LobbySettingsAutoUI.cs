using UnityEngine;
using UnityEngine.UI;

public class LobbySettingsAutoUI : MonoBehaviour
{
    [Header("Optional References")]
    [Tooltip("비워두면 씬에서 'Setting Button'을 자동으로 찾습니다.")]
    [SerializeField] private Button settingButton;

    [Tooltip("비워두면 Canvas 아래의 'Setting Panel'을 찾거나 새로 만듭니다.")]
    [SerializeField] private GameObject settingPanel;

    [Header("UI")]
    [SerializeField] private Vector2 panelSize = new Vector2(620f, 420f);

    private Slider volumeSlider;
    private Slider sensitivitySlider;

    private Text volumeValueText;
    private Text sensitivityValueText;

    private Font font;

    private void Awake()
    {
        GameSettings.Apply();

        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Canvas canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError(
                "LobbySettingsAutoUI: Canvas를 찾을 수 없습니다.",
                this
            );

            return;
        }

        FindOrCreateSettingButton(canvas.transform);
        FindOrCreatePanel(canvas.transform);

        BuildPanelContents();

        if (settingButton != null)
        {
            settingButton.onClick.RemoveListener(OpenSettings);
            settingButton.onClick.AddListener(OpenSettings);
        }

        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }

    private void FindOrCreateSettingButton(Transform canvas)
    {
        if (settingButton != null)
            return;

        Transform existing =
            FindChildRecursive(canvas, "Setting Button");

        if (existing != null)
        {
            settingButton = existing.GetComponent<Button>();

            if (settingButton == null)
            {
                settingButton =
                    existing.gameObject.AddComponent<Button>();
            }

            Image image =
                existing.GetComponent<Image>();

            if (image == null)
            {
                image =
                    existing.gameObject.AddComponent<Image>();
            }

            settingButton.targetGraphic = image;

            return;
        }

        GameObject buttonObject =
            CreateUIObject(
                "Setting Button",
                canvas,
                typeof(Image),
                typeof(Button)
            );

        RectTransform rect =
            buttonObject.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(260f, 70f);

        rect.anchoredPosition =
            new Vector2(0f, -150f);

        Image imageComponent =
            buttonObject.GetComponent<Image>();

        imageComponent.color =
            new Color(
                0.08f,
                0.14f,
                0.22f,
                0.92f
            );

        settingButton =
            buttonObject.GetComponent<Button>();

        settingButton.targetGraphic =
            imageComponent;

        Text label =
            CreateText(
                "Label",
                buttonObject.transform,
                "SETTINGS",
                26,
                TextAnchor.MiddleCenter
            );

        Stretch(label.rectTransform);
    }

    private void FindOrCreatePanel(Transform canvas)
    {
        if (settingPanel != null)
        {
            EnsurePanelImage();
            return;
        }

        Transform existing =
            FindChildRecursive(canvas, "Setting Panel");

        if (existing != null)
        {
            settingPanel = existing.gameObject;
            EnsurePanelImage();
            return;
        }

        settingPanel =
            CreateUIObject(
                "Setting Panel",
                canvas,
                typeof(Image)
            );
    }

    private void EnsurePanelImage()
    {
        if (settingPanel == null)
            return;

        Image image =
            settingPanel.GetComponent<Image>();

        if (image == null)
        {
            settingPanel.AddComponent<Image>();
        }
    }

    private void BuildPanelContents()
    {
        if (settingPanel == null)
        {
            Debug.LogError(
                "LobbySettingsAutoUI: Setting Panel이 없습니다.",
                this
            );

            return;
        }

        RectTransform panelRect =
            settingPanel.GetComponent<RectTransform>();

        if (panelRect == null)
        {
            Debug.LogError(
                "LobbySettingsAutoUI: Setting Panel에 RectTransform이 없습니다.",
                this
            );

            return;
        }

        panelRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        panelRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        panelRect.pivot =
            new Vector2(0.5f, 0.5f);

        panelRect.sizeDelta =
            panelSize;

        panelRect.anchoredPosition =
            Vector2.zero;

        Image panelImage =
            settingPanel.GetComponent<Image>();

        if (panelImage == null)
        {
            panelImage =
                settingPanel.AddComponent<Image>();
        }

        panelImage.color =
            new Color(
                0.035f,
                0.05f,
                0.09f,
                0.96f
            );

        ClearGeneratedChildren();

        Text title =
            CreateText(
                "Title",
                settingPanel.transform,
                "SETTINGS",
                36,
                TextAnchor.MiddleCenter
            );

        Place(
            title.rectTransform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -32f),
            new Vector2(520f, 60f)
        );

        CreateSettingRow(
            "Master Volume",
            "MASTER VOLUME",
            new Vector2(0f, 70f),
            0f,
            1f,
            GameSettings.MasterVolume,
            OnVolumeChanged,
            out volumeSlider,
            out volumeValueText
        );

        CreateSettingRow(
            "Mouse Sensitivity",
            "MOUSE SENSITIVITY",
            new Vector2(0f, -45f),
            GameSettings.MinMouseSensitivity,
            GameSettings.MaxMouseSensitivity,
            GameSettings.MouseSensitivity,
            OnSensitivityChanged,
            out sensitivitySlider,
            out sensitivityValueText
        );

        Button resetButton =
            CreateButton(
                "Reset Button",
                settingPanel.transform,
                "RESET",
                new Vector2(-145f, -145f),
                new Vector2(240f, 62f)
            );

        resetButton.onClick.AddListener(
            ResetSettings
        );

        Button closeButton =
            CreateButton(
                "Close Button",
                settingPanel.transform,
                "BACK",
                new Vector2(145f, -145f),
                new Vector2(240f, 62f)
            );

        closeButton.onClick.AddListener(
            CloseSettings
        );

        RefreshValues();
    }

    private void CreateSettingRow(
        string objectName,
        string label,
        Vector2 rowPosition,
        float min,
        float max,
        float current,
        UnityEngine.Events.UnityAction<float> callback,
        out Slider slider,
        out Text valueText)
    {
        GameObject row =
            CreateUIObject(
                objectName,
                settingPanel.transform
            );

        RectTransform rowRect =
            row.GetComponent<RectTransform>();

        rowRect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rowRect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rowRect.pivot =
            new Vector2(0.5f, 0.5f);

        rowRect.sizeDelta =
            new Vector2(520f, 95f);

        rowRect.anchoredPosition =
            rowPosition;

        Text labelText =
            CreateText(
                "Label",
                row.transform,
                label,
                22,
                TextAnchor.MiddleLeft
            );

        Place(
            labelText.rectTransform,
            new Vector2(0f, 1f),
            new Vector2(0f, 1f),
            Vector2.zero,
            new Vector2(380f, 35f)
        );

        valueText =
            CreateText(
                "Value",
                row.transform,
                "",
                22,
                TextAnchor.MiddleRight
            );

        Place(
            valueText.rectTransform,
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            Vector2.zero,
            new Vector2(120f, 35f)
        );

        slider =
            CreateSlider(
                row.transform
            );

        RectTransform sliderRect =
            slider.GetComponent<RectTransform>();

        Place(
            sliderRect,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 8f),
            new Vector2(520f, 32f)
        );

        slider.minValue = min;
        slider.maxValue = max;
        slider.value = current;

        slider.onValueChanged.AddListener(
            callback
        );
    }

    private Slider CreateSlider(
        Transform parent)
    {
        GameObject sliderObject =
            CreateUIObject(
                "Slider",
                parent,
                typeof(Slider)
            );

        Slider slider =
            sliderObject.GetComponent<Slider>();

        GameObject background =
            CreateUIObject(
                "Background",
                sliderObject.transform,
                typeof(Image)
            );

        Image backgroundImage =
            background.GetComponent<Image>();

        backgroundImage.color =
            new Color(
                0.17f,
                0.2f,
                0.25f,
                1f
            );

        Stretch(
            background.GetComponent<RectTransform>()
        );

        GameObject fillArea =
            CreateUIObject(
                "Fill Area",
                sliderObject.transform
            );

        RectTransform fillAreaRect =
            fillArea.GetComponent<RectTransform>();

        Stretch(fillAreaRect);

        fillAreaRect.offsetMin =
            new Vector2(6f, 6f);

        fillAreaRect.offsetMax =
            new Vector2(-6f, -6f);

        GameObject fill =
            CreateUIObject(
                "Fill",
                fillArea.transform,
                typeof(Image)
            );

        Image fillImage =
            fill.GetComponent<Image>();

        fillImage.color =
            new Color(
                0.2f,
                0.72f,
                1f,
                1f
            );

        Stretch(
            fill.GetComponent<RectTransform>()
        );

        GameObject handleArea =
            CreateUIObject(
                "Handle Slide Area",
                sliderObject.transform
            );

        RectTransform handleAreaRect =
            handleArea.GetComponent<RectTransform>();

        Stretch(handleAreaRect);

        handleAreaRect.offsetMin =
            new Vector2(10f, 0f);

        handleAreaRect.offsetMax =
            new Vector2(-10f, 0f);

        GameObject handle =
            CreateUIObject(
                "Handle",
                handleArea.transform,
                typeof(Image)
            );

        RectTransform handleRect =
            handle.GetComponent<RectTransform>();

        handleRect.sizeDelta =
            new Vector2(24f, 36f);

        Image handleImage =
            handle.GetComponent<Image>();

        handleImage.color =
            Color.white;

        slider.fillRect =
            fill.GetComponent<RectTransform>();

        slider.handleRect =
            handleRect;

        slider.targetGraphic =
            handleImage;

        slider.direction =
            Slider.Direction.LeftToRight;

        return slider;
    }

    private Button CreateButton(
        string objectName,
        Transform parent,
        string label,
        Vector2 position,
        Vector2 size)
    {
        GameObject obj =
            CreateUIObject(
                objectName,
                parent,
                typeof(Image),
                typeof(Button)
            );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0f);

        rect.anchorMax =
            new Vector2(0.5f, 0f);

        rect.pivot =
            new Vector2(0.5f, 0f);

        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Image image =
            obj.GetComponent<Image>();

        image.color =
            new Color(
                0.10f,
                0.30f,
                0.46f,
                1f
            );

        Button button =
            obj.GetComponent<Button>();

        button.targetGraphic = image;

        Text text =
            CreateText(
                "Label",
                obj.transform,
                label,
                24,
                TextAnchor.MiddleCenter
            );

        Stretch(text.rectTransform);

        return button;
    }

    private Text CreateText(
        string objectName,
        Transform parent,
        string value,
        int fontSize,
        TextAnchor alignment)
    {
        GameObject obj =
            CreateUIObject(
                objectName,
                parent,
                typeof(Text)
            );

        Text text =
            obj.GetComponent<Text>();

        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;

        return text;
    }

    private GameObject CreateUIObject(
        string objectName,
        Transform parent,
        params System.Type[] extraComponents)
    {
        System.Type[] components =
            new System.Type[
                extraComponents.Length + 1
            ];

        components[0] =
            typeof(RectTransform);

        for (
            int i = 0;
            i < extraComponents.Length;
            i++
        )
        {
            components[i + 1] =
                extraComponents[i];
        }

        GameObject obj =
            new GameObject(
                objectName,
                components
            );

        obj.transform.SetParent(
            parent,
            false
        );

        return obj;
    }

    private Transform FindChildRecursive(
        Transform root,
        string targetName)
    {
        foreach (Transform child in root)
        {
            if (child.name == targetName)
                return child;

            Transform found =
                FindChildRecursive(
                    child,
                    targetName
                );

            if (found != null)
                return found;
        }

        return null;
    }

    private void ClearGeneratedChildren()
    {
        for (
            int i =
                settingPanel.transform.childCount - 1;
            i >= 0;
            i--
        )
        {
            Transform child =
                settingPanel.transform.GetChild(i);

            Destroy(
                child.gameObject
            );
        }
    }

    public void OpenSettings()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
        }
    }

    public void CloseSettings()
    {
        GameSettings.Save();

        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
    }

    private void ResetSettings()
    {
        GameSettings.ResetToDefaults();

        if (volumeSlider != null)
        {
            volumeSlider.value =
                GameSettings.MasterVolume;
        }

        if (sensitivitySlider != null)
        {
            sensitivitySlider.value =
                GameSettings.MouseSensitivity;
        }

        RefreshValues();
    }

    private void OnVolumeChanged(
        float value)
    {
        GameSettings.MasterVolume = value;
        RefreshValues();
    }

    private void OnSensitivityChanged(
        float value)
    {
        GameSettings.MouseSensitivity = value;
        RefreshValues();
    }

    private void RefreshValues()
    {
        if (volumeValueText != null)
        {
            volumeValueText.text =
                Mathf.RoundToInt(
                    GameSettings.MasterVolume * 100f
                ) + "%";
        }

        if (sensitivityValueText != null)
        {
            sensitivityValueText.text =
                GameSettings.MouseSensitivity
                    .ToString("0.0");
        }
    }

    private void Place(
        RectTransform rect,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void Stretch(
        RectTransform rect)
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }

    private void OnDestroy()
    {
        GameSettings.Save();
    }
}