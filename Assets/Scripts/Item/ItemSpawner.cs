using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [Header("Spawn Point Settings")]
    [SerializeField] private Transform spawnPointRoot;

    [Header("Item Prefabs")]
    [SerializeField] private GameObject speedItemPrefab;
    [SerializeField] private GameObject healItemPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float minSpawnInterval = 8f;
    [SerializeField] private float maxSpawnInterval = 15f;

    [SerializeField] private int maxActiveItems = 2;

    [Tooltip("SpawnPoint 위치에서 위로 얼마나 띄울지")]
    [SerializeField] private float spawnHeightOffset = 1f;

    [Header("Spawn Chance")]
    [Range(0f, 1f)]
    [SerializeField] private float speedItemChance = 0.5f;

    private readonly List<Transform> spawnPoints =
        new List<Transform>();

    private readonly List<GameObject> activeItems =
        new List<GameObject>();


    private void Start()
    {
        RegisterSpawnPoints();

        StartCoroutine(
            SpawnRoutine()
        );
    }


    private void RegisterSpawnPoints()
    {
        spawnPoints.Clear();

        if (spawnPointRoot == null)
        {
            Debug.LogError(
                "[ItemSpawner] Spawn Point Root가 연결되지 않았습니다."
            );

            return;
        }

        for (int i = 0;
             i < spawnPointRoot.childCount;
             i++)
        {
            Transform point =
                spawnPointRoot.GetChild(i);

            if (point != null)
            {
                spawnPoints.Add(point);
            }
        }

        Debug.Log(
            $"[ItemSpawner] Spawn Point {spawnPoints.Count}개 등록"
        );
    }


    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            float waitTime =
                Random.Range(
                    minSpawnInterval,
                    maxSpawnInterval
                );

            yield return new WaitForSeconds(
                waitTime
            );

            RemoveDestroyedItems();

            if (activeItems.Count <
                maxActiveItems)
            {
                SpawnRandomItem();
            }
        }
    }


    private void SpawnRandomItem()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning(
                "[ItemSpawner] 사용할 Spawn Point가 없습니다."
            );

            return;
        }

        if (speedItemPrefab == null ||
            healItemPrefab == null)
        {
            Debug.LogWarning(
                "[ItemSpawner] Item Prefab이 연결되지 않았습니다."
            );

            return;
        }

        Transform spawnPoint =
            GetRandomSpawnPoint();

        if (spawnPoint == null)
            return;

        GameObject selectedPrefab;

        if (Random.value <= speedItemChance)
        {
            selectedPrefab =
                speedItemPrefab;
        }
        else
        {
            selectedPrefab =
                healItemPrefab;
        }

        Vector3 spawnPosition =
            spawnPoint.position +
            Vector3.up * spawnHeightOffset;

        GameObject item =
            Instantiate(
                selectedPrefab,
                spawnPosition,
                spawnPoint.rotation
            );

        activeItems.Add(item);

        Debug.Log(
            $"[ItemSpawner] ITEM SPAWN : {selectedPrefab.name} " +
            $"at {spawnPoint.name}"
        );
    }


    private Transform GetRandomSpawnPoint()
    {
        int index =
            Random.Range(
                0,
                spawnPoints.Count
            );

        return spawnPoints[index];
    }


    private void RemoveDestroyedItems()
    {
        activeItems.RemoveAll(
            item => item == null
        );
    }
}