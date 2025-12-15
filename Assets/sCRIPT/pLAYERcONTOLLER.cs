using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    // --- 기존 변수 (생략) ---
    public float moveSpeed = 5f;
    public float sprintSpeedMultiplier = 2f;
    public float jumpPower = 5f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 3f;

    float xRotation = 0f;
    CharacterController controller;
    Transform cam;
    Vector3 velocity;
    bool isGrounded;

    // --- 애니메이션 변수 ---
    private Animator anim;

    // --- 스테미나 변수 ---
    [Header("Stamina Settings")]
    public float maxStamina = 10f;
    public float sprintDuration = 7f;
    public float staminaRechargeTime = 10f;
    [SerializeField] private Slider staminaSlider;

    private float currentStamina;
    private bool isSprinting = false;
    private float staminaConsumeRate;
    private float staminaRechargeRate;

    // --- ★★★ 스테미나 고갈 상태 변수 추가 ★★★ ---
    private bool isStaminaDepleted = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>()?.transform;
        }

        anim = GetComponentInChildren<Animator>();
        if (anim == null)
        {
            Debug.LogError("PlayerController requires an Animator component on a child GameObject.");
        }

        // 스테미나 초기값 설정
        currentStamina = maxStamina;
        staminaConsumeRate = maxStamina / sprintDuration;
        staminaRechargeRate = maxStamina / staminaRechargeTime;

        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = currentStamina;
        }

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleStamina();
        HandleMove();
        HandleLook();
        HandleAnimation();
    }

    // --- ★★★ HandleStamina 메서드 수정 ★★★ ---
    void HandleStamina()
    {
        // 1. 스테미나 고갈 상태 확인 및 해제
        // 스테미나가 0이 되면 고갈 상태 (락아웃) 설정
        if (currentStamina <= 0.01f)
        {
            isStaminaDepleted = true;
        }

        // 스테미나가 완전히 충전되면 고갈 상태 (락아웃) 해제
        if (currentStamina >= maxStamina)
        {
            isStaminaDepleted = false;
        }

        // 2. 달리기 시작 조건 검사 및 스테미나 소모
        // LeftShift를 누르고, 스테미나가 남아 있고, ★아직 고갈 상태가 아니어야★ 달리기 가능
        if (Input.GetKey(KeyCode.LeftShift) && currentStamina > 0.01f && !isStaminaDepleted)
        {
            isSprinting = true;
            currentStamina -= staminaConsumeRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
        }
        // 3. 달리기 중단 조건
        // LeftShift를 떼거나, 스테미나가 고갈되었거나, 락아웃 상태가 되면 중단
        else
        {
            isSprinting = false;
        }

        // 4. 스테미나 충전
        // 달리기 중이 아닐 때만 충전
        if (!isSprinting && currentStamina < maxStamina)
        {
            currentStamina += staminaRechargeRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }

        if (staminaSlider != null)
        {
            staminaSlider.value = currentStamina;
        }
    }

    void HandleMove()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // isSprinting 변수에 따라 속도 결정
        float currentMoveSpeed = isSprinting ? moveSpeed * sprintSpeedMultiplier : moveSpeed;

        Vector3 move = transform.right * h + transform.forward * v;
        controller.Move(move * currentMoveSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);

            if (anim != null)
            {
                anim.SetTrigger("JumpTrigger");
            }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // (HandleLook, HandleAnimation 메서드는 변경 없음)
    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        if (cam != null)
        {
            cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }

    void HandleAnimation()
    {
        if (anim == null) return;

        anim.SetBool("IsGrounded", isGrounded);

        float inputMagnitude = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).magnitude;
        float targetSpeed;

        if (isGrounded)
        {
            if (inputMagnitude > 0.1f)
            {
                if (isSprinting)
                {
                    targetSpeed = 2.0f; // Run02
                }
                else
                {
                    targetSpeed = 1.0f; // Walk01
                }
            }
            else
            {
                targetSpeed = 0.0f; // Afk01 (Idle)
            }
        }
        else
        {
            targetSpeed = 0.0f;
        }

        anim.SetFloat("Speed", targetSpeed);
    }
}