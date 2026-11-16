using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Point Settings")]
    [SerializeField]
    private Transform spawnPointRoot;

    [Header("Item Prefabs")]
    [SerializeField]
    private GameObject speedItemPrefab;

    [SerializeField]
    private GameObject healItemPrefab;

    [SerializeField]
    private GameObject shieldItemPrefab;

    [Header("Spawn Settings")]
    [SerializeField]
    private float minSpawnInterval = 8f;

    [SerializeField]
    private float maxSpawnInterval = 15f;

    [SerializeField]
    private int maxActiveItems = 2;

    [SerializeField]
    private float spawnHeightOffset = 1f;

    [Header("Spawn Chance")]
    [Range(0f, 1f)]
    [SerializeField]
    private float speedItemChance = 0.4f;

    [Range(0f, 1f)]
    [SerializeField]
    private float healItemChance = 0.4f;

    [Range(0f, 1f)]
    [SerializeField]
    private float shieldItemChance = 0.2f;

    private readonly List<Transform>
        spawnPoints =
            new List<Transform>();

    private readonly List<GameObject>
        activeItems =
            new List<GameObject>();

    private Coroutine spawnRoutine;

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        RegisterSpawnPoints();

        spawnRoutine =
            StartCoroutine(
                SpawnRoutine()
            );
    }

    // =====================================================
    // SPAWN POINTS
    // =====================================================

    private void RegisterSpawnPoints()
    {
        spawnPoints.Clear();

        if (spawnPointRoot == null)
        {
            Debug.LogError(
                "[ItemSpawner] Spawn Point Root가 없습니다."
            );

            return;
        }

        for (
            int i = 0;
            i < spawnPointRoot.childCount;
            i++
        )
        {
            Transform point =
                spawnPointRoot.GetChild(i);

            if (point == null)
                continue;

            spawnPoints.Add(
                point
            );
        }

        Debug.Log(
            $"[ItemSpawner] SpawnPoint {spawnPoints.Count}개 등록"
        );
    }

    // =====================================================
    // SPAWN ROUTINE
    // =====================================================

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float waitTime =
                Random.Range(
                    Mathf.Max(
                        0.1f,
                        minSpawnInterval
                    ),
                    Mathf.Max(
                        minSpawnInterval,
                        maxSpawnInterval
                    )
                );

            yield return new WaitForSeconds(
                waitTime
            );

            CleanupActiveItems();

            if (
                activeItems.Count >=
                maxActiveItems
            )
            {
                continue;
            }

            SpawnRandomItem();
        }
    }

    // =====================================================
    // SPAWN
    // =====================================================

    private void SpawnRandomItem()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning(
                "[ItemSpawner] 사용할 Spawn Point가 없습니다."
            );

            return;
        }

        GameObject selectedPrefab =
            SelectRandomItemPrefab();

        if (selectedPrefab == null)
        {
            Debug.LogWarning(
                "[ItemSpawner] 생성 가능한 Item Prefab이 없습니다."
            );

            return;
        }

        Transform spawnPoint =
            spawnPoints[
                Random.Range(
                    0,
                    spawnPoints.Count
                )
            ];

        Vector3 spawnPosition =
            spawnPoint.position +
            Vector3.up *
            spawnHeightOffset;

        GameObject spawnedItem =
            Instantiate(
                selectedPrefab,
                spawnPosition,
                Quaternion.identity
            );

        activeItems.Add(
            spawnedItem
        );

        Debug.Log(
            $"[ItemSpawner] Spawn → {selectedPrefab.name}"
        );
    }

    // =====================================================
    // RANDOM ITEM
    // =====================================================

    private GameObject SelectRandomItemPrefab()
    {
        List<GameObject> prefabs =
            new List<GameObject>();

        List<float> weights =
            new List<float>();

        if (
            speedItemPrefab != null &&
            speedItemChance > 0f
        )
        {
            prefabs.Add(
                speedItemPrefab
            );

            weights.Add(
                speedItemChance
            );
        }

        if (
            healItemPrefab != null &&
            healItemChance > 0f
        )
        {
            prefabs.Add(
                healItemPrefab
            );

            weights.Add(
                healItemChance
            );
        }

        if (
            shieldItemPrefab != null &&
            shieldItemChance > 0f
        )
        {
            prefabs.Add(
                shieldItemPrefab
            );

            weights.Add(
                shieldItemChance
            );
        }

        if (prefabs.Count == 0)
            return null;

        float totalWeight =
            0f;

        for (
            int i = 0;
            i < weights.Count;
            i++
        )
        {
            totalWeight +=
                weights[i];
        }

        if (totalWeight <= 0f)
            return null;

        float randomValue =
            Random.Range(
                0f,
                totalWeight
            );

        float accumulated =
            0f;

        for (
            int i = 0;
            i < prefabs.Count;
            i++
        )
        {
            accumulated +=
                weights[i];

            if (
                randomValue <=
                accumulated
            )
            {
                return prefabs[i];
            }
        }

        return prefabs[
            prefabs.Count - 1
        ];
    }

    // =====================================================
    // CLEANUP
    // =====================================================

    private void CleanupActiveItems()
    {
        for (
            int i =
                activeItems.Count - 1;
            i >= 0;
            i--
        )
        {
            if (
                activeItems[i] ==
                null
            )
            {
                activeItems.RemoveAt(
                    i
                );
            }
        }
    }

    // =====================================================
    // DESTROY
    // =====================================================

    private void OnDestroy()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(
                spawnRoutine
            );
        }
    }
}