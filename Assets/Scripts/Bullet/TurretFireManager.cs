using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretFireManager : MonoBehaviour
{
    [System.Serializable]
    public class DifficultySettings
    {
        [Header("Fire Count")]
        public int minFireCount = 1;
        public int maxFireCount = 2;

        [Header("Fire Interval")]
        public float minInterval = 3.5f;
        public float maxInterval = 5f;
    }

    [Header("References")]
    [SerializeField]
    private List<BulletSpawner> spawners =
        new List<BulletSpawner>();

    [Header("Level Difficulty")]
    [SerializeField]
    private List<DifficultySettings> levelDifficulties =
        new List<DifficultySettings>()
        {
            new DifficultySettings
            {
                minFireCount = 1,
                maxFireCount = 2,
                minInterval = 3.5f,
                maxInterval = 5f
            },

            new DifficultySettings
            {
                minFireCount = 2,
                maxFireCount = 2,
                minInterval = 2.5f,
                maxInterval = 4f
            },

            new DifficultySettings
            {
                minFireCount = 2,
                maxFireCount = 3,
                minInterval = 1.8f,
                maxInterval = 3f
            }
        };

    [Header("Telegraph")]
    [SerializeField]
    private float telegraphDuration = 0.8f;

    [Header("Startup")]
    [SerializeField]
    private float startDelay = 1f;

    private int currentMinFireCount = 1;
    private int currentMaxFireCount = 2;

    private float currentMinInterval = 3.5f;
    private float currentMaxInterval = 5f;

    private Coroutine fireRoutine;

    private bool initialized;

    // =====================================================
    // UNITY
    // =====================================================

    private void Start()
    {
        InitializeSpawners();

        initialized = true;

        ApplyLevelDifficulty(1);

        StartFireRoutine();
    }

    // =====================================================
    // INITIALIZE
    // =====================================================

    private void InitializeSpawners()
    {
        if (spawners.Count == 0)
        {
            BulletSpawner[] found =
                FindObjectsByType<BulletSpawner>(
                    FindObjectsSortMode.None
                );

            spawners.AddRange(found);
        }

        foreach (
            BulletSpawner spawner
            in spawners
        )
        {
            if (spawner == null)
                continue;

            spawner.SetAutoFire(
                false
            );
        }

        Debug.Log(
            $"[TurretFireManager] " +
            $"BulletSpawner {spawners.Count}개 등록"
        );
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
                "[TurretFireManager] " +
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

        currentMinFireCount =
            Mathf.Max(
                1,
                setting.minFireCount
            );

        currentMaxFireCount =
            Mathf.Max(
                currentMinFireCount,
                setting.maxFireCount
            );

        currentMinInterval =
            Mathf.Max(
                0.1f,
                setting.minInterval
            );

        currentMaxInterval =
            Mathf.Max(
                currentMinInterval,
                setting.maxInterval
            );

        Debug.Log(
            $"[TurretFireManager] LEVEL {level} 적용 / " +
            $"Turret {currentMinFireCount}~{currentMaxFireCount} / " +
            $"Interval {currentMinInterval}~{currentMaxInterval}s"
        );
    }

    // =====================================================
    // FIRE ROUTINE
    // =====================================================

    private void StartFireRoutine()
    {
        if (fireRoutine != null)
        {
            StopCoroutine(
                fireRoutine
            );
        }

        fireRoutine =
            StartCoroutine(
                FireRoutine()
            );
    }

    private IEnumerator FireRoutine()
    {
        yield return new WaitForSeconds(
            startDelay
        );

        while (true)
        {
            FireRandomTurrets();

            float waitTime =
                Random.Range(
                    currentMinInterval,
                    currentMaxInterval
                );

            yield return new WaitForSeconds(
                waitTime
            );
        }
    }

    // =====================================================
    // RANDOM FIRE
    // =====================================================

    private void FireRandomTurrets()
    {
        List<BulletSpawner> available =
            new List<BulletSpawner>();

        foreach (
            BulletSpawner spawner
            in spawners
        )
        {
            if (
                spawner != null &&
                spawner.gameObject.activeInHierarchy &&
                !spawner.IsBusy
            )
            {
                available.Add(
                    spawner
                );
            }
        }

        if (available.Count == 0)
            return;

        int minimum =
            Mathf.Clamp(
                currentMinFireCount,
                1,
                available.Count
            );

        int maximum =
            Mathf.Clamp(
                currentMaxFireCount,
                minimum,
                available.Count
            );

        int fireCount =
            Random.Range(
                minimum,
                maximum + 1
            );

        Shuffle(
            available
        );

        for (
            int i = 0;
            i < fireCount;
            i++
        )
        {
            available[i]
                .FireWithTelegraph(
                    telegraphDuration
                );
        }
    }

    // =====================================================
    // SHUFFLE
    // =====================================================

    private void Shuffle(
        List<BulletSpawner> list)
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

            BulletSpawner temp =
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
        if (fireRoutine != null)
        {
            StopCoroutine(
                fireRoutine
            );
        }
    }
}