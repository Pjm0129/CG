using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public enum ItemType
    {
        Heal,
        Speed,
        Reflect
    }

    [Header("Item")]
    [SerializeField]
    private ItemType itemType = ItemType.Heal;

    [Header("Pickup")]
    [SerializeField]
    private float pickupRadius = 1.5f;

    [Header("Heal")]
    [SerializeField]
    private int healAmount = 1;

    [Header("Speed")]
    [SerializeField]
    private float speedMultiplier = 1.5f;

    [SerializeField]
    private float speedDuration = 5f;

    [Header("Reflect")]
    [SerializeField]
    private float reflectDuration = 5f;

    private Transform player;

    private bool collected;

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        FindPlayer();
    }

    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (collected)
            return;

        if (player == null)
        {
            FindPlayer();

            if (player == null)
                return;
        }

        Vector3 playerPosition =
            player.position;

        Vector3 itemPosition =
            transform.position;

        // 수평 거리만 계산
        playerPosition.y = 0f;
        itemPosition.y = 0f;

        float distance =
            Vector3.Distance(
                playerPosition,
                itemPosition
            );

        if (
            distance <=
            pickupRadius
        )
        {
            Collect();
        }
    }

    // =====================================================
    // FIND PLAYER
    // =====================================================

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (playerObject == null)
        {
            return;
        }

        player =
            playerObject.transform;
    }

    // =====================================================
    // COLLECT
    // =====================================================

    private void Collect()
    {
        if (collected)
            return;

        collected =
            true;

        switch (itemType)
        {
            case ItemType.Heal:

                ApplyHeal();

                break;

            case ItemType.Speed:

                ApplySpeed();

                break;

            case ItemType.Reflect:

                ApplyReflect();

                break;
        }

        Destroy(
            gameObject
        );
    }

    // =====================================================
    // HEAL
    // =====================================================

    private void ApplyHeal()
    {
        if (player == null)
            return;

        PlayerHealth playerHealth =
            player.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth =
                player.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth =
                player.GetComponentInChildren<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            Debug.LogWarning(
                "[PickupItem] PlayerHealth를 찾을 수 없습니다."
            );

            return;
        }

        playerHealth.Heal(
            healAmount
        );

        Debug.Log(
            $"[ITEM] HEAL +{healAmount}"
        );
    }

    // =====================================================
    // SPEED
    // =====================================================

    private void ApplySpeed()
    {
        if (player == null)
            return;

        PlayerItemEffects effects =
            player.GetComponent<PlayerItemEffects>();

        if (effects == null)
        {
            effects =
                player.GetComponentInParent<PlayerItemEffects>();
        }

        if (effects == null)
        {
            effects =
                player.GetComponentInChildren<PlayerItemEffects>();
        }

        if (effects == null)
        {
            Debug.LogWarning(
                "[PickupItem] PlayerItemEffects를 찾을 수 없습니다."
            );

            return;
        }

        effects.ApplySpeedBoost(
            speedMultiplier,
            speedDuration
        );

        Debug.Log(
            $"[ITEM] SPEED x{speedMultiplier} / " +
            $"{speedDuration}s"
        );
    }

    // =====================================================
    // REFLECT
    // =====================================================

    private void ApplyReflect()
    {
        if (player == null)
            return;

        PlayerReflectShield shield =
            player.GetComponent<PlayerReflectShield>();

        if (shield == null)
        {
            shield =
                player.GetComponentInParent<PlayerReflectShield>();
        }

        if (shield == null)
        {
            shield =
                player.GetComponentInChildren<PlayerReflectShield>();
        }

        // PlayerArmature에 미리 안 붙여놨어도 자동 추가
        if (shield == null)
        {
            shield =
                player.gameObject
                    .AddComponent<PlayerReflectShield>();
        }

        shield.ActivateShield(
            reflectDuration
        );

        Debug.Log(
            $"[ITEM] REFLECT SHIELD / " +
            $"{reflectDuration}s"
        );
    }

    // =====================================================
    // GIZMO
    // =====================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color =
            Color.cyan;

        Gizmos.DrawWireSphere(
            transform.position,
            Mathf.Max(
                0f,
                pickupRadius
            )
        );
    }
}