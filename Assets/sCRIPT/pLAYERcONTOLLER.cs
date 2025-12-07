using UnityEngine;
using UnityEngine.UI; // UI 요소를 사용하기 위해 필요

public class PlayerMovement : MonoBehaviour
{
    // --- 설정 가능한 변수 (Inspector에서 조정) ---
    [Header("이동 설정")]
    public float walkSpeed = 5f;       // 걷기 속도
    public float runSpeed = 10f;       // 달리기 속도
    public float rotationSpeed = 500f; // 회전 속도

    [Header("스태미나 설정")]
    public float maxStamina = 100f;      // 최대 스태미나
    public float staminaDrainRate = 15f; // 초당 소모되는 스태미나 (달릴 때)
    public float staminaChargeRate = 12.5f; // 초당 충전되는 스태미나 (100 / 8초 = 12.5)
    public float staminaRecoveryTime = 8f; // 스태미나 고갈 후 회복 대기 시간 (8초)

    [Header("UI 연결")]
    public Slider staminaSlider; // Canvas Overlay에 있는 Slider UI에 연결

    // --- 내부 상태 변수 ---
    private float currentStamina;
    private bool isRunning;
    private bool isRecovering = false; // 스태미나 고갈로 인해 회복 대기 중인지
    private float recoveryTimer = 0f;

    // 컴포넌트 참조
    private CharacterController characterController;
    private Animator animator; // 애니메이터 컴포넌트

    // --- Unity 생명 주기 함수 ---

    void Start()
    {
        // 필수 컴포넌트 가져오기
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();

        // 스태미나 초기화
        currentStamina = maxStamina;

        // UI Slider 초기 설정
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }
    }

    void Update()
    {
        // 1. 이동 처리
        HandleMovement();

        // 2. 스태미나 처리
        HandleStamina();

        // 3. UI 업데이트
        UpdateStaminaUI();
    }

    // --- 핵심 로직 함수 ---

    private void HandleMovement()
    {
        // 사용자 입력 가져오기
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 이동 방향 벡터 계산 (카메라 기준이 아닌 월드 기준)
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;

        // 이동 중인지 확인
        bool isMoving = moveDirection.magnitude > 0.1f;

        // 달리기 가능 여부 확인
        bool canRun = !isRecovering && currentStamina > 0;

        // Shift 키를 누르고 있고, 앞으로 이동 중이며, 달리기 가능한 상태인지 확인
        isRunning = Input.GetKey(KeyCode.LeftShift) && isMoving && canRun;

        // 실제 이동 속도 설정
        float currentSpeed = isRunning ? runSpeed : walkSpeed;

        // CharacterController를 사용하여 이동
        // Note: CharacterController는 Rigidbody가 아니므로 중력 처리가 필요
        if (characterController.isGrounded)
        {
            moveDirection *= currentSpeed;
        }
        else
        {
            // 공중에 있을 때는 속도를 유지하거나 감속 (필요에 따라 추가 로직)
        }

        // 중력 적용 (CharacterController는 자동으로 중력을 적용하지 않음)
        // 아주 간단한 중력 로직만 포함 (더 정교한 중력/점프는 필요에 따라 추가)
        if (!characterController.isGrounded)
        {
            moveDirection.y += Physics.gravity.y * Time.deltaTime;
        }

        // 실제 이동 실행
        characterController.Move(moveDirection * Time.deltaTime);

        // 회전 처리 (이동 방향으로 회전)
        if (isMoving)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(moveDirection.x, 0, moveDirection.z));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        // 애니메이션 파라미터 설정
        UpdateAnimation(isMoving, isRunning);
    }

    private void UpdateAnimation(bool isMoving, bool isRunning)
    {
        if (animator != null)
        {
            // 걷기: isMoving = true, isRunning = false
            // 달리기: isMoving = true, isRunning = true
            // Idle: isMoving = false

            // Speed 파라미터를 사용하여 애니메이션 블렌딩을 제어하는 방식도 일반적입니다.
            // 여기서는 Walk, Run, Idle 애니메이션 상태를 직접 전환한다고 가정합니다.

            if (isMoving)
            {
                if (isRunning)
                {
                    // Run 애니메이션 적용
                    animator.SetBool("IsRunning", true);
                    animator.SetBool("IsWalking", false);
                }
                else
                {
                    // Walk 애니메이션 적용
                    animator.SetBool("IsWalking", true);
                    animator.SetBool("IsRunning", false);
                }
            }
            else
            {
                // Idle 애니메이션 적용
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsRunning", false);
            }
        }
    }

    private void HandleStamina()
    {
        // 1. 회복 대기 상태 처리
        if (isRecovering)
        {
            recoveryTimer += Time.deltaTime;

            // 8초 회복 시간이 끝났는지 확인
            if (recoveryTimer >= staminaRecoveryTime)
            {
                // 회복 완료: 스태미나를 즉시 100%로 채우고 상태 해제
                currentStamina = maxStamina;
                isRecovering = false;
                recoveryTimer = 0f;
                Debug.Log("스태미나 완전 회복. 다시 달리기 가능.");
            }
            // 회복 중일 때는 다른 스태미나 로직을 수행하지 않음
            return;
        }

        // 2. 달리기 중일 때 소모
        if (isRunning)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f); // 0 미만으로 내려가지 않게

            // 스태미나 고갈 확인
            if (currentStamina <= 0f)
            {
                currentStamina = 0f;
                isRecovering = true; // 회복 대기 상태로 전환
                isRunning = false;   // 달리기 강제 중지
                Debug.Log("스태미나 고갈! 8초간 달리기 불가 상태 시작.");
            }
        }
        // 3. 걷거나 가만히 있을 때 충전 (달리기 중이 아닐 때)
        else
        {
            currentStamina += staminaChargeRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina); // Max 값 초과하지 않게
        }
    }

    private void UpdateStaminaUI()
    {
        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }

    // 이 함수를 사용하여 외부 스크립트에서 스태미나 상태를 확인할 수 있습니다.
    public float GetCurrentStamina()
    {
        return currentStamina;
    }
}