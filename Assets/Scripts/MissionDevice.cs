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
    [SerializeField] private Color idleColor = Color.gray;
    [SerializeField] private Color chargingColor = Color.yellow;
    [SerializeField] private Color activatedColor = Color.green;

    private float chargeTimer;
    private bool activated;
    private MaterialPropertyBlock colorBlock;

    private void Awake()
    {
        if (deviceRenderer == null)
            deviceRenderer = GetComponent<Renderer>();

        colorBlock = new MaterialPropertyBlock();
        SetColor(idleColor);
    }

    private void Update()
    {
        if (activated || missionManager == null)
            return;

        if (!missionManager.CanActivateDevices)
        {
            chargeTimer = 0f;
            SetColor(idleColor);
            return;
        }

        Vector3 playerPosition =
            missionManager.PlayerTransform.position;

        Vector3 devicePosition = transform.position;

        playerPosition.y = 0f;
        devicePosition.y = 0f;

        float distance = Vector3.Distance(
            playerPosition,
            devicePosition
        );

        if (distance <= activationRadius)
        {
            chargeTimer += Time.deltaTime;

            float progress = Mathf.Clamp01(
                chargeTimer / Mathf.Max(0.1f, activationTime)
            );

            SetColor(Color.Lerp(
                chargingColor,
                activatedColor,
                progress
            ));

            if (progress >= 1f)
            {
                activated = true;
                SetColor(activatedColor);
                missionManager.RegisterActivatedDevice(this);
            }
        }
        else
        {
            chargeTimer = 0f;
            SetColor(idleColor);
        }
    }

    private void SetColor(Color color)
    {
        if (deviceRenderer == null)
            return;

        deviceRenderer.GetPropertyBlock(colorBlock);
        colorBlock.SetColor("_BaseColor", color);
        colorBlock.SetColor("_Color", color);
        deviceRenderer.SetPropertyBlock(colorBlock);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Gizmos.DrawWireSphere(
            transform.position,
            Mathf.Max(0f, activationRadius)
        );
    }
}