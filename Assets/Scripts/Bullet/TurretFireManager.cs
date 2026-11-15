using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretFireManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("관리할 BulletSpawner들을 등록하세요. 비워두면 자동 검색합니다.")]
    [SerializeField]
    private List<BulletSpawner> spawners = new List<BulletSpawner>();

    [Header("Wave Settings")]
    [Tooltip("한 웨이브에서 발사할 터렛 최소 개수")]
    [Min(1)]
    [SerializeField] private int minFireCount = 1;

    [Tooltip("한 웨이브에서 발사할 터렛 최대 개수")]
    [Min(1)]
    [SerializeField] private int maxFireCount = 2;

    [Tooltip("다음 웨이브까지 최소 대기시간")]
    [Min(0.1f)]
    [SerializeField] private float minInterval = 2.5f;

    [Tooltip("다음 웨이브까지 최대 대기시간")]
    [Min(0.1f)]
    [SerializeField] private float maxInterval = 4f;

    [Header("Telegraph Settings")]
    [Tooltip("탄환 발사 전에 전조를 보여주는 시간")]
    [Min(0f)]
    [SerializeField] private float telegraphDuration = 0.8f;

    [Header("Startup")]
    [Tooltip("게임 시작 후 첫 웨이브까지 대기시간")]
    [Min(0f)]
    [SerializeField] private float startDelay = 1f;

    private Coroutine fireRoutine;
    private bool isRunning;
    private bool hasInitialized;

    private void Start()
    {
        InitializeSpawners();
        hasInitialized = true;
        StartFireRoutine();
    }

    private void InitializeSpawners()
    {
        if (spawners == null)
            spawners = new List<BulletSpawner>();

        if (spawners.Count == 0)
        {
            BulletSpawner[] found =
                FindObjectsByType<BulletSpawner>(
                    FindObjectsSortMode.None
                );

            spawners.AddRange(found);
        }

        // TurretFireManager가 전체 발사 타이밍을 관리하므로
        // 각 BulletSpawner의 개별 자동 발사는 꺼둡니다.
        foreach (BulletSpawner spawner in spawners)
        {
            if (spawner != null)
                spawner.SetAutoFire(false);
        }

        Debug.Log(
            $"[TurretFireManager] 등록된 터렛 스포너: {spawners.Count}"
        );
    }

    private void StartFireRoutine()
    {
        if (isRunning)
            return;

        isRunning = true;
        fireRoutine = StartCoroutine(FireRoutine());
    }

    private IEnumerator FireRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        while (isRunning)
        {
            FireRandomTurrets();

            float min = Mathf.Max(0.1f, minInterval);
            float max = Mathf.Max(min, maxInterval);
            float waitTime = Random.Range(min, max);

            yield return new WaitForSeconds(waitTime);
        }

        fireRoutine = null;
    }

    private void FireRandomTurrets()
    {
        List<BulletSpawner> available = new List<BulletSpawner>();

        foreach (BulletSpawner spawner in spawners)
        {
            if (spawner != null &&
                spawner.gameObject.activeInHierarchy &&
                !spawner.IsBusy)
            {
                available.Add(spawner);
            }
        }

        if (available.Count == 0)
        {
            Debug.Log("[TurretFireManager] 발사 가능한 터렛이 없습니다.");
            return;
        }

        int minimum = Mathf.Clamp(
            minFireCount,
            1,
            available.Count
        );

        int maximum = Mathf.Clamp(
            maxFireCount,
            minimum,
            available.Count
        );

        int fireCount = Random.Range(minimum, maximum + 1);

        Shuffle(available);

        for (int i = 0; i < fireCount; i++)
        {
            available[i].FireWithTelegraph(telegraphDuration);
        }

        Debug.Log(
            $"[TurretFireManager] 이번 웨이브 발사 터렛: {fireCount}개"
        );
    }

    private void Shuffle(List<BulletSpawner> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            BulletSpawner temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    public void SetSpawnerList(List<BulletSpawner> newSpawners)
    {
        spawners = newSpawners ?? new List<BulletSpawner>();

        foreach (BulletSpawner spawner in spawners)
        {
            if (spawner != null)
                spawner.SetAutoFire(false);
        }
    }

    private void OnDisable()
    {
        isRunning = false;

        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
            fireRoutine = null;
        }
    }

    private void OnEnable()
    {
        // Start 이전의 최초 OnEnable에서는 실행하지 않습니다.
        // Start 이후 비활성화/활성화되는 경우에만 재시작합니다.
        if (hasInitialized && !isRunning)
            StartFireRoutine();
    }

    private void OnDestroy()
    {
        isRunning = false;

        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
            fireRoutine = null;
        }
    }
}
