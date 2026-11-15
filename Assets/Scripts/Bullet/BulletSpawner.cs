using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

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

    [Header("Auto Fire")]
    [SerializeField] private bool autoFire = false;
    [SerializeField] private float spawnInterval = 1f;

    [Header("Pattern Selection")]
    [SerializeField] private bool randomPattern = true;
    [SerializeField] private bool preventSamePattern = true;
    [SerializeField] private FirePattern firePattern = FirePattern.Single;

    [Header("Pattern Materials")]
    [SerializeField] private Material singleMaterial;
    [SerializeField] private Material spreadMaterial;
    [SerializeField] private Material radialMaterial;

    [Header("Pattern Colors")]
    [SerializeField] private Color singleColor = Color.cyan;
    [SerializeField] private Color spreadColor = new Color(1f, 0.35f, 0.05f);
    [SerializeField] private Color radialColor = Color.magenta;

    [Header("Single Burst")]
    [Min(1)]
    [SerializeField] private int singleBurstCount = 3;
    [Min(0.05f)]
    [SerializeField] private float burstShotInterval = 0.2f;

    [Header("Spread Settings")]
    [Min(1)]
    [SerializeField] private int spreadBulletCount = 5;
    [Range(1f, 180f)]
    [SerializeField] private float spreadAngle = 45f;

    [Header("Radial Settings")]
    [Min(1)]
    [SerializeField] private int radialBulletCount = 12;

    [Header("Telegraph Settings")]
    [SerializeField] private bool useTelegraph = true;
    [SerializeField] private float singleTelegraphLength = 40f;
    [SerializeField] private float spreadTelegraphLength = 30f;
    [SerializeField] private float radialTelegraphLength = 14f;
    [SerializeField] private float telegraphHeightOffset = 0.15f;

    [Header("Neon Appearance")]
    [SerializeField] private float glowWidth = 0.18f;
    [SerializeField] private float coreWidth = 0.055f;
    [SerializeField] private float glowAlpha = 0.28f;
    [SerializeField] private float coreAlpha = 1f;
    [SerializeField] private float glowIntensity = 2.5f;
    [SerializeField] private float blinkSpeed = 5f;

    [Header("Radial Telegraph")]
    [SerializeField] private bool showRadialRing = true;
    [SerializeField] private float radialRingRadius = 3.5f;
    [Range(16, 128)]
    [SerializeField] private int ringSegments = 64;

    private float spawnTimer;
    private FirePattern lastPattern;
    private bool hasLastPattern;
    private bool isBusy;
    private Coroutine fireRoutine;
    private Material telegraphMaterial;
    private Color activeTelegraphColor;

    private readonly List<TelegraphPair> telegraphLines =
        new List<TelegraphPair>();

    public bool IsBusy => isBusy;
    public FirePattern LastPattern => lastPattern;

    private class TelegraphPair
    {
        public LineRenderer glow;
        public LineRenderer core;
    }

    private void Awake()
    {
        CreateTelegraphMaterial();
    }

    private void Start()
    {
        ResolvePlayer();
    }

    private void Update()
    {
        if (!autoFire || isBusy)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= Mathf.Max(0.05f, spawnInterval))
        {
            spawnTimer = 0f;
            FireWithTelegraph(0f);
        }
    }

    private void ResolvePlayer()
    {
        if (player != null && player.gameObject.activeInHierarchy)
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
            return;

        CharacterController controller =
            playerObject.GetComponent<CharacterController>();

        if (controller == null)
        {
            controller =
                playerObject.GetComponentInChildren<CharacterController>(true);
        }

        player = controller != null
            ? controller.transform
            : playerObject.transform;
    }

    private bool TryGetPlayerDirection(out Vector3 direction)
    {
        direction = Vector3.zero;

        if (player == null || !player.gameObject.activeInHierarchy)
            ResolvePlayer();

        if (player == null)
            return false;

        direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            return false;

        direction.Normalize();
        return true;
    }

    private void CreateTelegraphMaterial()
    {
        Shader shader = Shader.Find("DodgeRush/NeonTelegraph");

        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogWarning(
                "[BulletSpawner] 전조용 Shader를 찾지 못했습니다. " +
                "전조 선은 표시되지 않지만 탄환 발사는 진행됩니다."
            );
            return;
        }

        telegraphMaterial = new Material(shader);
        telegraphMaterial.name = "Runtime_Neon_Telegraph";

        if (telegraphMaterial.HasProperty("_Intensity"))
            telegraphMaterial.SetFloat("_Intensity", glowIntensity);

        if (telegraphMaterial.HasProperty("_BaseColor"))
            telegraphMaterial.SetColor("_BaseColor", Color.white);

        if (telegraphMaterial.HasProperty("_Color"))
            telegraphMaterial.SetColor("_Color", Color.white);
    }

    // 기존 호출부와의 호환성을 위해 유지합니다.
    // FireOnce는 전조 없이 해당 패턴을 즉시 시작합니다.
    public void FireOnce()
    {
        FireWithTelegraph(0f);
    }

    // TurretFireManager에서 호출합니다.
    public void FireWithTelegraph(float duration)
    {
        if (isBusy)
            return;

        if (bulletPrefab == null)
        {
            Debug.LogWarning($"[{name}] Bullet Prefab이 연결되지 않았습니다.");
            return;
        }

        FirePattern selectedPattern = SelectPattern();

        if (selectedPattern == FirePattern.Single)
        {
            if (!TryGetPlayerDirection(out _))
            {
                Debug.LogWarning($"[{name}] Player를 찾지 못해 발사를 취소합니다.");
                return;
            }
        }
        else if (selectedPattern == FirePattern.Spread)
        {
            if (!TryGetPlayerDirection(out _))
            {
                Debug.LogWarning($"[{name}] Player를 찾지 못해 발사를 취소합니다.");
                return;
            }
        }

        lastPattern = selectedPattern;
        hasLastPattern = true;
        isBusy = true;

        float actualTelegraphDuration =
            useTelegraph ? Mathf.Max(0f, duration) : 0f;

        fireRoutine = StartCoroutine(
            FireSequence(selectedPattern, actualTelegraphDuration)
        );
    }

    private FirePattern SelectPattern()
    {
        if (!randomPattern)
            return firePattern;

        int patternCount = 3;

        if (preventSamePattern && hasLastPattern)
        {
            int lastIndex = (int)lastPattern;
            int randomIndex = Random.Range(0, patternCount - 1);

            if (randomIndex >= lastIndex)
                randomIndex++;

            return (FirePattern)randomIndex;
        }

        return (FirePattern)Random.Range(0, patternCount);
    }

    private IEnumerator FireSequence(
        FirePattern pattern,
        float telegraphDuration)
    {
        List<Vector3> directions = BuildPatternDirections(pattern);

        if (directions.Count == 0)
        {
            FinishFiring();
            yield break;
        }

        if (telegraphDuration > 0f)
        {
            ShowTelegraph(pattern, directions);

            float elapsed = 0f;

            while (elapsed < telegraphDuration)
            {
                // Single은 전조 중에도 현재 플레이어 위치를 따라 조준합니다.
                if (pattern == FirePattern.Single &&
                    TryGetPlayerDirection(out Vector3 currentDirection))
                {
                    directions[0] = currentDirection;
                    UpdateSingleTelegraph(currentDirection);
                }

                UpdateTelegraphPulse();

                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        ClearTelegraph();

        switch (pattern)
        {
            case FirePattern.Single:
                yield return FireSingleBurst();
                break;

            case FirePattern.Spread:
                FireDirections(directions, FirePattern.Spread);
                break;

            case FirePattern.Radial:
                FireDirections(directions, FirePattern.Radial);
                break;
        }

        FinishFiring();
    }

    private List<Vector3> BuildPatternDirections(FirePattern pattern)
    {
        List<Vector3> directions = new List<Vector3>();

        switch (pattern)
        {
            case FirePattern.Single:
                {
                    if (TryGetPlayerDirection(out Vector3 direction))
                        directions.Add(direction);

                    break;
                }

            case FirePattern.Spread:
                {
                    if (!TryGetPlayerDirection(out Vector3 centerDirection))
                        break;

                    int count = Mathf.Max(1, spreadBulletCount);

                    if (count == 1)
                    {
                        directions.Add(centerDirection);
                        break;
                    }

                    float startAngle = -spreadAngle * 0.5f;
                    float angleStep = spreadAngle / (count - 1);

                    for (int i = 0; i < count; i++)
                    {
                        float angle = startAngle + angleStep * i;

                        Vector3 direction =
                            Quaternion.Euler(0f, angle, 0f) * centerDirection;

                        directions.Add(direction.normalized);
                    }

                    break;
                }

            case FirePattern.Radial:
                {
                    int count = Mathf.Max(1, radialBulletCount);
                    float angleStep = 360f / count;

                    for (int i = 0; i < count; i++)
                    {
                        float angle = angleStep * i;

                        Vector3 direction =
                            Quaternion.Euler(0f, angle, 0f) * Vector3.forward;

                        directions.Add(direction.normalized);
                    }

                    break;
                }
        }

        return directions;
    }

    private IEnumerator FireSingleBurst()
    {
        int shotCount = Mathf.Max(1, singleBurstCount);

        for (int i = 0; i < shotCount; i++)
        {
            // 매 탄환 발사 순간 플레이어 방향을 다시 계산합니다.
            // 이미 발사된 탄환은 Bullet 스크립트에 따라 직선 이동합니다.
            if (TryGetPlayerDirection(out Vector3 direction))
            {
                SpawnBullet(direction, FirePattern.Single);
            }

            if (i < shotCount - 1)
            {
                yield return new WaitForSeconds(
                    Mathf.Max(0.05f, burstShotInterval)
                );
            }
        }
    }

    private void FireDirections(
        List<Vector3> directions,
        FirePattern pattern)
    {
        foreach (Vector3 direction in directions)
            SpawnBullet(direction, pattern);
    }

    private void SpawnBullet(Vector3 direction, FirePattern pattern)
    {
        if (bulletPrefab == null || direction.sqrMagnitude <= 0.0001f)
            return;

        Bullet bullet = Instantiate(
            bulletPrefab,
            transform.position,
            Quaternion.identity
        );

        BulletVisual visual = bullet.GetComponent<BulletVisual>();

        if (visual == null)
            visual = bullet.GetComponentInChildren<BulletVisual>();

        if (visual != null)
        {
            visual.ApplyVisual(
                GetPatternMaterial(pattern),
                GetPatternColor(pattern)
            );
        }

        bullet.Initialize(direction.normalized);
    }

    private Material GetPatternMaterial(FirePattern pattern)
    {
        switch (pattern)
        {
            case FirePattern.Single:
                return singleMaterial;

            case FirePattern.Spread:
                return spreadMaterial;

            case FirePattern.Radial:
                return radialMaterial;

            default:
                return null;
        }
    }

    private Color GetPatternColor(FirePattern pattern)
    {
        switch (pattern)
        {
            case FirePattern.Single:
                return singleColor;

            case FirePattern.Spread:
                return spreadColor;

            case FirePattern.Radial:
                return radialColor;

            default:
                return Color.white;
        }
    }

    private Color GetTelegraphColor(FirePattern pattern)
    {
        return GetPatternColor(pattern);
    }

    private void ShowTelegraph(
        FirePattern pattern,
        List<Vector3> directions)
    {
        ClearTelegraph();

        if (telegraphMaterial == null)
            return;

        Vector3 origin =
            transform.position + Vector3.up * telegraphHeightOffset;

        activeTelegraphColor = GetTelegraphColor(pattern);

        switch (pattern)
        {
            case FirePattern.Single:
                if (directions.Count > 0)
                {
                    CreateLine(
                        origin,
                        origin + directions[0] * singleTelegraphLength
                    );
                }
                break;

            case FirePattern.Spread:
                foreach (Vector3 direction in directions)
                {
                    CreateLine(
                        origin,
                        origin + direction * spreadTelegraphLength
                    );
                }
                break;

            case FirePattern.Radial:
                foreach (Vector3 direction in directions)
                {
                    CreateLine(
                        origin,
                        origin + direction * radialTelegraphLength
                    );
                }

                if (showRadialRing)
                    CreateCircle(origin, radialRingRadius);

                break;
        }

        UpdateTelegraphPulse();
    }

    private void CreateLine(Vector3 start, Vector3 end)
    {
        TelegraphPair pair = CreateTelegraphPair("Telegraph_Line");

        SetPairPositions(pair, new Vector3[] { start, end });
        telegraphLines.Add(pair);
    }

    private void CreateCircle(Vector3 center, float radius)
    {
        TelegraphPair pair = CreateTelegraphPair("Telegraph_Ring");

        int segments = Mathf.Max(16, ringSegments);
        Vector3[] points = new Vector3[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;

            points[i] = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );
        }

        SetPairPositions(pair, points);
        telegraphLines.Add(pair);
    }

    private TelegraphPair CreateTelegraphPair(string objectName)
    {
        TelegraphPair pair = new TelegraphPair();

        pair.glow = CreateLineRenderer(
            objectName + "_Glow",
            glowWidth
        );

        pair.core = CreateLineRenderer(
            objectName + "_Core",
            coreWidth
        );

        return pair;
    }

    private LineRenderer CreateLineRenderer(
        string objectName,
        float width)
    {
        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(transform, true);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.positionCount = 2;
        line.sharedMaterial = telegraphMaterial;
        line.startWidth = width;
        line.endWidth = width;
        line.alignment = LineAlignment.View;
        line.numCapVertices = 8;
        line.numCornerVertices = 8;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;

        return line;
    }

    private void SetPairPositions(
        TelegraphPair pair,
        Vector3[] positions)
    {
        if (pair.glow != null)
        {
            pair.glow.positionCount = positions.Length;
            pair.glow.SetPositions(positions);
        }

        if (pair.core != null)
        {
            pair.core.positionCount = positions.Length;
            pair.core.SetPositions(positions);
        }
    }

    private void UpdateSingleTelegraph(Vector3 direction)
    {
        if (telegraphLines.Count == 0)
            return;

        Vector3 origin =
            transform.position + Vector3.up * telegraphHeightOffset;

        Vector3 end =
            origin + direction * singleTelegraphLength;

        SetPairPositions(
            telegraphLines[0],
            new Vector3[] { origin, end }
        );
    }

    private void UpdateTelegraphPulse()
    {
        float pulse = Mathf.Lerp(
            0.65f,
            1.15f,
            Mathf.PingPong(Time.time * blinkSpeed, 1f)
        );

        Color glow = activeTelegraphColor;
        glow.a = Mathf.Clamp01(glowAlpha * pulse);

        Color core = activeTelegraphColor;
        core.a = Mathf.Clamp01(coreAlpha * pulse);

        foreach (TelegraphPair pair in telegraphLines)
        {
            if (pair.glow != null)
            {
                pair.glow.startColor = glow;
                pair.glow.endColor = glow;
            }

            if (pair.core != null)
            {
                pair.core.startColor = core;
                pair.core.endColor = core;
            }
        }
    }

    private void ClearTelegraph()
    {
        foreach (TelegraphPair pair in telegraphLines)
        {
            if (pair.glow != null)
                Destroy(pair.glow.gameObject);

            if (pair.core != null)
                Destroy(pair.core.gameObject);
        }

        telegraphLines.Clear();
    }

    private void FinishFiring()
    {
        ClearTelegraph();
        isBusy = false;
        fireRoutine = null;
    }

    public void SetAutoFire(bool value)
    {
        autoFire = value;
        spawnTimer = 0f;
    }

    public FirePattern GetLastPattern()
    {
        return lastPattern;
    }

    private void OnDisable()
    {
        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
            fireRoutine = null;
        }

        isBusy = false;
        ClearTelegraph();
    }

    private void OnDestroy()
    {
        ClearTelegraph();

        if (telegraphMaterial != null)
        {
            Destroy(telegraphMaterial);
            telegraphMaterial = null;
        }
    }
}
