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
        // Renderer가 부모에 없으면
        // 자식 모델에서도 자동 검색
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


    /// <summary>
    /// MissionManager가 자동으로 연결합니다.
    /// </summary>
    public void Initialize(
        MissionManager manager)
    {
        missionManager =
            manager;
    }


    /// <summary>
    /// 새로운 미션 Device로 활성화할 때 초기화
    /// </summary>
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


        // 높이는 무시하고
        // 바닥 거리만 계산
        playerPosition.y = 0f;

        devicePosition.y = 0f;


        float distance =
            Vector3.Distance(
                playerPosition,
                devicePosition
            );


        // 점령 구역 안
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


        // 구역 밖으로 나가면 점령 초기화
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