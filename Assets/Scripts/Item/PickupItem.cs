using UnityEngine;

public class PickupItem : MonoBehaviour
{
    public enum ItemType
    {
        Speed,
        Heal
    }

    [Header("Item Type")]
    [SerializeField] private ItemType itemType;

    [Header("Pickup Settings")]
    [SerializeField] private float pickupRadius = 1.5f;

    [Header("Heal Settings")]
    [SerializeField] private int healAmount = 1;

    [Header("Speed Settings")]
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float speedDuration = 5f;

    private Transform player;
    private bool collected;


    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
        else
        {
            Debug.LogError(
                "[PickupItem] Player 태그 오브젝트를 찾지 못했습니다."
            );
        }
    }


    private void Update()
    {
        if (collected || player == null)
            return;

        Vector3 playerPosition = player.position;
        Vector3 itemPosition = transform.position;

        // 높이 차이는 무시
        playerPosition.y = 0f;
        itemPosition.y = 0f;

        float distance =
            Vector3.Distance(
                playerPosition,
                itemPosition
            );

        if (distance <= pickupRadius)
        {
            Collect();
        }
    }


    private void Collect()
    {
        if (collected)
            return;

        collected = true;

        switch (itemType)
        {
            case ItemType.Heal:
                UseHealItem();
                break;

            case ItemType.Speed:
                UseSpeedItem();
                break;
        }

        Destroy(gameObject);
    }


    private void UseHealItem()
    {
        PlayerHealth health =
            player.GetComponent<PlayerHealth>();

        if (health == null)
        {
            health =
                player.GetComponentInParent<PlayerHealth>();
        }

        if (health == null)
        {
            Debug.LogWarning(
                "[PickupItem] PlayerHealth를 찾을 수 없습니다."
            );

            return;
        }

        health.Heal(
            Mathf.Max(1, healAmount)
        );

        Debug.Log(
            $"[ITEM] HEAL +{healAmount}"
        );
    }


    private void UseSpeedItem()
    {
        PlayerItemEffects effects =
            player.GetComponent<PlayerItemEffects>();

        if (effects == null)
        {
            effects =
                player.GetComponentInParent<PlayerItemEffects>();
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
            $"[ITEM] SPEED BOOST x{speedMultiplier} / {speedDuration}s"
        );
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawWireSphere(
            transform.position,
            pickupRadius
        );
    }
}