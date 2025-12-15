using UnityEngine;

public class MonsterAI1 : MonoBehaviour
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
    public float gravity = 20.0f;

    [Header("Detection Settings")]
    public float detectionRadius = 10f;
    public float lostSightTime = 3f;

    public LayerMask obstacleLayer;

    // --- 내부 관리 변수 ---
    private Transform playerTarget;
    private Vector3 currentRandomDirection;
    private float detectionTimer;
    private float randomMoveTimer;
    private Animator animator;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;

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

        GameObject playerObj = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
        }
        else
        {
            Debug.LogError("Player Tag를 가진 오브젝트를 씬에서 찾을 수 없습니다. 플레이어 태그를 확인하세요.");
        }

        // ★★★ 자식 오브젝트에서 Animator 찾기 (강조) ★★★
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("[FATAL ERROR] Animator 컴포넌트가 이 오브젝트 또는 자식 오브젝트에 없습니다!");
        }
        // ★★★ ------------------------------------ ★★★

        SetNewRandomDirection();
        SwitchState(MonsterState.Walk);
    }

    void Update()
    {
        // 1. 중력 적용 및 착지 보정
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        else
        {
            moveDirection.y = -0.5f;
        }

        // 2. 상태 전환 감지
        CheckForPlayerDetection();

        // 3. 현재 상태에 따른 이동 벡터 설정 및 로테이션 처리
        // ★ HandleWalkState와 HandleRunState에서 각각의 속도(walkSpeed, runSpeed)를 설정합니다. ★
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
        if (currentState == newState)
        {
            return;
        }

        MonsterState oldState = currentState;
        currentState = newState;
        Debug.Log($"[AI] State Switched: {oldState} -> {newState}. Current Speed: {(newState == MonsterState.Run ? runSpeed : walkSpeed)}"); // 상태 전환 및 속도 디버그

        // 애니메이션 트리거 호출
        if (animator != null)
        {
            if (newState == MonsterState.Walk)
            {
                animator.SetTrigger(ANIM_WALK);
                Debug.Log($"[ANIM] SetTrigger: {ANIM_WALK}");
            }
            else if (newState == MonsterState.Run)
            {
                animator.SetTrigger(ANIM_RUN);
                Debug.Log($"[ANIM] SetTrigger: {ANIM_RUN}");
            }
        }

        switch (currentState)
        {
            case MonsterState.Walk:
                SetNewRandomDirection();
                break;

            case MonsterState.Run:
                detectionTimer = 0f;
                break;
        }
    }

    private void HandleWalkState()
    {
        // ★ walkSpeed 적용 ★
        Vector3 horizontalMove = currentRandomDirection * walkSpeed;
        moveDirection.x = horizontalMove.x;
        moveDirection.z = horizontalMove.z;

        RotateTowards(currentRandomDirection);

        randomMoveTimer -= Time.deltaTime;
        if (randomMoveTimer <= 0f)
        {
            SetNewRandomDirection();
        }
    }

    private void SetNewRandomDirection()
    {
        Vector3 newDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        currentRandomDirection = newDirection;
        randomMoveTimer = Random.Range(randomMoveDuration * 0.5f, randomMoveDuration * 1.5f);
    }

    private void HandleRunState()
    {
        if (playerTarget == null) return;

        Vector3 directionToPlayer = (playerTarget.position - transform.position);
        directionToPlayer.y = 0;
        directionToPlayer.Normalize();

        // ★ runSpeed 적용 ★
        Vector3 horizontalMove = directionToPlayer * runSpeed;
        moveDirection.x = horizontalMove.x;
        moveDirection.z = horizontalMove.z;

        RotateTowards(directionToPlayer);
    }

    private void RotateTowards(Vector3 targetDirection)
    {
        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // --- 감지 및 상태 전환 로직 ---

    private void CheckForPlayerDetection()
    {
        if (playerTarget == null) return;

        bool isPlayerVisible = IsPlayerVisible();

        if (currentState == MonsterState.Walk)
        {
            if (isPlayerVisible)
            {
                SwitchState(MonsterState.Run);
            }
        }
        else if (currentState == MonsterState.Run)
        {
            if (isPlayerVisible)
            {
                detectionTimer = 0f;
            }
            else
            {
                detectionTimer += Time.deltaTime;
                if (detectionTimer >= lostSightTime)
                {
                    SwitchState(MonsterState.Walk);
                }
            }
        }
    }

    // (IsPlayerVisible, OnControllerColliderHit, OnDrawGizmosSelected는 이전과 동일)
    private bool IsPlayerVisible()
    {
        if (playerTarget == null) return false;

        Vector3 monsterPosition = transform.position + Vector3.up * 0.5f;
        Vector3 targetPosition = playerTarget.position + Vector3.up * 0.5f;
        Vector3 rayDirection = (targetPosition - monsterPosition).normalized;
        float distanceToPlayer = Vector3.Distance(monsterPosition, targetPosition);

        if (distanceToPlayer > detectionRadius)
        {
            return false;
        }

        RaycastHit hit;
        // Raycast가 플레이어와 장애물 레이어 모두를 확인해야 하므로, 
        // 레이어 마스크를 obstacleLayer만 사용하거나, 플레이어를 포함한 모든 것을 확인하도록 수정할 수 있습니다.
        // 현재는 obstacleLayer에 부딪혔을 때 플레이어가 아니면 막힌 것으로 간주합니다.
        if (Physics.Raycast(monsterPosition, rayDirection, out hit, detectionRadius, obstacleLayer))
        {
            if (!hit.transform.CompareTag(PLAYER_TAG))
            {
                return false; // 장애물에 가려짐
            }
            return true;
        }

        return true;
    }

    // 디버깅을 위한 시각화
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (playerTarget != null)
        {
            Vector3 rayDirection = (playerTarget.position - transform.position).normalized;
            Vector3 startPosition = transform.position + Vector3.up * 0.5f;

            if (IsPlayerVisible())
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.yellow;
            }
            Gizmos.DrawRay(startPosition, rayDirection * detectionRadius);
        }
    }
}