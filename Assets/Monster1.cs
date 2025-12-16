using UnityEngine;

public class MonsterAI1 : MonoBehaviour
{
    // --- 몬스터 상태 정의 ---
    private enum MonsterState { Walk, Run }
    private MonsterState currentState = MonsterState.Walk;

    // --- 기본 설정 변수 ---
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
    private PlayerController playerController;
    private Vector3 currentRandomDirection;
    private float detectionTimer;
    private float randomMoveTimer;
    private Animator animator;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;

    // ★★★ AI 일시 정지 상태 (게임 오버 시 움직임 방지) ★★★
    private bool isAIPaused = false;

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
            playerController = playerObj.GetComponent<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("Player 오브젝트에 PlayerController 컴포넌트가 없습니다!");
            }
        }
        else
        {
            Debug.LogError("Player Tag를 가진 오브젝트를 씬에서 찾을 수 없습니다.");
        }

        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("[FATAL ERROR] Animator 컴포넌트가 이 오브젝트 또는 자식 오브젝트에 없습니다!");
        }

        ResetMonsterState();
    }

    // 씬 로드 후 항상 초기 상태를 보장하는 함수
    private void ResetMonsterState()
    {
        detectionTimer = 0f;
        randomMoveTimer = 0f;
        moveDirection = Vector3.zero;
        isAIPaused = false;

        SetNewRandomDirection();
        SwitchState(MonsterState.Walk);

        this.enabled = true;
    }

    void Update()
    {
        if (isAIPaused)
        {
            if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
                characterController.Move(moveDirection * Time.deltaTime);
            }
            return;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }
        else
        {
            moveDirection.y = -0.5f;
        }

        CheckForPlayerDetection();

        switch (currentState)
        {
            case MonsterState.Walk:
                HandleWalkState();
                break;
            case MonsterState.Run:
                HandleRunState();
                break;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    // 몬스터 충돌 감지 로직 (Is Trigger 콜라이더 필요)
    private void OnTriggerEnter(Collider other)
    {
        if (isAIPaused) return;

        if (other.CompareTag(PLAYER_TAG))
        {
            isAIPaused = true;
            moveDirection = Vector3.zero;

            if (GameOverManager.Instance != null)
            {
                GameOverManager.Instance.StartCollisionGameOver();
            }
        }
    }

    // 플레이어에게 추적 상태를 통보
    private void SendChaseStateToPlayer(bool isChasing)
    {
        if (playerController != null)
        {
            playerController.SetMonsterChaseState(isChasing);
        }
    }

    // --- 상태 전환 ---
    private void SwitchState(MonsterState newState)
    {
        if (currentState == newState) return;

        MonsterState oldState = currentState;
        currentState = newState;

        // PlayerController에 추적 상태 통보
        if (newState == MonsterState.Run && oldState != MonsterState.Run)
        {
            SendChaseStateToPlayer(true);
        }
        else if (newState == MonsterState.Walk && oldState == MonsterState.Run)
        {
            SendChaseStateToPlayer(false);
        }

        if (animator != null)
        {
            if (newState == MonsterState.Walk) { animator.SetTrigger(ANIM_WALK); }
            else if (newState == MonsterState.Run) { animator.SetTrigger(ANIM_RUN); }
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