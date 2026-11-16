using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HazardZoneManager : MonoBehaviour
{
    [System.Serializable]
    public class DifficultySettings
    {
        [Header("Spawn Count")]
        public int minSpawnCount = 1;
        public int maxSpawnCount = 1;

        [Header("Spawn Interval")]
        public float minSpawnInterval = 6f;
        public float maxSpawnInterval = 8f;
    }

    [Header("References")]
    [SerializeField]
    private HazardZone hazardZonePrefab;

    [SerializeField]
    private Transform spawnPointRoot;

    [Header("Level Difficulty")]
    [SerializeField]
    private List<DifficultySettings> levelDifficulties =
        new List<DifficultySettings>()
        {
            new DifficultySettings
            {
                minSpawnCount = 1,
                maxSpawnCount = 1,
                minSpawnInterval = 6f,
                maxSpawnInterval = 8f
            },

            new DifficultySettings
            {
                minSpawnCount = 1,
                maxSpawnCount = 2,
                minSpawnInterval = 4f,
                maxSpawnInterval = 6f
            },

            new DifficultySettings
            {
                minSpawnCount = 2,
                maxSpawnCount = 3,
                minSpawnInterval = 3f,
                maxSpawnInterval = 4.5f
            }
        };

    [Header("Startup")]
    [SerializeField]
    private float startDelay = 5f;

    [Header("Position")]
    [SerializeField]
    private float spawnHeightOffset = 0.2f;

    [Header("Player Safety")]
    [SerializeField]
    private float minimumPlayerDistance = 2f;

    private int currentMinSpawnCount = 1;
    private int currentMaxSpawnCount = 1;

    private float currentMinSpawnInterval = 6f;
    private float currentMaxSpawnInterval = 8f;

    private readonly List<Transform>
        spawnPoints =
            new List<Transform>();

    private Transform player;

    private Coroutine spawnRoutine;

    // =====================================================
    // UNITY
    // =====================================================

    private void Start()
    {
        FindPlayer();

        RegisterSpawnPoints();

        ApplyLevelDifficulty(
            1
        );

        StartSpawnRoutine();
    }

    // =====================================================
    // LEVEL DIFFICULTY
    // =====================================================

    public void ApplyLevelDifficulty(
        int level)
    {
        if (
            levelDifficulties == null ||
            levelDifficulties.Count == 0
        )
        {
            Debug.LogWarning(
                "[HazardZoneManager] " +
                "Level Difficulty 설정이 없습니다."
            );

            return;
        }

        int index =
            Mathf.Clamp(
                level - 1,
                0,
                levelDifficulties.Count - 1
            );

        DifficultySettings setting =
            levelDifficulties[index];

        currentMinSpawnCount =
            Mathf.Max(
                1,
                setting.minSpawnCount
            );

        currentMaxSpawnCount =
            Mathf.Max(
                currentMinSpawnCount,
                setting.maxSpawnCount
            );

        currentMinSpawnInterval =
            Mathf.Max(
                0.1f,
                setting.minSpawnInterval
            );

        currentMaxSpawnInterval =
            Mathf.Max(
                currentMinSpawnInterval,
                setting.maxSpawnInterval
            );

        Debug.Log(
            $"[HazardZoneManager] LEVEL {level} 적용 / " +
            $"Hazard {currentMinSpawnCount}~{currentMaxSpawnCount} / " +
            $"Interval {currentMinSpawnInterval}~{currentMaxSpawnInterval}s"
        );
    }

    // =====================================================
    // PLAYER
    // =====================================================

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

    // =====================================================
    // SPAWN POINT
    // =====================================================

    private void RegisterSpawnPoints()
    {
        spawnPoints.Clear();

        if (spawnPointRoot == null)
        {
            Debug.LogError(
                "[HazardZoneManager] " +
                "Spawn Point Root가 없습니다."
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
            $"[HazardZoneManager] " +
            $"SpawnPoint {spawnPoints.Count}개 등록"
        );
    }

    // =====================================================
    // ROUTINE
    // =====================================================

    private void StartSpawnRoutine()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(
                spawnRoutine
            );
        }

        spawnRoutine =
            StartCoroutine(
                SpawnRoutine()
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
                    currentMinSpawnInterval,
                    currentMaxSpawnInterval
                );

            yield return new WaitForSeconds(
                waitTime
            );
        }
    }

    // =====================================================
    // SPAWN WAVE
    // =====================================================

    private void SpawnWave()
    {
        if (hazardZonePrefab == null)
        {
            Debug.LogWarning(
                "[HazardZoneManager] " +
                "HazardZone Prefab이 없습니다."
            );

            return;
        }

        if (spawnPoints.Count == 0)
            return;

        List<Transform> available =
            new List<Transform>();

        for (
            int i = 0;
            i < spawnPoints.Count;
            i++
        )
        {
            Transform point =
                spawnPoints[i];

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

        if (available.Count == 0)
            return;

        Shuffle(
            available
        );

        int minimum =
            Mathf.Clamp(
                currentMinSpawnCount,
                1,
                available.Count
            );

        int maximum =
            Mathf.Clamp(
                currentMaxSpawnCount,
                minimum,
                available.Count
            );

        int spawnCount =
            Random.Range(
                minimum,
                maximum + 1
            );

        for (
            int i = 0;
            i < spawnCount;
            i++
        )
        {
            SpawnHazard(
                available[i]
            );
        }
    }

    // =====================================================
    // VALID POSITION
    // =====================================================

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

    // =====================================================
    // SPAWN
    // =====================================================

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

    // =====================================================
    // SHUFFLE
    // =====================================================

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

    // =====================================================
    // CLEANUP
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