using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurretFireManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("관리할 BulletSpawner들을 등록하세요.")]
    [SerializeField]
    private List<BulletSpawner> spawners =
        new List<BulletSpawner>();

    [Header("Wave Settings")]
    [Tooltip("한 번에 최소 몇 개의 터렛이 발사할지")]
    [SerializeField] private int minFireCount = 2;

    [Tooltip("한 번에 최대 몇 개의 터렛이 발사할지")]
    [SerializeField] private int maxFireCount = 3;

    [Tooltip("다음 발사까지 최소 대기시간")]
    [SerializeField] private float minInterval = 0.8f;

    [Tooltip("다음 발사까지 최대 대기시간")]
    [SerializeField] private float maxInterval = 1.5f;

    [Header("Startup")]
    [Tooltip("게임 시작 후 첫 발사까지 대기시간")]
    [SerializeField] private float startDelay = 1f;

    private Coroutine fireRoutine;


    private void Start()
    {
        // Inspector 등록을 안 했다면 자동 검색
        if (spawners.Count == 0)
        {
            BulletSpawner[] found =
                FindObjectsByType<BulletSpawner>(
                    FindObjectsSortMode.None
                );

            spawners.AddRange(found);
        }

        // 모든 개별 자동 발사 OFF
        foreach (BulletSpawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.SetAutoFire(false);
            }
        }

        fireRoutine =
            StartCoroutine(FireRoutine());
    }


    private IEnumerator FireRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            FireRandomTurrets();

            float waitTime =
                Random.Range(
                    minInterval,
                    maxInterval
                );

            yield return new WaitForSeconds(waitTime);
        }
    }


    private void FireRandomTurrets()
    {
        List<BulletSpawner> available =
            new List<BulletSpawner>();


        // null 제거
        foreach (BulletSpawner spawner in spawners)
        {
            if (spawner != null &&
                spawner.gameObject.activeInHierarchy)
            {
                available.Add(spawner);
            }
        }


        if (available.Count == 0)
        {
            return;
        }


        // 최소/최대 발사 개수 보정
        int minimum =
            Mathf.Clamp(
                minFireCount,
                1,
                available.Count
            );


        int maximum =
            Mathf.Clamp(
                maxFireCount,
                minimum,
                available.Count
            );


        int fireCount =
            Random.Range(
                minimum,
                maximum + 1
            );


        // 리스트 섞기
        Shuffle(available);


        // 앞에서 fireCount개 발사
        for (int i = 0; i < fireCount; i++)
        {
            available[i].FireOnce();
        }
    }


    private void Shuffle(
        List<BulletSpawner> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
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


    private void OnDestroy()
    {
        if (fireRoutine != null)
        {
            StopCoroutine(fireRoutine);
        }
    }
}