using UnityEngine;

public class MissionDevice : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private MissionManager missionManager;

    [SerializeField]
    private Renderer deviceRenderer;

    [Header("Activation Settings")]
    [SerializeField]
    private float activationRadius = 2f;

    [SerializeField]
    private float activationTime = 2f;

    [Header("Colors")]
    [SerializeField]
    private Color idleColor =
        Color.gray;

    [SerializeField]
    private Color chargingColor =
        Color.yellow;

    [SerializeField]
    private Color activatedColor =
        Color.green;

    private float chargeTimer;

    private bool activated;

    private bool availableForLevel =
        true;

    private MaterialPropertyBlock
        colorBlock;

    // =====================================================
    // UNITY
    // =====================================================

    private void Awake()
    {
        if (deviceRenderer == null)
        {
            deviceRenderer =
                GetComponent<Renderer>();
        }

        colorBlock =
            new MaterialPropertyBlock();

        SetColor(
            idleColor
        );
    }

    private void Update()
    {
        if (
            !availableForLevel ||
            activated ||
            missionManager == null
        )
        {
            return;
        }

        if (
            !missionManager
                .CanActivateDevices
        )
        {
            chargeTimer =
                0f;

            SetColor(
                idleColor
            );

            return;
        }

        Transform playerTransform =
            missionManager
                .PlayerTransform;

        if (playerTransform == null)
            return;

        Vector3 playerPosition =
            playerTransform.position;

        Vector3 devicePosition =
            transform.position;

        playerPosition.y =
            0f;

        devicePosition.y =
            0f;

        float distance =
            Vector3.Distance(
                playerPosition,
                devicePosition
            );

        if (
            distance <=
            activationRadius
        )
        {
            chargeTimer +=
                Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    chargeTimer /
                    Mathf.Max(
                        0.1f,
                        activationTime
                    )
                );

            SetColor(
                Color.Lerp(
                    chargingColor,
                    activatedColor,
                    progress
                )
            );

            if (
                progress >=
                1f
            )
            {
                ActivateDevice();
            }
        }
        else
        {
            chargeTimer =
                0f;

            SetColor(
                idleColor
            );
        }
    }

    // =====================================================
    // SET MANAGER
    // =====================================================

    public void SetMissionManager(
        MissionManager manager)
    {
        missionManager =
            manager;
    }

    // =====================================================
    // LEVEL CONTROL
    // =====================================================

    public void SetAvailableForLevel(
        bool available)
    {
        availableForLevel =
            available;

        chargeTimer =
            0f;

        activated =
            false;

        gameObject.SetActive(
            available
        );

        if (available)
        {
            SetColor(
                idleColor
            );
        }
    }

    public void ResetDevice()
    {
        chargeTimer =
            0f;

        activated =
            false;

        availableForLevel =
            true;

        SetColor(
            idleColor
        );
    }

    // =====================================================
    // ACTIVATE
    // =====================================================

    private void ActivateDevice()
    {
        if (
            activated ||
            !availableForLevel
        )
        {
            return;
        }

        activated =
            true;

        chargeTimer =
            activationTime;

        SetColor(
            activatedColor
        );

        if (
            missionManager != null
        )
        {
            missionManager
                .RegisterActivatedDevice(
                    this
                );
        }
    }

    // =====================================================
    // COLOR
    // =====================================================

    private void SetColor(
        Color color)
    {
        if (deviceRenderer == null)
            return;

        if (colorBlock == null)
        {
            colorBlock =
                new MaterialPropertyBlock();
        }

        deviceRenderer.GetPropertyBlock(
            colorBlock
        );

        colorBlock.SetColor(
            "_BaseColor",
            color
        );

        colorBlock.SetColor(
            "_Color",
            color
        );

        deviceRenderer.SetPropertyBlock(
            colorBlock
        );
    }

    // =====================================================
    // GIZMO
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            Mathf.Max(
                0f,
                activationRadius
            )
        );
    }
}