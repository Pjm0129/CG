using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform player;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1f;

    private float spawnTimer;

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            SpawnBullet();
            spawnTimer = 0f;
        }
    }

    private void SpawnBullet()
    {
        if (bulletPrefab == null || player == null)
            return;

        Vector3 direction =
            player.position - transform.position;

        // Y축 고정: 탄환은 수평 직선으로 이동
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return;

        direction.Normalize();

        Bullet bullet = Instantiate(
            bulletPrefab,
            transform.position,
            Quaternion.identity
        );

        bullet.Initialize(direction);
    }
}
