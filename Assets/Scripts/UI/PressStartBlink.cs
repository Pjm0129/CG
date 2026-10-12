using UnityEngine;
using TMPro;

public class PressStartBlink : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float blinkSpeed = 2f;
    [SerializeField] private float minAlpha = 0.25f;

    private void Awake()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (text == null)
            return;

        float alpha = Mathf.Lerp(
            minAlpha,
            1f,
            (Mathf.Sin(Time.time * blinkSpeed) + 1f) * 0.5f
        );

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }
}