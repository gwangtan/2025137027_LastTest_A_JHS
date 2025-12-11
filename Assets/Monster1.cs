using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    // --- 몬스터 상태 정의 ---
    private enum MonsterState
    {
        Walk,
        Run
    }
    private MonsterState currentState = MonsterState.Walk;

    // --- 기본 설정 변수 (Inspector에서 설정) ---
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 5f;
    public float randomMoveDuration = 3f;
    public float gravity = 20.0f; // CharacterController를 위한 중력

    [Header("Detection Settings")]
    public float detectionRadius = 10f; // 플레이어 감지 사정거리
    public float lostSightTime = 4f; // Run -> Walk 전환 시간

    // 레이캐스트가 벽/장애물에 부딪히는지 확인하기 위한 레이어 마스크
    public LayerMask obstacleLayer;

    // --- 내부 관리 변수 ---
    private Transform playerTarget;
    private Vector3 currentRandomDirection;
    private float detectionTimer;
    private float randomMoveTimer;
    private Animator animator;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero; // CharacterController.Move에 사용될 벡터

    private const string PLAYER_TAG = "Player";
    private const string ANIM_WALK = "Walk";
    private const string ANIM_RUN = "Run";

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController 컴포넌트가 필요합니다!");
            enabled = false;
            return;
        }

        // Player Tag 찾기
        GameObject playerObj = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player Tag를 가진 오브젝트를 씬에서 찾을 수 없습니다. 플레이어 태그를 확인하세요.");
        }

        animator = GetComponent<Animator>();

        SetNewRandomDirection();
        SwitchState(MonsterState.Walk);
    }

    void Update()
    {
        // 1. 중력 적용 및 착지 보정 (★★ 이동 불가 및 착지 문제 해결 ★★)
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        else
        {
            // CharacterController가 지면에 확실히 붙도록 -5.0f의 강제 하강 벡터를 유지
            moveDirection.y = -5.0f;
        }

        // 2. 상태 전환 감지 (★★ 함수 누락 오류 해결 ★★)
        CheckForPlayerDetection();

        // 3. 현재 상태에 따른 이동 벡터 설정 및 로테이션 처리
        switch (currentState)
        {
            case MonsterState.Walk:
                HandleWalkState();
                break;
            case MonsterState.Run:
                HandleRunState();
                break;
        }

        // 4. CharacterController를 사용하여 이동 실행
        characterController.Move(moveDirection * Time.deltaTime);
    }

    // --- 상태 전환 ---

    private void SwitchState(MonsterState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (currentState)
        {
            case MonsterState.Walk:
                // Debug.Log("State: Walk");
                if (animator != null) animator.SetTrigger(ANIM_WALK);
                SetNewRandomDirection(); // 새 방향 설정 시 바로 로테이션 적용
                break;

            case MonsterState.Run:
                // Debug.Log("State: Run");
                if (animator != null) animator.SetTrigger(ANIM_RUN);
                detectionTimer = 0f;
                break;
        }
    }

    // --- Walk 상태 (랜덤 이동) ---

    private void HandleWalkState()
    {
        // 1. 이동 벡터 설정
        Vector3 horizontalMove = currentRandomDirection * walkSpeed;
        moveDirection.x = horizontalMove.x;
        moveDirection.z = horizontalMove.z;

        // 2. 몬스터 회전 (★★ 로테이션 누락 문제 해결 ★★)
        Vector3 targetDirection = new Vector3(currentRandomDirection.x, 0, currentRandomDirection.z);
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }

        // 3. 랜덤 이동 타이머 체크
        randomMoveTimer -= Time.deltaTime;
        if (randomMoveTimer <= 0f)
        {
            SetNewRandomDirection();
        }
    }

    private void SetNewRandomDirection()
    {
        // 새로운 랜덤 방향 벡터 설정
        Vector3 newDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        currentRandomDirection = newDirection;
        randomMoveTimer = Random.Range(randomMoveDuration * 0.5f, randomMoveDuration * 1.5f);

        // 새 방향 설정 시 즉시 회전하여, 이동 시작 시 지연 없이 방향을 바라보게 보정
        if (newDirection != Vector3.zero)
        {
            Quaternion startRotation = Quaternion.LookRotation(newDirection);
            transform.rotation = startRotation;
        }
    }

    // --- Run 상태 (추격) ---

    private void HandleRunState()
    {
        if (playerTarget == null) return;

        // 1. 플레이어를 향하는 방향 계산
        Vector3 directionToPlayer = (playerTarget.position - transform.position);
        directionToPlayer.y = 0;
        directionToPlayer.Normalize();

        // 2. 이동 벡터 설정
        Vector3 horizontalMove = directionToPlayer * runSpeed;
        moveDirection.x = horizontalMove.x;
        moveDirection.z = horizontalMove.z;

        // 3. 몬스터 회전 (플레이어를 향해 부드럽게 회전)
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // --- CharacterController 충돌 처리 (벽 감지 및 방향 전환) ---

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 충돌한 오브젝트가 장애물 레이어에 속하고, 현재 Walk 상태인지 확인
        if (((1 << hit.gameObject.layer) & obstacleLayer) != 0 && currentState == MonsterState.Walk)
        {
            // 벽에 부딪혔을 때 새로운 랜덤 방향 설정 및 즉시 회전
            SetNewRandomDirection();
        }
    }

    // --- 감지 및 상태 전환 로직 (★★ 함수 누락 오류 해결 ★★) ---

    private void CheckForPlayerDetection()
    {
        if (playerTarget == null) return;

        bool isPlayerVisible = IsPlayerVisible();

        if (currentState == MonsterState.Walk)
        {
            // Walk 상태: 플레이어가 보이면 Run 상태로 전환
            if (isPlayerVisible)
            {
                SwitchState(MonsterState.Run);
            }
        }
        else if (currentState == MonsterState.Run)
        {
            // Run 상태:
            if (isPlayerVisible)
            {
                // 플레이어가 보이면 타이머 초기화 (추격 지속)
                detectionTimer = 0f;
            }
            else
            {
                // 플레이어가 시야를 벗어나거나 벽에 막히면 타이머 증가
                detectionTimer += Time.deltaTime;

                if (detectionTimer >= lostSightTime)
                {
                    // 4초 이상 보이지 않으면 Walk 상태로 복귀
                    SwitchState(MonsterState.Walk);
                }
            }
        }
    }

    private bool IsPlayerVisible()
    {
        if (playerTarget == null) return false;

        // 1. 거리 검사
        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);
        if (distanceToPlayer > detectionRadius)
        {
            return false;
        }

        // 2. 시야 검사 (Line of Sight)
        Vector3 rayDirection = playerTarget.position - transform.position;
        // 몬스터의 중심에서 쏘기 위해 약간 위로 올릴 수 있음
        Vector3 startPosition = transform.position + Vector3.up * 0.5f;

        // Raycast가 장애물 레이어에 먼저 부딪히는지 확인
        if (Physics.Raycast(startPosition, rayDirection.normalized, out RaycastHit hit, detectionRadius, obstacleLayer))
        {
            // Raycast가 장애물에 부딪히면 (벽에 가려지면) 보이지 않음
            if (!hit.transform.CompareTag(PLAYER_TAG))
            {
                return false;
            }
        }

        // 거리 내에 있고, 장애물에 가려지지 않았다면 시야 확보
        return true;
    }

    // 디버깅을 위한 시각화
    private void OnDrawGizmosSelected()
    {
        // 감지 사정거리를 씬 뷰에 빨간색 원으로 표시
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}