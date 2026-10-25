using UnityEngine;

public class BulletSpawner : MonoBehaviour
{
    public enum FirePattern
    {
        Single,
        Spread,
        Radial
    }

    [Header("References")]
    [SerializeField] private Bullet bulletPrefab;
    [SerializeField] private Transform player;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 1f;
    [SerializeField] private FirePattern firePattern = FirePattern.Single;

    [Header("Spread Settings")]
    [SerializeField] private int spreadBulletCount = 5;
    [SerializeField] private float spreadAngle = 45f;

    [Header("Radial Settings")]
    [SerializeField] private int radialBulletCount = 12;

    private float spawnTimer;

    private void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            SpawnPattern();
            spawnTimer = 0f;
        }
    }

    private void SpawnPattern()
    {
        if (bulletPrefab == null)
            return;

        switch (firePattern)
        {
            case FirePattern.Single:
                SpawnSingle();
                break;

            case FirePattern.Spread:
                SpawnSpread();
                break;

            case FirePattern.Radial:
                SpawnRadial();
                break;
        }
    }

    private void SpawnSingle()
    {
        if (!TryGetDirectionToPlayer(out Vector3 direction))
            return;

        SpawnBullet(direction);
    }

    private void SpawnSpread()
    {
        if (!TryGetDirectionToPlayer(out Vector3 centerDirection))
            return;

        int bulletCount = Mathf.Max(1, spreadBulletCount);

        if (bulletCount == 1)
        {
            SpawnBullet(centerDirection);
            return;
        }

        float startAngle = -spreadAngle * 0.5f;
        float angleStep = spreadAngle / (bulletCount - 1);

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = startAngle + angleStep * i;

            Vector3 direction =
                Quaternion.AngleAxis(angle, Vector3.up) * centerDirection;

            SpawnBullet(direction);
        }
    }

    private void SpawnRadial()
    {
        int bulletCount = Mathf.Max(1, radialBulletCount);
        float angleStep = 360f / bulletCount;

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = angleStep * i;
            float radians = angle * Mathf.Deg2Rad;

            Vector3 direction = new Vector3(
                Mathf.Sin(radians),
                0f,
                Mathf.Cos(radians)
            );

            SpawnBullet(direction);
        }
    }

    private bool TryGetDirectionToPlayer(out Vector3 direction)
    {
        direction = Vector3.zero;

        if (player == null)
            return false;

        direction = player.position - transform.position;

        // 탄환은 수평(XZ 평면)으로 이동
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        direction.Normalize();
        return true;
    }

    private void SpawnBullet(Vector3 direction)
    {
        Bullet bullet = Instantiate(
            bulletPrefab,
            transform.position,
            Quaternion.identity
        );

        bullet.Initialize(direction.normalized);
    }
}
