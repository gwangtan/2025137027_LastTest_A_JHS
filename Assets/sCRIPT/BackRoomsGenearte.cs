using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class FinalSceneShuffle : MonoBehaviour
{
    // === 인스펙터 설정 ===
    // 씬에 있는 9개의 맵 조각 인스턴스를 순서대로 할당해야 합니다. (0번부터 8번까지)
    public GameObject[] allPrefabs = new GameObject[9];

    // 고정할 프리팹의 인덱스 (조각 2, 6, 8)
    private readonly int[] fixedIndices = { 2, 6, 8 };

    // 이 스크립트를 Start() 대신 Awake()에서 실행하여, 다른 스크립트보다 먼저 배치되도록 할 수 있습니다.
    void Start()
    {
        if (allPrefabs.Length != 9 || allPrefabs.Any(p => p == null))
        {
            Debug.LogError("9개의 맵 오브젝트를 인스펙터의 'All Prefabs' 배열에 순서대로 모두 할당해주세요.");
            return;
        }

        ShuffleMapsBasedOnScenePositions();
    }

    /// <summary>
    /// Fisher-Yates 셔플 알고리즘
    /// </summary>
    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }

    void ShuffleMapsBasedOnScenePositions()
    {
        // 1. 모든 프리팹의 현재(초기) 위치를 가져와 셔플될 위치 풀(Pool)로 사용합니다.
        // Y 좌표는 0.0f로 강제 고정하여 안정성을 높입니다.
        Vector3[] initialPositionsPool = new Vector3[allPrefabs.Length];
        for (int i = 0; i < allPrefabs.Length; i++)
        {
            Vector3 rawPos = allPrefabs[i].transform.position;
            // 씬에 배치된 X, Z 위치는 그대로 사용하고, Y만 0으로 고정
            initialPositionsPool[i] = new Vector3(rawPos.x, 0.0f, rawPos.z);
        }

        // 2. 셔플 대상 위치/프리팹 추출
        List<Vector3> positionsToShuffle = new List<Vector3>(); // 섞일 위치 (6개)
        List<GameObject> prefabsToShuffle = new List<GameObject>(); // 섞일 프리팹 (6개)

        for (int i = 0; i < allPrefabs.Length; i++)
        {
            GameObject currentPrefab = allPrefabs[i];
            Vector3 currentPosition = initialPositionsPool[i];

            if (fixedIndices.Contains(i))
            {
                // 고정된 조각 (2, 6, 8)은 그 위치에 그대로 둡니다.
                currentPrefab.transform.position = currentPosition;
            }
            else
            {
                // 셔플 대상 리스트 채우기
                positionsToShuffle.Add(currentPosition);
                prefabsToShuffle.Add(currentPrefab);
            }
        }

        // 3. 셔플 대상 위치 목록을 무작위로 섞음
        Shuffle(positionsToShuffle);

        // 4. 셔플된 조각들을 섞인 위치에 재배치
        for (int i = 0; i < prefabsToShuffle.Count; i++)
        {
            GameObject prefab = prefabsToShuffle[i]; // 조각 0, 1, 3, 4, 5, 7
            Vector3 newPosition = positionsToShuffle[i]; // 섞인 위치

            // 섞인 위치로 이동
            prefab.transform.position = newPosition;
        }

        Debug.Log("맵 조각 셔플 완료: 2, 6, 8번을 제외한 조각들의 위치가 씬의 초기 위치를 기반으로 섞였습니다.");
    }
}