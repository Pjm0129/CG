using StarterAssets;
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class MouseSensitivityApplier : MonoBehaviour
{
    private StarterAssetsInputs inputs;
    private Vector2 rawLook;
    private bool scaledThisFrame;

    private void Awake()
    {
        inputs = GetComponent<StarterAssetsInputs>();

        if (inputs == null)
        {
            Debug.LogError(
                "MouseSensitivityApplier: StarterAssetsInputs가 없습니다.",
                this
            );
        }
    }

    private void Update()
    {
        if (inputs == null)
            return;

        rawLook = inputs.look;
        inputs.look = rawLook * GameSettings.MouseSensitivity;
        scaledThisFrame = true;
    }

    private void LateUpdate()
    {
        if (inputs == null || !scaledThisFrame)
            return;

        // ThirdPersonController의 LateUpdate가 감도 적용 값을 사용한 뒤
        // 원래 입력값으로 돌려놓아 프레임마다 값이 누적되지 않게 합니다.
        inputs.look = rawLook;
        scaledThisFrame = false;
    }
}
