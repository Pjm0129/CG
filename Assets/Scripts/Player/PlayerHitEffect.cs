using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerHealth))]
public class PlayerHitEffect : MonoBehaviour
{
    [Header("Hit Effect")]
    // 화면에 표시할 색상과 최대 불투명도
    [SerializeField]
    private Color hitColor =
        new Color(1f, 0f, 0f, 0.3f);

    // 피격효과가 사라지는 데 걸리는 시간
    [SerializeField] private float fadeDuration = 0.3f;

    private PlayerHealth playerHealth;
    private int previousHP;

    private Image hitImage;
    private GameObject effectCanvas;
    private Coroutine fadeCoroutine;

    private void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        previousHP = playerHealth.CurrentHP;

        CreateHitScreen();

        // HP가 변경될 때 실행할 함수 연결
        playerHealth.HealthChanged += OnHealthChanged;
    }

    private void CreateHitScreen()
    {
        // 피격효과 전용 Canvas 생성
        effectCanvas = new GameObject(
            "HitEffectCanvas",
            typeof(RectTransform),
            typeof(Canvas)
        );

        Canvas canvas = effectCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        // 화면 전체를 덮는 Image 생성
        GameObject imageObject = new GameObject(
            "HitFlash",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        imageObject.transform.SetParent(effectCanvas.transform, false);

        hitImage = imageObject.GetComponent<Image>();

        // 피격효과가 UI 클릭을 막지 않도록 설정
        hitImage.raycastTarget = false;

        RectTransform rect = hitImage.rectTransform;

        // 해상도와 관계없이 화면 전체에 맞춤
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 처음에는 투명하게 표시
        hitImage.color = new Color(
            hitColor.r,
            hitColor.g,
            hitColor.b,
            0f
        );
    }

    private void OnHealthChanged(int currentHP, int maxHP)
    {
        // 회복이 아닌, 실제 HP 감소일 때만 효과 실행
        if (currentHP < previousHP)
        {
            // 연속 피격되면 이전 효과를 중단하고 다시 시작
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeHitEffect());
        }

        previousHP = currentHP;
    }

    private IEnumerator FadeHitEffect()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        hitImage.color = hitColor;

        while (elapsed < duration)
        {
            // 게임 시간이 멈춰도 효과는 사라지도록 설정
            elapsed += Time.unscaledDeltaTime;

            Color color = hitColor;

            // 불투명도를 서서히 0으로 감소
            color.a = Mathf.Lerp(
                hitColor.a,
                0f,
                elapsed / duration
            );

            hitImage.color = color;
            yield return null;
        }

        hitImage.color = new Color(
            hitColor.r,
            hitColor.g,
            hitColor.b,
            0f
        );

        fadeCoroutine = null;
    }

    private void OnDestroy()
    {
        // 삭제될 때 이벤트 연결과 생성한 화면 정리
        if (playerHealth != null)
            playerHealth.HealthChanged -= OnHealthChanged;

        if (effectCanvas != null)
            Destroy(effectCanvas);
    }
}