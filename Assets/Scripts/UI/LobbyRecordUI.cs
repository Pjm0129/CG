using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class LobbyRecordUI : MonoBehaviour
{
    private const string LobbySceneName =
        "Lobby";

    private const string LastClearTimeKey =
        "DodgeRush_LastClearTime";

    private const string BestClearTimeKey =
        "DodgeRush_BestClearTime";

    private const string ClearCountKey =
        "DodgeRush_ClearCount";

    private GameObject canvasObject;

    // =====================================================
    // AUTO CREATE
    // =====================================================

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad
    )]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded -=
            OnSceneLoaded;

        SceneManager.sceneLoaded +=
            OnSceneLoaded;
    }

    private static void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if (scene.name != LobbySceneName)
            return;

        LobbyRecordUI existing =
            FindFirstObjectByType<LobbyRecordUI>();

        if (existing != null)
            return;

        GameObject obj =
            new GameObject(
                "Lobby Record UI"
            );

        obj.AddComponent<LobbyRecordUI>();
    }

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        BuildUI();
    }

    // =====================================================
    // UI
    // =====================================================

    private void BuildUI()
    {
        canvasObject =
            new GameObject(
                "Record Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler)
            );

        canvasObject.transform.SetParent(
            transform,
            false
        );

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            50;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

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
                "Game Record Panel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image)
            );

        panelObject.transform.SetParent(
            canvasObject.transform,
            false
        );

        RectTransform panel =
            panelObject.GetComponent<RectTransform>();

        panel.anchorMin =
            new Vector2(
                1f,
                0f
            );

        panel.anchorMax =
            new Vector2(
                1f,
                0f
            );

        panel.pivot =
            new Vector2(
                1f,
                0f
            );

        panel.anchoredPosition =
            new Vector2(
                -40f,
                40f
            );

        panel.sizeDelta =
            new Vector2(
                360f,
                205f
            );

        Image panelImage =
            panelObject.GetComponent<Image>();

        panelImage.color =
            new Color(
                0.035f,
                0.05f,
                0.09f,
                0.90f
            );

        panelImage.raycastTarget =
            false;

        Font font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        // TITLE
        Text title =
            CreateText(
                "Title",
                panel,
                font,
                "GAME RECORD",
                28,
                TextAnchor.MiddleLeft,
                Color.white
            );

        Place(
            title.rectTransform,
            new Vector2(
                24f,
                -18f
            ),
            new Vector2(
                310f,
                42f
            )
        );

        bool hasRecord =
            PlayerPrefs.HasKey(
                BestClearTimeKey
            );

        string bestText =
            hasRecord
                ? FormatTime(
                    PlayerPrefs.GetFloat(
                        BestClearTimeKey
                    )
                )
                : "--:--";

        string lastText =
            PlayerPrefs.HasKey(
                LastClearTimeKey
            )
                ? FormatTime(
                    PlayerPrefs.GetFloat(
                        LastClearTimeKey
                    )
                )
                : "--:--";

        int clearCount =
            PlayerPrefs.GetInt(
                ClearCountKey,
                0
            );

        // BEST
        Text bestLabel =
            CreateText(
                "Best Label",
                panel,
                font,
                "BEST CLEAR",
                18,
                TextAnchor.MiddleLeft,
                new Color(
                    0.25f,
                    0.8f,
                    1f
                )
            );

        Place(
            bestLabel.rectTransform,
            new Vector2(
                24f,
                -72f
            ),
            new Vector2(
                180f,
                32f
            )
        );

        Text bestValue =
            CreateText(
                "Best Value",
                panel,
                font,
                bestText,
                26,
                TextAnchor.MiddleRight,
                Color.white
            );

        Place(
            bestValue.rectTransform,
            new Vector2(
                205f,
                -70f
            ),
            new Vector2(
                125f,
                36f
            )
        );

        // LAST
        Text lastLabel =
            CreateText(
                "Last Label",
                panel,
                font,
                "LAST CLEAR",
                18,
                TextAnchor.MiddleLeft,
                Color.white
            );

        Place(
            lastLabel.rectTransform,
            new Vector2(
                24f,
                -116f
            ),
            new Vector2(
                180f,
                32f
            )
        );

        Text lastValue =
            CreateText(
                "Last Value",
                panel,
                font,
                lastText,
                26,
                TextAnchor.MiddleRight,
                Color.white
            );

        Place(
            lastValue.rectTransform,
            new Vector2(
                205f,
                -114f
            ),
            new Vector2(
                125f,
                36f
            )
        );

        // CLEAR COUNT
        Text clearLabel =
            CreateText(
                "Clear Label",
                panel,
                font,
                "CLEARS",
                16,
                TextAnchor.MiddleLeft,
                new Color(
                    0.65f,
                    0.7f,
                    0.8f
                )
            );

        Place(
            clearLabel.rectTransform,
            new Vector2(
                24f,
                -162f
            ),
            new Vector2(
                180f,
                30f
            )
        );

        Text clearValue =
            CreateText(
                "Clear Value",
                panel,
                font,
                clearCount.ToString(),
                20,
                TextAnchor.MiddleRight,
                new Color(
                    0.65f,
                    0.7f,
                    0.8f
                )
            );

        Place(
            clearValue.rectTransform,
            new Vector2(
                205f,
                -160f
            ),
            new Vector2(
                125f,
                32f
            )
        );
    }

    // =====================================================
    // TEXT
    // =====================================================

    private Text CreateText(
        string objectName,
        Transform parent,
        Font font,
        string value,
        int fontSize,
        TextAnchor alignment,
        Color color)
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        Text text =
            obj.GetComponent<Text>();

        text.font =
            font;

        text.text =
            value;

        text.fontSize =
            fontSize;

        text.alignment =
            alignment;

        text.color =
            color;

        text.raycastTarget =
            false;

        return text;
    }

    private void Place(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin =
            new Vector2(
                0f,
                1f
            );

        rect.anchorMax =
            new Vector2(
                0f,
                1f
            );

        rect.pivot =
            new Vector2(
                0f,
                1f
            );

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;
    }

    // =====================================================
    // FORMAT
    // =====================================================

    private static string FormatTime(
        float seconds)
    {
        int totalSeconds =
            Mathf.FloorToInt(
                Mathf.Max(
                    0f,
                    seconds
                )
            );

        int minutes =
            totalSeconds / 60;

        int remainSeconds =
            totalSeconds % 60;

        return
            $"{minutes:00}:{remainSeconds:00}";
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    private void OnDestroy()
    {
        if (canvasObject != null)
        {
            Destroy(
                canvasObject
            );
        }
    }
}