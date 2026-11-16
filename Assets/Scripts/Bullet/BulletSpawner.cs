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
        Radial,
        Spiral
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

    [Header("Single Visual")]
    [SerializeField] private Material singleMaterial;
    [SerializeField]
    private Color singleColor =
        new Color(0.1f, 0.85f, 1f, 1f);

    [Header("Spread Visual")]
    [SerializeField] private Material spreadMaterial;
    [SerializeField]
    private Color spreadColor =
        new Color(1f, 0.35f, 0.05f, 1f);

    [Header("Radial Visual")]
    [SerializeField] private Material radialMaterial;
    [SerializeField]
    private Color radialColor =
        new Color(1f, 0.1f, 0.9f, 1f);

    [Header("Spiral Visual")]
    [Tooltip("비워두면 Radial Material을 사용합니다.")]
    [SerializeField] private Material spiralMaterial;

    [SerializeField]
    private Color spiralColor =
        new Color(0.25f, 1f, 0.55f, 1f);

    [Header("Single Burst")]
    [SerializeField] private int singleBurstCount = 3;
    [SerializeField] private float burstShotInterval = 0.2f;

    [Header("Spread")]
    [SerializeField] private int spreadBulletCount = 5;
    [SerializeField] private float spreadAngle = 45f;

    [Header("Radial")]
    [SerializeField] private int radialBulletCount = 12;

    [Header("Spiral")]
    [Tooltip("한 번의 Spiral 공격에서 반복 발사되는 횟수")]
    [SerializeField] private int spiralWaveCount = 5;

    [Tooltip("한 Wave에서 발사할 탄환 수")]
    [SerializeField] private int spiralBulletCount = 8;

    [Tooltip("다음 Wave마다 회전할 각도")]
    [SerializeField] private float spiralRotationStep = 12f;

    [Tooltip("Spiral Wave 사이의 발사 간격")]
    [SerializeField] private float spiralShotInterval = 0.12f;

    [Header("Telegraph")]
    [SerializeField] private bool useTelegraph = true;

    [SerializeField] private float singleTelegraphLength = 40f;
    [SerializeField] private float spreadTelegraphLength = 30f;
    [SerializeField] private float radialTelegraphLength = 14f;
    [SerializeField] private float spiralTelegraphLength = 14f;

    [SerializeField] private float telegraphHeightOffset = 0.15f;

    [Header("Neon Telegraph")]
    [SerializeField] private float glowWidth = 0.18f;
    [SerializeField] private float coreWidth = 0.055f;

    [Range(0f, 1f)]
    [SerializeField] private float glowAlpha = 0.28f;

    [Range(0f, 1f)]
    [SerializeField] private float coreAlpha = 1f;

    [SerializeField] private float glowIntensity = 2.5f;
    [SerializeField] private float blinkSpeed = 5f;

    [Header("Radial Ring")]
    [SerializeField] private bool showRadialRing = true;
    [SerializeField] private float radialRingRadius = 3.5f;
    [SerializeField] private int radialRingSegments = 64;

    private FirePattern lastPattern;
    private bool hasLastPattern;

    private bool isBusy;

    private float autoFireTimer;

    private Material telegraphMaterial;

    private readonly List<LineRenderer>
        telegraphGlowLines =
            new List<LineRenderer>();

    private readonly List<LineRenderer>
        telegraphCoreLines =
            new List<LineRenderer>();

    private LineRenderer radialGlowRing;
    private LineRenderer radialCoreRing;

    public bool IsBusy => isBusy;

    public FirePattern LastPattern =>
        lastPattern;

    // =====================================================
    // UNITY
    // =====================================================

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
        if (!autoFire)
            return;

        if (isBusy)
            return;

        autoFireTimer +=
            Time.deltaTime;

        if (autoFireTimer >= spawnInterval)
        {
            autoFireTimer = 0f;

            FireOnce();
        }
    }

    // =====================================================
    // PLAYER
    // =====================================================

    private void ResolvePlayer()
    {
        if (player != null)
            return;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (playerObject == null)
            return;

        CharacterController controller =
            playerObject.GetComponent<CharacterController>();

        if (controller == null)
        {
            controller =
                playerObject
                    .GetComponentInChildren<CharacterController>();
        }

        if (controller != null)
        {
            player =
                controller.transform;
        }
        else
        {
            player =
                playerObject.transform;
        }
    }

    private bool TryGetPlayerDirection(
        out Vector3 direction)
    {
        direction =
            transform.forward;

        if (player == null)
        {
            ResolvePlayer();
        }

        if (player == null)
            return false;

        Vector3 targetPosition =
            player.position;

        Vector3 spawnPosition =
            transform.position;

        targetPosition.y =
            spawnPosition.y;

        direction =
            targetPosition -
            spawnPosition;

        if (direction.sqrMagnitude <= 0.001f)
            return false;

        direction.Normalize();

        return true;
    }

    // =====================================================
    // FIRE ENTRY
    // =====================================================

    public void FireOnce()
    {
        FireWithTelegraph(
            0f
        );
    }

    public void FireWithTelegraph(
        float telegraphDuration)
    {
        if (isBusy)
            return;

        FirePattern selectedPattern =
            SelectPattern();

        if (
            selectedPattern == FirePattern.Single ||
            selectedPattern == FirePattern.Spread
        )
        {
            if (
                !TryGetPlayerDirection(
                    out _
                )
            )
            {
                return;
            }
        }

        lastPattern =
            selectedPattern;

        hasLastPattern =
            true;

        StartCoroutine(
            FireSequence(
                selectedPattern,
                telegraphDuration
            )
        );
    }

    // =====================================================
    // PATTERN SELECTION
    // =====================================================

    private FirePattern SelectPattern()
    {
        if (!randomPattern)
        {
            return firePattern;
        }

        int patternCount =
            System.Enum.GetValues(
                typeof(FirePattern)
            ).Length;

        FirePattern selected;

        int safety =
            0;

        do
        {
            selected =
                (FirePattern)Random.Range(
                    0,
                    patternCount
                );

            safety++;
        }
        while (
            preventSamePattern &&
            hasLastPattern &&
            selected == lastPattern &&
            safety < 20
        );

        return selected;
    }

    // =====================================================
    // FIRE SEQUENCE
    // =====================================================

    private IEnumerator FireSequence(
        FirePattern pattern,
        float telegraphDuration)
    {
        isBusy =
            true;

        if (
            useTelegraph &&
            telegraphDuration > 0f
        )
        {
            yield return StartCoroutine(
                TelegraphRoutine(
                    pattern,
                    telegraphDuration
                )
            );
        }

        HideTelegraph();

        switch (pattern)
        {
            case FirePattern.Single:

                yield return StartCoroutine(
                    FireSingleBurst()
                );

                break;

            case FirePattern.Spread:

                FireSpread();

                break;

            case FirePattern.Radial:

                FireRadial();

                break;

            case FirePattern.Spiral:

                yield return StartCoroutine(
                    FireSpiral()
                );

                break;
        }

        isBusy =
            false;
    }

    // =====================================================
    // SINGLE
    // =====================================================

    private IEnumerator FireSingleBurst()
    {
        int count =
            Mathf.Max(
                1,
                singleBurstCount
            );

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            if (
                TryGetPlayerDirection(
                    out Vector3 direction
                )
            )
            {
                SpawnBullet(
                    direction,
                    FirePattern.Single
                );
            }

            if (
                i <
                count - 1
            )
            {
                yield return new WaitForSeconds(
                    Mathf.Max(
                        0f,
                        burstShotInterval
                    )
                );
            }
        }
    }

    // =====================================================
    // SPREAD
    // =====================================================

    private void FireSpread()
    {
        if (
            !TryGetPlayerDirection(
                out Vector3 centerDirection
            )
        )
        {
            return;
        }

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

        for (
            int i = 0;
            i < count;
            i++
        )
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
                direction,
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

        float step =
            360f /
            count;

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            float angle =
                step * i;

            Vector3 direction =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) *
                Vector3.forward;

            SpawnBullet(
                direction,
                FirePattern.Radial
            );
        }
    }

    // =====================================================
    // SPIRAL
    // =====================================================

    private IEnumerator FireSpiral()
    {
        int waveCount =
            Mathf.Max(
                1,
                spiralWaveCount
            );

        int bulletCount =
            Mathf.Max(
                1,
                spiralBulletCount
            );

        float bulletAngleStep =
            360f /
            bulletCount;

        float currentOffset =
            Random.Range(
                0f,
                360f
            );

        for (
            int wave = 0;
            wave < waveCount;
            wave++
        )
        {
            for (
                int i = 0;
                i < bulletCount;
                i++
            )
            {
                float angle =
                    currentOffset +
                    bulletAngleStep * i;

                Vector3 direction =
                    Quaternion.Euler(
                        0f,
                        angle,
                        0f
                    ) *
                    Vector3.forward;

                SpawnBullet(
                    direction,
                    FirePattern.Spiral
                );
            }

            currentOffset +=
                spiralRotationStep;

            if (
                wave <
                waveCount - 1
            )
            {
                yield return new WaitForSeconds(
                    Mathf.Max(
                        0f,
                        spiralShotInterval
                    )
                );
            }
        }
    }

    // =====================================================
    // BULLET SPAWN
    // =====================================================

    private void SpawnBullet(
        Vector3 direction,
        FirePattern pattern)
    {
        if (bulletPrefab == null)
            return;

        Bullet bullet =
            Instantiate(
                bulletPrefab,
                transform.position,
                Quaternion.identity
            );

        ApplyBulletVisual(
            bullet,
            pattern
        );

        bullet.Initialize(
            direction.normalized
        );
    }

    // =====================================================
    // BULLET VISUAL
    // =====================================================

    private void ApplyBulletVisual(
        Bullet bullet,
        FirePattern pattern)
    {
        if (bullet == null)
            return;

        BulletVisual visual =
            bullet.GetComponent<BulletVisual>();

        if (visual == null)
        {
            visual =
                bullet.GetComponentInChildren<BulletVisual>();
        }

        if (visual == null)
            return;

        Material material =
            null;

        Color color =
            Color.white;

        switch (pattern)
        {
            case FirePattern.Single:

                material =
                    singleMaterial;

                color =
                    singleColor;

                break;

            case FirePattern.Spread:

                material =
                    spreadMaterial;

                color =
                    spreadColor;

                break;

            case FirePattern.Radial:

                material =
                    radialMaterial;

                color =
                    radialColor;

                break;

            case FirePattern.Spiral:

                material =
                    spiralMaterial != null
                        ? spiralMaterial
                        : radialMaterial;

                color =
                    spiralColor;

                break;
        }

        visual.ApplyVisual(
            material,
            color
        );
    }

    // =====================================================
    // TELEGRAPH
    // =====================================================

    private IEnumerator TelegraphRoutine(
        FirePattern pattern,
        float duration)
    {
        float elapsed =
            0f;

        CreateTelegraphForPattern(
            pattern
        );

        while (
            elapsed <
            duration
        )
        {
            float progress =
                Mathf.Clamp01(
                    elapsed /
                    duration
                );

            UpdateTelegraph(
                pattern,
                progress
            );

            elapsed +=
                Time.deltaTime;

            yield return null;
        }
    }

    private void CreateTelegraphForPattern(
        FirePattern pattern)
    {
        HideTelegraph();

        switch (pattern)
        {
            case FirePattern.Single:

                CreateLinePair();

                break;

            case FirePattern.Spread:

                for (
                    int i = 0;
                    i <
                    Mathf.Max(
                        1,
                        spreadBulletCount
                    );
                    i++
                )
                {
                    CreateLinePair();
                }

                break;

            case FirePattern.Radial:

                for (
                    int i = 0;
                    i <
                    Mathf.Max(
                        1,
                        radialBulletCount
                    );
                    i++
                )
                {
                    CreateLinePair();
                }

                CreateRadialRing();

                break;

            case FirePattern.Spiral:

                for (
                    int i = 0;
                    i <
                    Mathf.Max(
                        1,
                        spiralBulletCount
                    );
                    i++
                )
                {
                    CreateLinePair();
                }

                CreateRadialRing();

                break;
        }
    }

    private void UpdateTelegraph(
        FirePattern pattern,
        float progress)
    {
        Color patternColor =
            GetPatternColor(
                pattern
            );

        float blink =
            0.65f +
            Mathf.Sin(
                Time.time *
                blinkSpeed *
                Mathf.PI *
                2f
            ) *
            0.35f;

        Color glowColor =
            patternColor;

        glowColor.a =
            glowAlpha *
            blink;

        Color coreColor =
            patternColor;

        coreColor.a =
            coreAlpha;

        switch (pattern)
        {
            case FirePattern.Single:

                UpdateSingleTelegraph();

                break;

            case FirePattern.Spread:

                UpdateSpreadTelegraph();

                break;

            case FirePattern.Radial:

                UpdateRadialTelegraph(
                    0f
                );

                break;

            case FirePattern.Spiral:

                float rotation =
                    Time.time *
                    90f;

                UpdateRadialTelegraph(
                    rotation
                );

                break;
        }

        UpdateLineColors(
            glowColor,
            coreColor
        );

        UpdateRingColors(
            glowColor,
            coreColor
        );
    }

    // =====================================================
    // SINGLE TELEGRAPH
    // =====================================================

    private void UpdateSingleTelegraph()
    {
        if (
            telegraphGlowLines.Count <
            1
        )
        {
            return;
        }

        if (
            !TryGetPlayerDirection(
                out Vector3 direction
            )
        )
        {
            return;
        }

        SetLinePair(
            0,
            direction,
            singleTelegraphLength
        );
    }

    // =====================================================
    // SPREAD TELEGRAPH
    // =====================================================

    private void UpdateSpreadTelegraph()
    {
        if (
            !TryGetPlayerDirection(
                out Vector3 centerDirection
            )
        )
        {
            return;
        }

        int count =
            telegraphGlowLines.Count;

        if (count <= 0)
            return;

        if (count == 1)
        {
            SetLinePair(
                0,
                centerDirection,
                spreadTelegraphLength
            );

            return;
        }

        float startAngle =
            -spreadAngle *
            0.5f;

        float step =
            spreadAngle /
            (count - 1);

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            float angle =
                startAngle +
                step * i;

            Vector3 direction =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) *
                centerDirection;

            SetLinePair(
                i,
                direction,
                spreadTelegraphLength
            );
        }
    }

    // =====================================================
    // RADIAL / SPIRAL TELEGRAPH
    // =====================================================

    private void UpdateRadialTelegraph(
        float angleOffset)
    {
        int count =
            telegraphGlowLines.Count;

        if (count <= 0)
            return;

        float step =
            360f /
            count;

        float length =
            lastPattern ==
            FirePattern.Spiral
                ? spiralTelegraphLength
                : radialTelegraphLength;

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            float angle =
                angleOffset +
                step * i;

            Vector3 direction =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) *
                Vector3.forward;

            SetLinePair(
                i,
                direction,
                length
            );
        }
    }

    // =====================================================
    // LINE CREATION
    // =====================================================

    private void CreateLinePair()
    {
        LineRenderer glow =
            CreateLineRenderer(
                "Telegraph_Glow",
                glowWidth
            );

        LineRenderer core =
            CreateLineRenderer(
                "Telegraph_Core",
                coreWidth
            );

        telegraphGlowLines.Add(
            glow
        );

        telegraphCoreLines.Add(
            core
        );
    }

    private LineRenderer CreateLineRenderer(
        string objectName,
        float width)
    {
        GameObject lineObject =
            new GameObject(
                objectName
            );

        lineObject.transform.SetParent(
            transform,
            false
        );

        LineRenderer line =
            lineObject.AddComponent<LineRenderer>();

        line.material =
            telegraphMaterial;

        line.useWorldSpace =
            true;

        line.positionCount =
            2;

        line.startWidth =
            width;

        line.endWidth =
            width;

        line.numCapVertices =
            4;

        line.shadowCastingMode =
            ShadowCastingMode.Off;

        line.receiveShadows =
            false;

        return line;
    }

    private void SetLinePair(
        int index,
        Vector3 direction,
        float length)
    {
        if (
            index < 0 ||
            index >=
            telegraphGlowLines.Count ||
            index >=
            telegraphCoreLines.Count
        )
        {
            return;
        }

        Vector3 start =
            transform.position +
            Vector3.up *
            telegraphHeightOffset;

        Vector3 end =
            start +
            direction.normalized *
            length;

        telegraphGlowLines[index]
            .SetPosition(
                0,
                start
            );

        telegraphGlowLines[index]
            .SetPosition(
                1,
                end
            );

        telegraphCoreLines[index]
            .SetPosition(
                0,
                start
            );

        telegraphCoreLines[index]
            .SetPosition(
                1,
                end
            );
    }

    // =====================================================
    // RADIAL RING
    // =====================================================

    private void CreateRadialRing()
    {
        if (!showRadialRing)
            return;

        radialGlowRing =
            CreateRingRenderer(
                "RadialRing_Glow",
                glowWidth
            );

        radialCoreRing =
            CreateRingRenderer(
                "RadialRing_Core",
                coreWidth
            );

        UpdateRingPositions(
            radialGlowRing
        );

        UpdateRingPositions(
            radialCoreRing
        );
    }

    private LineRenderer CreateRingRenderer(
        string name,
        float width)
    {
        GameObject ringObject =
            new GameObject(
                name
            );

        ringObject.transform.SetParent(
            transform,
            false
        );

        LineRenderer ring =
            ringObject.AddComponent<LineRenderer>();

        ring.material =
            telegraphMaterial;

        ring.useWorldSpace =
            true;

        ring.loop =
            true;

        ring.positionCount =
            Mathf.Max(
                8,
                radialRingSegments
            );

        ring.startWidth =
            width;

        ring.endWidth =
            width;

        ring.shadowCastingMode =
            ShadowCastingMode.Off;

        ring.receiveShadows =
            false;

        return ring;
    }

    private void UpdateRingPositions(
        LineRenderer ring)
    {
        if (ring == null)
            return;

        int count =
            ring.positionCount;

        Vector3 center =
            transform.position +
            Vector3.up *
            telegraphHeightOffset;

        for (
            int i = 0;
            i < count;
            i++
        )
        {
            float angle =
                (float)i /
                count *
                Mathf.PI *
                2f;

            Vector3 offset =
                new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle)
                ) *
                radialRingRadius;

            ring.SetPosition(
                i,
                center + offset
            );
        }
    }

    // =====================================================
    // COLORS
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

            case FirePattern.Spiral:
                return spiralColor;
        }

        return Color.white;
    }

    private void UpdateLineColors(
        Color glowColor,
        Color coreColor)
    {
        for (
            int i = 0;
            i < telegraphGlowLines.Count;
            i++
        )
        {
            if (
                telegraphGlowLines[i] != null
            )
            {
                telegraphGlowLines[i]
                    .startColor =
                    glowColor;

                telegraphGlowLines[i]
                    .endColor =
                    glowColor;
            }
        }

        for (
            int i = 0;
            i < telegraphCoreLines.Count;
            i++
        )
        {
            if (
                telegraphCoreLines[i] != null
            )
            {
                telegraphCoreLines[i]
                    .startColor =
                    coreColor;

                telegraphCoreLines[i]
                    .endColor =
                    coreColor;
            }
        }
    }

    private void UpdateRingColors(
        Color glowColor,
        Color coreColor)
    {
        if (radialGlowRing != null)
        {
            radialGlowRing.startColor =
                glowColor;

            radialGlowRing.endColor =
                glowColor;
        }

        if (radialCoreRing != null)
        {
            radialCoreRing.startColor =
                coreColor;

            radialCoreRing.endColor =
                coreColor;
        }
    }

    // =====================================================
    // TELEGRAPH MATERIAL
    // =====================================================

    private void CreateTelegraphMaterial()
    {
        Shader shader =
            Shader.Find(
                "DodgeRush/NeonTelegraph"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );
        }

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Sprites/Default"
                );
        }

        if (shader == null)
            return;

        telegraphMaterial =
            new Material(
                shader
            );

        telegraphMaterial.name =
            "Runtime_NeonTelegraph";

        if (
            telegraphMaterial.HasProperty(
                "_Intensity"
            )
        )
        {
            telegraphMaterial.SetFloat(
                "_Intensity",
                glowIntensity
            );
        }
    }

    // =====================================================
    // HIDE TELEGRAPH
    // =====================================================

    private void HideTelegraph()
    {
        for (
            int i =
                telegraphGlowLines.Count - 1;
            i >= 0;
            i--
        )
        {
            if (
                telegraphGlowLines[i] != null
            )
            {
                Destroy(
                    telegraphGlowLines[i]
                        .gameObject
                );
            }
        }

        for (
            int i =
                telegraphCoreLines.Count - 1;
            i >= 0;
            i--
        )
        {
            if (
                telegraphCoreLines[i] != null
            )
            {
                Destroy(
                    telegraphCoreLines[i]
                        .gameObject
                );
            }
        }

        telegraphGlowLines.Clear();
        telegraphCoreLines.Clear();

        if (radialGlowRing != null)
        {
            Destroy(
                radialGlowRing.gameObject
            );

            radialGlowRing =
                null;
        }

        if (radialCoreRing != null)
        {
            Destroy(
                radialCoreRing.gameObject
            );

            radialCoreRing =
                null;
        }
    }

    // =====================================================
    // PUBLIC CONTROL
    // =====================================================

    public void SetAutoFire(
        bool value)
    {
        autoFire =
            value;

        autoFireTimer =
            0f;
    }

    public FirePattern GetLastPattern()
    {
        return lastPattern;
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    private void OnDestroy()
    {
        HideTelegraph();

        if (telegraphMaterial != null)
        {
            Destroy(
                telegraphMaterial
            );
        }
    }
}