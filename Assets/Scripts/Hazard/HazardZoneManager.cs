using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HazardZoneManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private HazardZone hazardZonePrefab;

    [SerializeField]
    private Transform spawnPointRoot;

    [Header("Spawn Settings")]
    [SerializeField]
    private float startDelay = 5f;

    [SerializeField]
    private float minSpawnInterval = 4f;

    [SerializeField]
    private float maxSpawnInterval = 7f;

    [SerializeField]
    private int minSpawnCount = 1;

    [SerializeField]
    private int maxSpawnCount = 2;

    [Header("Position Settings")]
    [SerializeField]
    private float spawnHeightOffset = 0.08f;

    [Header("Player Safety")]
    [Tooltip("플레이어 바로 밑에 위험지역이 생성되는 것을 방지")]
    [SerializeField]
    private float minimumPlayerDistance = 2f;

    private readonly List<Transform>
        spawnPoints =
            new List<Transform>();

    private Transform player;

    private Coroutine spawnRoutine;


    private void Start()
    {
        FindPlayer();

        RegisterSpawnPoints();

        spawnRoutine =
            StartCoroutine(
                SpawnRoutine()
            );
    }


    private void FindPlayer()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag(
                "Player"
            );

        if (playerObject != null)
        {
            player =
                playerObject.transform;
        }
    }


    private void RegisterSpawnPoints()
    {
        spawnPoints.Clear();

        if (spawnPointRoot == null)
        {
            Debug.LogError(
                "[HazardZoneManager] Spawn Point Root가 연결되지 않았습니다."
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

            if (point != null)
            {
                spawnPoints.Add(
                    point
                );
            }
        }

        Debug.Log(
            $"[HazardZoneManager] SpawnPoint {spawnPoints.Count}개 등록"
        );
    }


    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(
            startDelay
        );

        while (true)
        {
            SpawnWave();

            float waitTime =
                Random.Range(
                    minSpawnInterval,
                    maxSpawnInterval
                );

            yield return new WaitForSeconds(
                waitTime
            );
        }
    }


    private void SpawnWave()
    {
        if (hazardZonePrefab == null)
        {
            Debug.LogWarning(
                "[HazardZoneManager] HazardZone Prefab이 없습니다."
            );

            return;
        }

        if (spawnPoints.Count == 0)
            return;

        int minimum =
            Mathf.Clamp(
                minSpawnCount,
                1,
                spawnPoints.Count
            );

        int maximum =
            Mathf.Clamp(
                maxSpawnCount,
                minimum,
                spawnPoints.Count
            );

        int spawnCount =
            Random.Range(
                minimum,
                maximum + 1
            );

        List<Transform> available =
            new List<Transform>();

        foreach (
            Transform point
            in spawnPoints
        )
        {
            if (
                IsValidSpawnPoint(
                    point
                )
            )
            {
                available.Add(
                    point
                );
            }
        }

        Shuffle(
            available
        );

        int actualCount =
            Mathf.Min(
                spawnCount,
                available.Count
            );

        for (
            int i = 0;
            i < actualCount;
            i++
        )
        {
            SpawnHazard(
                available[i]
            );
        }
    }


    private bool IsValidSpawnPoint(
        Transform point)
    {
        if (point == null)
            return false;

        if (player == null)
        {
            FindPlayer();

            if (player == null)
                return true;
        }

        Vector3 playerPosition =
            player.position;

        Vector3 spawnPosition =
            point.position;

        playerPosition.y = 0f;
        spawnPosition.y = 0f;

        float distance =
            Vector3.Distance(
                playerPosition,
                spawnPosition
            );

        return distance >=
            minimumPlayerDistance;
    }


    private void SpawnHazard(
        Transform point)
    {
        Vector3 spawnPosition =
            point.position +
            Vector3.up *
            spawnHeightOffset;

        Instantiate(
            hazardZonePrefab,
            spawnPosition,
            Quaternion.identity
        );
    }


    private void Shuffle(
        List<Transform> list)
    {
        for (
            int i =
                list.Count - 1;
            i > 0;
            i--
        )
        {
            int randomIndex =
                Random.Range(
                    0,
                    i + 1
                );

            Transform temp =
                list[i];

            list[i] =
                list[randomIndex];

            list[randomIndex] =
                temp;
        }
    }


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