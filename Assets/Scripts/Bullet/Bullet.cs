using UnityEngine;
using StarterAssets;

public class Bullet : MonoBehaviour
{
    [Header("Bullet")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float lifeTime = 5f;

    [Header("Damage")]
    [SerializeField] private int damage = 1;

    private Vector3 moveDirection;

    public void Initialize(Vector3 direction)
    {
        moveDirection = direction.normalized;

        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.position +=
            moveDirection * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.TakeDamage(damage);

        // 플레이어와 충돌하면 무조건 탄 제거
        Destroy(gameObject);
    }
}