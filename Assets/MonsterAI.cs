using UnityEngine;
using UnityEngine.AI;

public class MonsterAI : MonoBehaviour
{
    // ====== AI 상태 정의 ======
    public enum AIState { Patrol, Chase }
    public AIState currentState = AIState.Patrol;

    // ====== Inspector에서 설정 가능한 변수 ======
    [Header("Movement Settings")]
    public float walkSpeed = 2.0f; // 순찰 상태의 속도
    public float runSpeed = 5.0f;  // 추적 상태의 속도

    [Header("Detection Settings")]
    public float sightRange = 15.0f;     // 플레이어를 감지하는 시야 범위
    public float loseTargetTime = 3.0f;  // 플레이어를 놓쳤을 때 추적을 포기할 시간 (요청 사항)
    public LayerMask obstacleMask;       // 벽과 같은 장애물 레이어 (Inspector에서 설정)

    [Header("Patrol Settings")]
    public float patrolRange = 20.0f; // 랜덤 순찰 목표 지점의 최대 반경

    // ====== 내부 변수 ======
    private NavMeshAgent agent;
    private Animator anim;
    private Transform player;

    private float targetLostTimer;
    private Vector3 lastKnownPlayerPosition; // 플레이어를 마지막으로 본 위치

    private void Start()
    {
        // 필수 컴포넌트 가져오기
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        // 플레이어 오브젝트 찾기 (태그 사용)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player 오브젝트에 'Player' 태그가 지정되지 않았습니다.");
        }

        targetLostTimer = loseTargetTime;
        SetState(AIState.Patrol); // 시작은 순찰 상태
    }

    private void Update()
    {
        if (player == null) return;

        // 매 프레임 플레이어 감지 여부를 확인
        bool isPlayerInSight = CheckForPlayer();

        // 상태 기계 업데이트
        switch (currentState)
        {
            case AIState.Patrol:
                PatrolState(isPlayerInSight);
                break;
            case AIState.Chase:
                ChaseState(isPlayerInSight);
                break;
        }
    }

    /// <summary>
    /// AI 상태를 설정하고, 해당 상태에 맞는 초기 설정을 수행합니다.
    /// </summary>
    private void SetState(AIState newState)
    {
        currentState = newState;

        switch (currentState)
        {
            case AIState.Patrol:
                agent.speed = walkSpeed;
                anim.SetBool("IsRunning", false);
                anim.SetBool("IsWalking", true);
                SetNewPatrolPoint(); // 순찰 시작 시 목표 지점 설정
                break;

            case AIState.Chase:
                agent.speed = runSpeed;
                anim.SetBool("IsWalking", false);
                anim.SetBool("IsRunning", true);
                targetLostTimer = loseTargetTime; // 추적 시작 시 타이머 초기화
                break;
        }
    }

    // ====================================================================
    // 1. 플레이어 감지 로직
    // ====================================================================

    /// <summary>
    /// 플레이어가 시야 범위 내에 있고, 장애물에 가려지지 않았는지 확인합니다.
    /// </summary>
    bool CheckForPlayer()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 1. 시야 범위 확인
        if (distanceToPlayer <= sightRange)
        {
            // 2. Linecast를 사용하여 벽/장애물 확인
            // 몬스터의 눈높이에서 플레이어의 눈높이로 레이캐스트 (정확도를 위해 Y축을 조정할 수 있음)
            Vector3 startPos = transform.position + Vector3.up * 0.5f;
            Vector3 endPos = player.position + Vector3.up * 0.5f;

            RaycastHit hit;

            // obstacleMask 레이어만 충돌 검사
            if (Physics.Linecast(startPos, endPos, out hit, obstacleMask))
            {
                // 장애물에 맞았으므로, 플레이어가 보이지 않음
                return false;
            }
            else
            {
                // 장애물에 막히지 않았으므로, 플레이어가 보임
                lastKnownPlayerPosition = player.position;
                return true;
            }
        }
        return false;
    }

    // ====================================================================
    // 2. Patrol State (순찰 상태)
    // ====================================================================

    private void PatrolState(bool isPlayerInSight)
    {
        // 1. 감지: 플레이어가 보이면 추적 상태로 전환
        if (isPlayerInSight)
        {
            SetState(AIState.Chase);
            return;
        }

        // 2. 이동: 현재 목표 지점에 도착했으면 새 목표 설정
        // pathPending: 경로 계산 중인지 확인
        // remainingDistance: 목표 지점까지 남은 거리
        if (!agent.pathPending && agent.remainingDistance < 1.0f)
        {
            SetNewPatrolPoint();
        }
    }

    /// <summary>
    /// 현재 위치 주변의 랜덤한 내비게이션 가능한 지점을 찾습니다.
    /// </summary>
    private void SetNewPatrolPoint()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRange;
        randomDirection += transform.position;
        NavMeshHit hit;

        // NavMesh 상의 유효한 지점을 찾습니다. (patrolRange 내에서)
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRange, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // ====================================================================
    // 3. Chase State (추적 상태)
    // ====================================================================

    private void ChaseState(bool isPlayerInSight)
    {
        // 1. 계속 감지: 플레이어가 시야 내에 있는 경우
        if (isPlayerInSight)
        {
            agent.SetDestination(player.position); // 플레이어 위치로 이동
            targetLostTimer = loseTargetTime;     // 타이머 초기화
        }
        // 2. 놓침: 플레이어가 시야에서 사라진 경우
        else
        {
            // 마지막으로 본 위치로 이동을 계속 시도
            agent.SetDestination(lastKnownPlayerPosition);

            // 타이머 감소
            targetLostTimer -= Time.deltaTime;

            // 3초 이상 놓쳤거나, 마지막 위치에 도착한 경우
            if (targetLostTimer <= 0f || (!agent.pathPending && agent.remainingDistance < 1.0f))
            {
                SetState(AIState.Patrol); // 순찰 상태로 복귀
            }
        }
    }
}