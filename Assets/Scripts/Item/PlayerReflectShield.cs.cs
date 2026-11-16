using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerReflectShield : MonoBehaviour
{
    [Header("Reflect Settings")]
    [SerializeField]
    private float reflectRadius = 2.5f;

    [SerializeField]
    private float defaultDuration = 5f;

    [Tooltip("반사된 탄환이 바로 다시 반사되는 것을 막는 최소 시간")]
    [SerializeField]
    private float reflectCooldown = 0.15f;

    [Header("Visual")]
    [SerializeField]
    private bool showShieldVisual = true;

    [SerializeField]
    private Color shieldColor =
        new Color(
            0.15f,
            0.85f,
            1f,
            0.28f
        );

    private bool shieldActive;

    private Coroutine shieldRoutine;

    private GameObject shieldVisual;

    private readonly Dictionary<int, float>
        reflectedBullets =
            new Dictionary<int, float>();

    public bool IsActive =>
        shieldActive;

    // =====================================================
    // UNITY
    // =====================================================

    private void Update()
    {
        if (!shieldActive)
            return;

        ReflectNearbyBullets();

        CleanupCooldownList();
    }

    // =====================================================
    // ACTIVATE
    // =====================================================

    public void ActivateShield(
        float duration)
    {
        duration =
            duration > 0f
                ? duration
                : defaultDuration;

        if (shieldRoutine != null)
        {
            StopCoroutine(
                shieldRoutine
            );
        }

        shieldRoutine =
            StartCoroutine(
                ShieldRoutine(
                    duration
                )
            );
    }

    private IEnumerator ShieldRoutine(
        float duration)
    {
        shieldActive =
            true;

        CreateShieldVisual();

        Debug.Log(
            $"[ITEM] REFLECT SHIELD ON / {duration}s"
        );

        yield return new WaitForSeconds(
            duration
        );

        shieldActive =
            false;

        RemoveShieldVisual();

        reflectedBullets.Clear();

        shieldRoutine =
            null;

        Debug.Log(
            "[ITEM] REFLECT SHIELD OFF"
        );
    }

    // =====================================================
    // REFLECT
    // =====================================================

    private void ReflectNearbyBullets()
    {
        Bullet[] bullets =
            FindObjectsByType<Bullet>(
                FindObjectsSortMode.None
            );

        Vector3 playerPosition =
            transform.position;

        playerPosition.y =
            0f;

        for (
            int i = 0;
            i < bullets.Length;
            i++
        )
        {
            Bullet bullet =
                bullets[i];

            if (bullet == null)
                continue;

            int id =
                bullet.GetInstanceID();

            if (
                reflectedBullets.ContainsKey(
                    id
                )
            )
            {
                continue;
            }

            Vector3 bulletPosition =
                bullet.transform.position;

            bulletPosition.y =
                0f;

            float distance =
                Vector3.Distance(
                    playerPosition,
                    bulletPosition
                );

            if (
                distance >
                reflectRadius
            )
            {
                continue;
            }

            Vector3 direction =
                bullet.transform.position -
                transform.position;

            direction.y =
                0f;

            if (
                direction.sqrMagnitude <=
                0.001f
            )
            {
                direction =
                    transform.forward;
            }

            direction.Normalize();

            bullet.Initialize(
                direction
            );

            reflectedBullets[id] =
                Time.time;

            Debug.Log(
                "[ReflectShield] Bullet Reflected"
            );
        }
    }

    // =====================================================
    // COOLDOWN
    // =====================================================

    private void CleanupCooldownList()
    {
        if (
            reflectedBullets.Count == 0
        )
        {
            return;
        }

        List<int> removeList =
            new List<int>();

        foreach (
            KeyValuePair<int, float> pair
            in reflectedBullets
        )
        {
            if (
                Time.time -
                pair.Value >=
                reflectCooldown
            )
            {
                removeList.Add(
                    pair.Key
                );
            }
        }

        for (
            int i = 0;
            i < removeList.Count;
            i++
        )
        {
            reflectedBullets.Remove(
                removeList[i]
            );
        }
    }

    // =====================================================
    // VISUAL
    // =====================================================

    private void CreateShieldVisual()
    {
        if (!showShieldVisual)
            return;

        if (shieldVisual != null)
        {
            Destroy(
                shieldVisual
            );
        }

        shieldVisual =
            GameObject.CreatePrimitive(
                PrimitiveType.Sphere
            );

        shieldVisual.name =
            "ReflectShieldVisual";

        shieldVisual.transform.SetParent(
            transform,
            false
        );

        shieldVisual.transform.localPosition =
            Vector3.up * 1f;

        shieldVisual.transform.localScale =
            Vector3.one *
            reflectRadius *
            2f;

        Collider collider =
            shieldVisual.GetComponent<Collider>();

        if (collider != null)
        {
            Destroy(
                collider
            );
        }

        Renderer renderer =
            shieldVisual.GetComponent<Renderer>();

        if (renderer == null)
            return;

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
            return;

        Material material =
            new Material(
                shader
            );

        material.name =
            "Runtime_ReflectShield";

        if (
            material.HasProperty(
                "_BaseColor"
            )
        )
        {
            material.SetColor(
                "_BaseColor",
                shieldColor
            );

            material.SetFloat(
                "_Surface",
                1f
            );

            material.SetFloat(
                "_ZWrite",
                0f
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
                shieldColor
            );
        }

        renderer.material =
            material;
    }

    private void RemoveShieldVisual()
    {
        if (shieldVisual != null)
        {
            Destroy(
                shieldVisual
            );

            shieldVisual =
                null;
        }
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
            reflectRadius
        );
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    private void OnDisable()
    {
        shieldActive =
            false;

        RemoveShieldVisual();

        reflectedBullets.Clear();
    }
}