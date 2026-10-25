using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using StarterAssets;

[DisallowMultipleComponent]
public class GameUI : MonoBehaviour
{
    private PlayerHealth health;
    private GameObject canvasObject;
    private GameObject gameOverPanel;
    private Text hpText;
    private Text timeText;
    private Text resultText;
    private Image hpFill;
    private Font uiFont;
    private float survivalTime;
    private bool gameOver;
    private bool changingScene;
    private bool initialized;
    private float previousTimeScale;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    public void Initialize(PlayerHealth playerHealth)
    {
        if (initialized || playerHealth == null)
            return;

        initialized = true;
        health = playerHealth;
        previousTimeScale = Time.timeScale;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        BuildUI();
        health.HealthChanged += UpdateHealth;
        health.Died += ShowGameOver;
        UpdateHealth(health.CurrentHP, health.MaxHP);
        UpdateTime();
        if (health.IsDead)
            ShowGameOver();
    }

    private void Update()
    {
        if (!initialized || gameOver)
            return;

        survivalTime += Time.deltaTime;
        UpdateTime();
    }

    private void BuildUI()
    {
        canvasObject = new GameObject("Game UI", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        RectTransform root = canvasObject.GetComponent<RectTransform>();

        Color panelColor = new Color(0.035f, 0.05f, 0.09f, 0.9f);
        RectTransform hpPanel = MakePanel("Health Panel", root, panelColor);
        Place(hpPanel, new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(36, -36), new Vector2(340, 126));
        hpText = MakeText("HP", hpPanel, "", 30, TextAnchor.MiddleLeft);
        Place(hpText.rectTransform, new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(24, -12), new Vector2(292, 48));

        RectTransform bar = MakePanel("Health Bar Background", hpPanel,
            new Color(0.17f, 0.2f, 0.25f));
        Place(bar, new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(24, 24), new Vector2(292, 22));
        RectTransform fill = MakePanel("Health Bar Fill", bar,
            new Color(0.2f, 0.87f, 0.65f));
        Stretch(fill);
        hpFill = fill.GetComponent<Image>();

        RectTransform timerPanel = MakePanel("Survival Time Panel", root, panelColor);
        Place(timerPanel, new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-36, -36), new Vector2(320, 126));
        Text label = MakeText("Time Label", timerPanel, "SURVIVAL TIME", 20,
            TextAnchor.MiddleCenter);
        Place(label.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -12), new Vector2(300, 36));
        timeText = MakeText("Time Value", timerPanel, "", 42, TextAnchor.MiddleCenter);
        Place(timeText.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 16), new Vector2(300, 56));

        RectTransform overlay = MakePanel("Game Over Overlay", root,
            new Color(0, 0, 0, 0.76f));
        Stretch(overlay);
        overlay.GetComponent<Image>().raycastTarget = true;
        gameOverPanel = overlay.gameObject;

        RectTransform card = MakePanel("Result Card", overlay, panelColor);
        Place(card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(660, 440));
        Text title = MakeText("Game Over Title", card, "GAME OVER", 64,
            TextAnchor.MiddleCenter);
        title.color = new Color(1f, 0.38f, 0.4f);
        Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -42), new Vector2(620, 90));
        resultText = MakeText("Final Survival Time", card, "", 32,
            TextAnchor.MiddleCenter);
        Place(resultText.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
            new Vector2(0, -155), new Vector2(620, 60));

        MakeButton("Restart Button", card, "RESTART", new Vector2(-150, 70),
            new Color(0.12f, 0.55f, 0.43f), RestartGame);
        MakeButton("Lobby Button", card, "LOBBY", new Vector2(150, 70),
            new Color(0.22f, 0.28f, 0.4f), ReturnToLobby);
        gameOverPanel.SetActive(false);

        if (EventSystem.current == null)
        {
            GameObject events = new GameObject("Game UI EventSystem", typeof(EventSystem));
            events.transform.SetParent(canvasObject.transform, false);
            events.AddComponent<InputSystemUIInputModule>();
        }
    }

    private void UpdateHealth(int current, int maximum)
    {
        hpText.text = "HP  " + current + " / " + maximum;
        float ratio = maximum > 0 ? (float)current / maximum : 0f;
        hpFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
        hpFill.color = ratio > 0.4f
            ? new Color(0.2f, 0.87f, 0.65f)
            : new Color(1f, 0.32f, 0.35f);
    }

    private void UpdateTime()
    {
        timeText.text = FormatTime(survivalTime);
    }

    private string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        return (totalSeconds / 60).ToString("00") + ":" + (totalSeconds % 60).ToString("00");
    }

    private void ShowGameOver()
    {
        if (gameOver)
            return;

        gameOver = true;
        resultText.text = "SURVIVED  " + FormatTime(survivalTime);
        gameOverPanel.SetActive(true);

        PlayerInput input = health.GetComponent<PlayerInput>();
        if (input != null)
            input.DeactivateInput();
        ThirdPersonController movement = health.GetComponent<ThirdPersonController>();
        if (movement != null)
            movement.enabled = false;
        PlayerDash dash = health.GetComponent<PlayerDash>();
        if (dash != null)
        {
            dash.StopAllCoroutines();
            dash.enabled = false;
        }
        StarterAssetsInputs starterInput = health.GetComponent<StarterAssetsInputs>();
        if (starterInput != null)
        {
            starterInput.cursorLocked = false;
            starterInput.cursorInputForLook = false;
            starterInput.look = Vector2.zero;
        }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void RestartGame()
    {
        if (changingScene)
            return;
        changingScene = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void ReturnToLobby()
    {
        if (changingScene)
            return;
        changingScene = true;
        Time.timeScale = 1f;
        SceneManager.LoadScene(health.LobbySceneName);
    }

    private void OnDestroy()
    {
        if (!initialized)
            return;
        if (health != null)
        {
            health.HealthChanged -= UpdateHealth;
            health.Died -= ShowGameOver;
        }
        if (gameOver)
        {
            Time.timeScale = previousTimeScale;
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
        }
        if (canvasObject != null)
            Destroy(canvasObject);
    }

    private RectTransform MakePanel(string objectName, Transform parent, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return obj.GetComponent<RectTransform>();
    }

    private Text MakeText(string objectName, Transform parent, string value, int size,
        TextAnchor alignment)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.font = uiFont;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void MakeButton(string objectName, Transform parent, string label,
        Vector2 position, Color color, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = MakePanel(objectName, parent, color);
        Place(rect, new Vector2(0.5f, 0), new Vector2(0.5f, 0), position, new Vector2(270, 70));
        Image image = rect.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(action);
        Text text = MakeText("Label", rect, label, 28, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform);
    }

    private void Place(RectTransform rect, Vector2 anchor, Vector2 pivot,
        Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
