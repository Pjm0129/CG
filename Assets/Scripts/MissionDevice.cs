using UnityEngine;

public class MissionDevice : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MissionManager missionManager;

    [SerializeField] private Renderer deviceRenderer;


    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 2f;

    [SerializeField] private float activationTime = 2f;


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

    private MaterialPropertyBlock colorBlock;


    private void Awake()
    {
        if (deviceRenderer == null)
        {
            deviceRenderer =
                GetComponentInChildren<Renderer>(
                    true
                );
        }


        colorBlock =
            new MaterialPropertyBlock();


        ResetDevice();
    }


    // MissionManager 자동 연결
    public void Initialize(
        MissionManager manager)
    {
        missionManager =
            manager;
    }


    // Device를 다시 사용할 때 초기화
    public void ResetDevice()
    {
        chargeTimer = 0f;

        activated = false;


        if (colorBlock == null)
        {
            colorBlock =
                new MaterialPropertyBlock();
        }


        SetColor(
            idleColor
        );
    }


    private void OnEnable()
    {
        ResetDevice();
    }


    private void Update()
    {
        if (activated ||
            missionManager == null)
        {
            return;
        }


        if (!missionManager.CanActivateDevices)
        {
            chargeTimer = 0f;

            SetColor(
                idleColor
            );

            return;
        }


        Transform player =
            missionManager.PlayerTransform;


        if (player == null)
        {
            return;
        }


        Vector3 playerPosition =
            player.position;


        Vector3 devicePosition =
            transform.position;


        // 높이는 무시
        playerPosition.y = 0f;

        devicePosition.y = 0f;


        float distance =
            Vector3.Distance(
                playerPosition,
                devicePosition
            );


        // 점령 범위 내부
        if (distance <= activationRadius)
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


            // 점령 완료
            if (progress >= 1f)
            {
                activated = true;


                SetColor(
                    activatedColor
                );


                missionManager
                    .RegisterActivatedDevice(
                        this
                    );
            }
        }

        // 점령 범위 밖
        else
        {
            chargeTimer = 0f;


            SetColor(
                idleColor
            );
        }
    }


    private void SetColor(
        Color color)
    {
        if (deviceRenderer == null)
        {
            return;
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