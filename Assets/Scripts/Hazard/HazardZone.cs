using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class HazardZone : MonoBehaviour
{
    [Header("Hazard Settings")]
    [SerializeField] private float radius = 3.5f;
    [SerializeField] private float warningDuration = 0.9f;
    [SerializeField] private int damage = 1;

    [Header("Warning Visual")]
    [SerializeField] private float lineWidth = 0.12f;
    [SerializeField] private int circleSegments = 64;

    [SerializeField]
    private Color warningColor =
        new Color(1f, 0.08f, 0.02f, 1f);

    [SerializeField]
    private Color dangerColor =
        new Color(1f, 0f, 0f, 1f);

    [Header("Fill Settings")]
    [Range(0f, 1f)]
    [SerializeField] private float fillAlpha = 0.3f;

    [SerializeField] private float visualHeight = 0.15f;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed = 6f;

    [Header("Explosion")]
    [Tooltip("ppfxExplosionFireball01 프리팹 연결")]
    [SerializeField] private GameObject explosionPrefab;

    [Tooltip("폭발 이펙트 위치 Y 보정")]
    [SerializeField] private float explosionHeightOffset = 0.1f;

    [Tooltip("폭발 이펙트 크기")]
    [SerializeField] private float explosionScale = 1f;

    [Tooltip("폭발 이펙트 자동 삭제 시간")]
    [SerializeField] private float explosionLifetime = 3f;

    private Transform player;
    private PlayerHealth playerHealth;

    private LineRenderer warningCircle;

    private GameObject fillObject;
    private MeshRenderer fillRenderer;

    private Material lineMaterial;
    private Material fillMaterial;

    private bool exploded;

    public float Radius => radius;

    private void Awake()
    {
        FindPlayer();

        CreateFillCircle();
        CreateWarningCircle();
    }

    private void Start()
    {
        StartCoroutine(
            HazardRoutine()
        );
    }

    // =====================================================
    // PLAYER
    // =====================================================

    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject == null)
        {
            Debug.LogWarning(
                "[HazardZone] Player를 찾을 수 없습니다."
            );

            return;
        }

        player =
            playerObject.transform;

        playerHealth =
            playerObject.GetComponent<PlayerHealth>();

        if (playerHealth == null)
        {
            playerHealth =
                playerObject.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            playerHealth =
                playerObject.GetComponentInChildren<PlayerHealth>();
        }
    }

    // =====================================================
    // WARNING FILL
    // =====================================================

    private void CreateFillCircle()
    {
        fillObject =
            new GameObject("HazardFill");

        fillObject.transform.SetParent(
            transform,
            false
        );

        fillObject.transform.localPosition =
            new Vector3(
                0f,
                visualHeight,
                0f
            );

        MeshFilter meshFilter =
            fillObject.AddComponent<MeshFilter>();

        fillRenderer =
            fillObject.AddComponent<MeshRenderer>();

        Mesh mesh =
            new Mesh();

        int segments =
            Mathf.Max(
                8,
                circleSegments
            );

        Vector3[] vertices =
            new Vector3[
                segments + 1
            ];

        int[] triangles =
            new int[
                segments * 3
            ];

        vertices[0] =
            Vector3.zero;

        for (
            int i = 0;
            i < segments;
            i++
        )
        {
            float angle =
                (float)i /
                segments *
                Mathf.PI *
                2f;

            vertices[i + 1] =
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );
        }

        for (
            int i = 0;
            i < segments;
            i++
        )
        {
            int current =
                i + 1;

            int next =
                ((i + 1) % segments) + 1;

            triangles[i * 3] =
                0;

            triangles[i * 3 + 1] =
                next;

            triangles[i * 3 + 2] =
                current;
        }

        mesh.vertices =
            vertices;

        mesh.triangles =
            triangles;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh =
            mesh;

        Shader shader =
            Shader.Find(
                "Universal Render Pipeline/Unlit"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Sprites/Default"
                );
        }

        if (shader == null)
        {
            Debug.LogError(
                "[HazardZone] Fill Shader를 찾지 못했습니다."
            );

            return;
        }

        fillMaterial =
            new Material(shader);

        fillMaterial.name =
            "Runtime_HazardFill";

        Color fillColor =
            warningColor;

        fillColor.a =
            fillAlpha;

        if (
            fillMaterial.HasProperty(
                "_BaseColor"
            )
        )
        {
            fillMaterial.SetColor(
                "_BaseColor",
                fillColor
            );
        }

        if (
            fillMaterial.HasProperty(
                "_Color"
            )
        )
        {
            fillMaterial.SetColor(
                "_Color",
                fillColor
            );
        }

        fillMaterial.SetFloat(
            "_Surface",
            1f
        );

        fillMaterial.SetInt(
            "_SrcBlend",
            (int)BlendMode.SrcAlpha
        );

        fillMaterial.SetInt(
            "_DstBlend",
            (int)BlendMode.OneMinusSrcAlpha
        );

        fillMaterial.SetInt(
            "_ZWrite",
            0
        );

        fillMaterial.renderQueue =
            3000;

        fillRenderer.material =
            fillMaterial;

        fillRenderer.shadowCastingMode =
            ShadowCastingMode.Off;

        fillRenderer.receiveShadows =
            false;
    }

    // =====================================================
    // WARNING OUTLINE
    // =====================================================

    private void CreateWarningCircle()
    {
        GameObject circleObject =
            new GameObject(
                "WarningCircle"
            );

        circleObject.transform.SetParent(
            transform,
            false
        );

        warningCircle =
            circleObject.AddComponent<LineRenderer>();

        Shader shader =
            Shader.Find(
                "Sprites/Default"
            );

        if (shader == null)
        {
            shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );
        }

        if (shader == null)
            return;

        lineMaterial =
            new Material(shader);

        warningCircle.material =
            lineMaterial;

        warningCircle.useWorldSpace =
            false;

        warningCircle.loop =
            true;

        warningCircle.positionCount =
            circleSegments;

        warningCircle.startWidth =
            lineWidth;

        warningCircle.endWidth =
            lineWidth;

        warningCircle.numCapVertices =
            4;

        warningCircle.shadowCastingMode =
            ShadowCastingMode.Off;

        warningCircle.receiveShadows =
            false;

        for (
            int i = 0;
            i < circleSegments;
            i++
        )
        {
            float angle =
                (float)i /
                circleSegments *
                Mathf.PI *
                2f;

            Vector3 position =
                new Vector3(
                    Mathf.Cos(angle) * radius,
                    visualHeight + 0.01f,
                    Mathf.Sin(angle) * radius
                );

            warningCircle.SetPosition(
                i,
                position
            );
        }
    }

    // =====================================================
    // MAIN ROUTINE
    // =====================================================

    private IEnumerator HazardRoutine()
    {
        float elapsed =
            0f;

        while (
            elapsed <
            warningDuration
        )
        {
            UpdateWarningVisual(
                elapsed
            );

            elapsed +=
                Time.deltaTime;

            yield return null;
        }

        // 전조 제거
        HideWarning();

        // 폭발 이펙트
        SpawnExplosion();

        // 데미지 판정
        ApplyDamage();

        // HazardZone 자체는 바로 제거
        Destroy(
            gameObject
        );
    }

    // =====================================================
    // WARNING EFFECT
    // =====================================================

    private void UpdateWarningVisual(
        float elapsed)
    {
        float progress =
            Mathf.Clamp01(
                elapsed /
                warningDuration
            );

        float pulse =
            Mathf.PingPong(
                Time.time *
                pulseSpeed,
                1f
            );

        Color color =
            Color.Lerp(
                warningColor,
                dangerColor,
                progress
            );

        if (
            warningCircle != null
        )
        {
            Color outlineColor =
                color;

            outlineColor.a =
                Mathf.Lerp(
                    0.45f,
                    1f,
                    pulse
                );

            warningCircle.startColor =
                outlineColor;

            warningCircle.endColor =
                outlineColor;

            float width =
                Mathf.Lerp(
                    lineWidth,
                    lineWidth * 1.8f,
                    progress
                );

            warningCircle.startWidth =
                width;

            warningCircle.endWidth =
                width;
        }

        if (
            fillMaterial != null
        )
        {
            Color fillColor =
                color;

            fillColor.a =
                Mathf.Lerp(
                    fillAlpha * 0.5f,
                    fillAlpha,
                    pulse
                );

            SetMaterialColor(
                fillMaterial,
                fillColor
            );
        }
    }

    // =====================================================
    // REMOVE WARNING
    // =====================================================

    private void HideWarning()
    {
        if (
            warningCircle != null
        )
        {
            warningCircle.enabled =
                false;
        }

        if (
            fillObject != null
        )
        {
            fillObject.SetActive(
                false
            );
        }
    }

    // =====================================================
    // EXPLOSION
    // =====================================================

    private void SpawnExplosion()
    {
        if (
            explosionPrefab == null
        )
        {
            Debug.LogWarning(
                "[HazardZone] Explosion Prefab이 연결되지 않았습니다."
            );

            return;
        }

        Vector3 explosionPosition =
            transform.position +
            Vector3.up *
            explosionHeightOffset;

        GameObject explosion =
            Instantiate(
                explosionPrefab,
                explosionPosition,
                Quaternion.identity
            );

        explosion.transform.localScale =
            Vector3.one *
            explosionScale;

        Destroy(
            explosion,
            explosionLifetime
        );
    }

    // =====================================================
    // DAMAGE
    // =====================================================

    private void ApplyDamage()
    {
        if (exploded)
            return;

        exploded =
            true;

        if (
            player == null
        )
        {
            FindPlayer();
        }

        if (
            player == null
        )
            return;

        Vector3 playerPosition =
            player.position;

        Vector3 hazardPosition =
            transform.position;

        playerPosition.y =
            0f;

        hazardPosition.y =
            0f;

        float distance =
            Vector3.Distance(
                playerPosition,
                hazardPosition
            );

        if (
            distance <=
            radius
        )
        {
            if (
                playerHealth == null
            )
            {
                playerHealth =
                    player.GetComponent<PlayerHealth>();

                if (
                    playerHealth == null
                )
                {
                    playerHealth =
                        player.GetComponentInParent<PlayerHealth>();
                }
            }

            if (
                playerHealth != null
            )
            {
                playerHealth.TakeDamage(
                    damage
                );

                Debug.Log(
                    "[HazardZone] Explosion Hit!"
                );
            }
        }
    }

    // =====================================================
    // MATERIAL COLOR
    // =====================================================

    private void SetMaterialColor(
        Material material,
        Color color)
    {
        if (
            material.HasProperty(
                "_BaseColor"
            )
        )
        {
            material.SetColor(
                "_BaseColor",
                color
            );
        }

        if (
            material.HasProperty(
                "_Color"
            )
        )
        {
            material.SetColor(
                "_Color",
                color
            );
        }
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    private void OnDestroy()
    {
        if (
            lineMaterial != null
        )
        {
            Destroy(
                lineMaterial
            );
        }

        if (
            fillMaterial != null
        )
        {
            Destroy(
                fillMaterial
            );
        }
    }
}