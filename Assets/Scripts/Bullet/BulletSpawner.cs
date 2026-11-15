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
    [SerializeField] private bool autoFire = false;

    [SerializeField] private float spawnInterval = 1f;


    [Header("Pattern Settings")]
    [Tooltip("발사할 때마다 패턴을 랜덤으로 선택합니다.")]
    [SerializeField] private bool randomPattern = true;

    [Tooltip("같은 패턴이 연속으로 나오는 것을 방지합니다.")]
    [SerializeField] private bool preventSamePattern = true;

    [Tooltip("Random Pattern OFF일 때 사용하는 패턴")]
    [SerializeField]
    private FirePattern firePattern =
        FirePattern.Single;


    [Header("Pattern Materials")]
    [SerializeField] private Material singleMaterial;

    [SerializeField] private Material spreadMaterial;

    [SerializeField] private Material radialMaterial;


    [Header("Pattern Colors")]
    [SerializeField]
    private Color singleColor =
        new Color(0f, 1f, 1f, 1f);

    [SerializeField]
    private Color spreadColor =
        new Color(1f, 0.35f, 0.05f, 1f);

    [SerializeField]
    private Color radialColor =
        new Color(1f, 0f, 1f, 1f);


    [Header("Spread Settings")]
    [SerializeField] private int spreadBulletCount = 5;

    [SerializeField] private float spreadAngle = 45f;


    [Header("Radial Settings")]
    [SerializeField] private int radialBulletCount = 12;


    private float spawnTimer;

    private FirePattern lastPattern;

    private bool hasLastPattern;


    // =====================================================
    // Update
    // =====================================================

    private void Update()
    {
        if (!autoFire)
            return;


        spawnTimer += Time.deltaTime;


        if (spawnTimer >= spawnInterval)
        {
            FireOnce();

            spawnTimer = 0f;
        }
    }


    // =====================================================
    // 외부에서 호출하는 1회 발사
    // =====================================================

    public void FireOnce()
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning(
                $"[{name}] Bullet Prefab이 없습니다."
            );

            return;
        }


        FirePattern selectedPattern;


        if (randomPattern)
        {
            selectedPattern =
                GetRandomPattern();
        }
        else
        {
            selectedPattern =
                firePattern;
        }


        switch (selectedPattern)
        {
            case FirePattern.Single:

                FireSingle();

                break;


            case FirePattern.Spread:

                FireSpread();

                break;


            case FirePattern.Radial:

                FireRadial();

                break;
        }


        lastPattern =
            selectedPattern;


        hasLastPattern =
            true;


        Debug.Log(
            $"[{name}] Fire Pattern : {selectedPattern}"
        );
    }


    // =====================================================
    // 랜덤 패턴 선택
    // =====================================================

    private FirePattern GetRandomPattern()
    {
        int patternCount =
            System.Enum.GetValues(
                typeof(FirePattern)
            ).Length;


        FirePattern selected =
            (FirePattern)Random.Range(
                0,
                patternCount
            );


        if (preventSamePattern &&
            hasLastPattern &&
            patternCount > 1)
        {
            while (selected == lastPattern)
            {
                selected =
                    (FirePattern)Random.Range(
                        0,
                        patternCount
                    );
            }
        }


        return selected;
    }


    // =====================================================
    // SINGLE
    // =====================================================

    private void FireSingle()
    {
        if (player == null)
            return;


        Vector3 direction =
            GetDirectionToPlayer();


        if (direction.sqrMagnitude <= 0.0001f)
            return;


        SpawnBullet(
            direction,
            FirePattern.Single
        );
    }


    // =====================================================
    // SPREAD
    // =====================================================

    private void FireSpread()
    {
        if (player == null)
            return;


        Vector3 centerDirection =
            GetDirectionToPlayer();


        if (centerDirection.sqrMagnitude <= 0.0001f)
            return;


        int count =
            Mathf.Max(
                1,
                spreadBulletCount
            );


        if (count == 1)
        {
            SpawnBullet(
                centerDirection,
                FirePattern.Spread
            );

            return;
        }


        float startAngle =
            -spreadAngle * 0.5f;


        float angleStep =
            spreadAngle /
            (count - 1);


        for (int i = 0; i < count; i++)
        {
            float angle =
                startAngle +
                angleStep * i;


            Vector3 direction =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) *
                centerDirection;


            SpawnBullet(
                direction.normalized,
                FirePattern.Spread
            );
        }
    }


    // =====================================================
    // RADIAL
    // =====================================================

    private void FireRadial()
    {
        int count =
            Mathf.Max(
                1,
                radialBulletCount
            );


        float angleStep =
            360f /
            count;


        for (int i = 0; i < count; i++)
        {
            float angle =
                angleStep *
                i;


            Vector3 direction =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) *
                Vector3.forward;


            SpawnBullet(
                direction.normalized,
                FirePattern.Radial
            );
        }
    }


    // =====================================================
    // 플레이어 방향
    // =====================================================

    private Vector3 GetDirectionToPlayer()
    {
        Vector3 direction =
            player.position -
            transform.position;


        direction.y =
            0f;


        if (direction.sqrMagnitude >
            0.0001f)
        {
            direction.Normalize();
        }


        return direction;
    }


    // =====================================================
    // Bullet 생성
    // =====================================================

    private void SpawnBullet(
        Vector3 direction,
        FirePattern pattern)
    {
        Bullet bullet =
            Instantiate(
                bulletPrefab,
                transform.position,
                Quaternion.identity
            );


        // --------------------------------------------
        // 패턴에 따른 Visual 적용
        // --------------------------------------------

        BulletVisual visual =
            bullet.GetComponent<BulletVisual>();


        if (visual == null)
        {
            visual =
                bullet.GetComponentInChildren<BulletVisual>();
        }


        if (visual != null)
        {
            Material material =
                GetPatternMaterial(
                    pattern
                );


            Color color =
                GetPatternColor(
                    pattern
                );


            visual.ApplyVisual(
                material,
                color
            );
        }
        else
        {
            Debug.LogWarning(
                "[BulletSpawner] Bullet Prefab에 " +
                "BulletVisual 컴포넌트가 없습니다."
            );
        }


        // --------------------------------------------
        // 기존 Bullet 이동 시작
        // --------------------------------------------

        bullet.Initialize(
            direction
        );
    }


    // =====================================================
    // Material
    // =====================================================

    private Material GetPatternMaterial(
        FirePattern pattern)
    {
        switch (pattern)
        {
            case FirePattern.Single:

                return singleMaterial;


            case FirePattern.Spread:

                return spreadMaterial;


            case FirePattern.Radial:

                return radialMaterial;
        }


        return null;
    }


    // =====================================================
    // Color
    // =====================================================

    private Color GetPatternColor(
        FirePattern pattern)
    {
        switch (pattern)
        {
            case FirePattern.Single:

                return singleColor;


            case FirePattern.Spread:

                return spreadColor;


            case FirePattern.Radial:

                return radialColor;
        }


        return Color.white;
    }


    // =====================================================
    // TurretFireManager에서 사용
    // =====================================================

    public void SetAutoFire(
        bool value)
    {
        autoFire =
            value;


        spawnTimer =
            0f;
    }


    public FirePattern GetLastPattern()
    {
        return lastPattern;
    }
}