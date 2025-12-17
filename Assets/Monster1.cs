using UnityEngine;

public class MonsterAI1 : MonoBehaviour
{
    private enum MonsterState { Walk, Run }
    private MonsterState currentState = MonsterState.Walk;

    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float rotationSpeed = 5f;
    public float randomMoveDuration = 3f;
    public float gravity = 20.0f;

    [Header("Detection Settings")]
    public float detectionRadius = 15f;
    public float lostSightTime = 3f;
    public LayerMask obstacleLayer;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip ambientSound;
    [SerializeField] private AudioClip chaseSound;
    public float soundDetectionMultiplier = 1.5f;
    public float maxSoundVolume = 1.0f;

    private Transform playerTarget;
    private PlayerController playerController;
    private Vector3 currentRandomDirection;
    private float detectionTimer;
    private float randomMoveTimer;
    private Animator animator;
    private CharacterController characterController;
    private Vector3 moveDirection = Vector3.zero;
    private bool isAIPaused = false;

    private const string PLAYER_TAG = "Player";

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        GameObject playerObj = GameObject.FindGameObjectWithTag(PLAYER_TAG);
        if (playerObj != null)
        {
            playerTarget = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
        }

        animator = GetComponentInChildren<Animator>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.maxDistance = detectionRadius * soundDetectionMultiplier;
            audioSource.minDistance = 2f;
        }

        ResetMonsterState();
    }

    private void ResetMonsterState()
    {
        detectionTimer = 0f;
        randomMoveTimer = 0f;
        moveDirection = Vector3.zero;
        isAIPaused = false;
        SetNewRandomDirection();

        currentState = MonsterState.Walk;
        UpdateAnimation(MonsterState.Walk);

        if (audioSource != null)
        {
            audioSource.clip = ambientSound;
            audioSource.Stop();
        }
        this.enabled = true;
    }

    void Update()
    {
        if (isAIPaused) { if (audioSource.isPlaying) audioSource.Stop(); HandleGravity(); return; }

        HandleGravity();
        CheckForPlayerDetection();
        HandleSoundControl(); // 매 프레임 거리 체크 및 사운드 관리

        switch (currentState)
        {
            case MonsterState.Walk: HandleWalkState(); break;
            case MonsterState.Run: HandleRunState(); break;
        }

        characterController.Move(moveDirection * Time.deltaTime);
    }

    private void HandleSoundControl()
    {
        if (playerTarget == null || audioSource == null) return;

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        float soundRange = detectionRadius * soundDetectionMultiplier;

        // 범위 내에 있을 때
        if (distance <= soundRange)
        {
            if (!audioSource.isPlaying)
            {
                // 현재 상태에 맞는 클립이 설정되어 있는지 확인 후 재생
                AudioClip targetClip = (currentState == MonsterState.Run) ? chaseSound : ambientSound;
                if (audioSource.clip != targetClip) audioSource.clip = targetClip;
                audioSource.Play();
            }
        }
        else
        {
            if (audioSource.isPlaying) audioSource.Stop();
        }
    }

    private void SwitchState(MonsterState newState)
    {
        if (currentState == newState) return;

        MonsterState oldState = currentState;
        currentState = newState;

        // 1. 사운드 즉시 교체 및 재생
        UpdateStateSound(newState);

        // 2. 애니메이션 업데이트
        UpdateAnimation(newState);

        // 3. 플레이어 컨트롤러에 상태 통보
        if (newState == MonsterState.Run) playerController?.SetMonsterChaseState(true);
        else if (oldState == MonsterState.Run) playerController?.SetMonsterChaseState(false);

        if (currentState == MonsterState.Walk) SetNewRandomDirection();
        else detectionTimer = 0f;
    }

    private void UpdateStateSound(MonsterState state)
    {
        if (audioSource == null) return;
        AudioClip targetClip = (state == MonsterState.Run) ? chaseSound : ambientSound;

        if (audioSource.clip != targetClip)
        {
            audioSource.clip = targetClip;
            // 재생 중이었거나 범위 안이라면 즉시 새로운 클립으로 다시 재생
            if (Vector3.Distance(transform.position, playerTarget.position) <= detectionRadius * soundDetectionMultiplier)
            {
                audioSource.Play();
            }
        }
    }

    private void UpdateAnimation(MonsterState state)
    {
        if (animator == null) return;

        // Trigger 방식은 가끔 씹힐 수 있으므로 Bool 방식을 권장하지만, 
        // 기존 애니메이터 설정에 맞춰 둘 다 실행되도록 안전하게 작성했습니다.
        if (state == MonsterState.Walk)
        {
            animator.SetTrigger("Walk");
            animator.SetBool("IsRunning", false);
        }
        else
        {
            animator.SetTrigger("Run");
            animator.SetBool("IsRunning", true);
        }
    }

    // --- 나머지 이동 관련 로직 (기존과 동일) ---
    private void HandleWalkState()
    {
        Vector3 horizontalMove = currentRandomDirection * walkSpeed;
        moveDirection.x = horizontalMove.x; moveDirection.z = horizontalMove.z;
        RotateTowards(currentRandomDirection);
        randomMoveTimer -= Time.deltaTime;
        if (randomMoveTimer <= 0f) SetNewRandomDirection();
    }

    private void HandleRunState()
    {
        if (playerTarget == null) return;
        Vector3 directionToPlayer = (playerTarget.position - transform.position);
        directionToPlayer.y = 0; directionToPlayer.Normalize();
        Vector3 horizontalMove = directionToPlayer * runSpeed;
        moveDirection.x = horizontalMove.x; moveDirection.z = horizontalMove.z;
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
        bool isVisible = IsPlayerVisible();
        if (currentState == MonsterState.Walk && isVisible) SwitchState(MonsterState.Run);
        else if (currentState == MonsterState.Run)
        {
            if (isVisible) detectionTimer = 0f;
            else
            {
                detectionTimer += Time.deltaTime;
                if (detectionTimer >= lostSightTime) SwitchState(MonsterState.Walk);
            }
        }
    }

    private bool IsPlayerVisible()
    {
        if (playerTarget == null) return false;
        Vector3 monsterPos = transform.position + Vector3.up * 0.5f;
        Vector3 targetPos = playerTarget.position + Vector3.up * 0.5f;
        float dist = Vector3.Distance(monsterPos, targetPos);
        if (dist > detectionRadius) return false;
        if (Physics.Raycast(monsterPos, (targetPos - monsterPos).normalized, out RaycastHit hit, detectionRadius, obstacleLayer))
        {
            return hit.transform.CompareTag(PLAYER_TAG);
        }
        return true;
    }

    private void HandleGravity()
    {
        if (!characterController.isGrounded) moveDirection.y -= gravity * Time.deltaTime;
        else moveDirection.y = -0.5f;
    }

    private void SetNewRandomDirection()
    {
        currentRandomDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        randomMoveTimer = Random.Range(randomMoveDuration * 0.5f, randomMoveDuration * 1.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isAIPaused) return;
        if (other.CompareTag(PLAYER_TAG))
        {
            isAIPaused = true; moveDirection = Vector3.zero;
            if (audioSource != null) audioSource.Stop();
            if (GameOverManager.Instance != null) GameOverManager.Instance.StartCollisionGameOver();
        }
    }
}