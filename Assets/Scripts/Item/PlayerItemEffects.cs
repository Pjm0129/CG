using System.Collections;
using UnityEngine;
using StarterAssets;

public class PlayerItemEffects : MonoBehaviour
{
    private ThirdPersonController controller;

    private Coroutine speedBoostCoroutine;

    private float originalMoveSpeed;
    private float originalSprintSpeed;


    private void Awake()
    {
        controller =
            GetComponent<ThirdPersonController>();

        if (controller == null)
        {
            controller =
                GetComponentInParent<ThirdPersonController>();
        }

        if (controller == null)
        {
            Debug.LogError(
                "[PlayerItemEffects] ThirdPersonController를 찾을 수 없습니다."
            );

            return;
        }

        originalMoveSpeed =
            controller.MoveSpeed;

        originalSprintSpeed =
            controller.SprintSpeed;
    }


    public void ApplySpeedBoost(
        float multiplier,
        float duration)
    {
        if (controller == null)
            return;

        if (speedBoostCoroutine != null)
        {
            StopCoroutine(
                speedBoostCoroutine
            );
        }

        speedBoostCoroutine =
            StartCoroutine(
                SpeedBoostRoutine(
                    multiplier,
                    duration
                )
            );
    }


    private IEnumerator SpeedBoostRoutine(
        float multiplier,
        float duration)
    {
        float safeMultiplier =
            Mathf.Max(
                1f,
                multiplier
            );

        controller.MoveSpeed =
            originalMoveSpeed *
            safeMultiplier;

        controller.SprintSpeed =
            originalSprintSpeed *
            safeMultiplier;


        Debug.Log(
            $"[ITEM] SPEED BOOST START : " +
            $"{originalMoveSpeed} → {controller.MoveSpeed}"
        );


        yield return new WaitForSeconds(
            Mathf.Max(
                0.1f,
                duration
            )
        );


        controller.MoveSpeed =
            originalMoveSpeed;

        controller.SprintSpeed =
            originalSprintSpeed;


        Debug.Log(
            $"[ITEM] SPEED BOOST END : " +
            $"{controller.MoveSpeed}"
        );


        speedBoostCoroutine = null;
    }


    private void OnDisable()
    {
        if (controller == null)
            return;

        controller.MoveSpeed =
            originalMoveSpeed;

        controller.SprintSpeed =
            originalSprintSpeed;
    }
}