using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    // --- 기본 이동 변수 ---
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
    private bool isStaminaDepleted = false;

    // --- 정신력 및 아이템 변수 ---
    [Header("Sanity & Item Settings")]
    public float maxSanity = 100f;
    [SerializeField] private Slider sanitySlider;
    // 3분(180초)에 0으로 도달하도록 설정된 기본 정신력 감소율
    public float baseSanityDrainRate = 0.556f;
    public float chaseSanityDrainRate = 5f;
    public float chaseDurationToDrain = 4f;

    // 인벤토리 UI 오버레이
    [Header("Inventory UI Overlay")]
    public Image almondWaterUISprite;
    public Text almondWaterCountText;
    [SerializeField] private Image interactPromptUI;

    // 게임 오버 설정
    [Header("Game Over Settings")]
    [SerializeField] private GameOverManager gameOverManager;
    public string monsterTag = "Monster";

    private float currentSanity;
    private Dictionary<string, int> inventory = new Dictionary<string, int>();
    private GameObject currentInteractableItem;

    // 몬스터 감지 관련 변수
    private bool isMonsterChasing = false;
    private float chaseTimer = 0f;
    private bool isGameOverCalled = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>()?.transform;
        anim = GetComponentInChildren<Animator>();

        // 초기값 설정 및 비율 계산
        currentStamina = maxStamina;
        staminaConsumeRate = maxStamina / sprintDuration;
        staminaRechargeRate = maxStamina / staminaRechargeTime;
        currentSanity = maxSanity;

        // UI 초기화
        if (staminaSlider != null) { staminaSlider.maxValue = maxStamina; staminaSlider.value = currentStamina; }
        if (sanitySlider != null) { sanitySlider.maxValue = maxSanity; sanitySlider.value = currentSanity; }

        if (interactPromptUI != null) interactPromptUI.enabled = false;
        UpdateInventoryUI();

        Cursor.lockState = CursorLockMode.Locked;

        // GameOverManager 싱글톤 참조
        if (gameOverManager == null)
        {
            gameOverManager = GameOverManager.Instance;
        }
    }

    void Update()
    {
        if (isGameOverCalled) return;

        HandleStamina();
        HandleSanity();
        HandleMove();
        HandleLook();
        HandleAnimation();
        HandleInteractions();
    }

    // --- 외부 호출 메서드 ---
    public void SetMonsterChaseState(bool isChasing)
    {
        isMonsterChasing = isChasing;
        if (!isChasing)
        {
            chaseTimer = 0f;
        }
    }

    // --- HandleSanity (정신력) 메서드 ---
    void HandleSanity()
    {
        // 1. 기본 정신력 감소 (지속적)
        currentSanity -= baseSanityDrainRate * Time.deltaTime;

        // 2. 몬스터 추적 시 추가 감소 로직
        if (isMonsterChasing)
        {
            chaseTimer += Time.deltaTime;
        }

        if (chaseTimer >= chaseDurationToDrain)
        {
            currentSanity -= chaseSanityDrainRate * Time.deltaTime;
        }

        currentSanity = Mathf.Max(currentSanity, 0f); // 0 이하 방지

        // 3. UI에 반영
        if (sanitySlider != null)
        {
            sanitySlider.value = currentSanity;
        }

        // 4. 정신력 고갈 및 게임 오버 처리
        if (currentSanity <= 0.01f && !isGameOverCalled)
        {
            isGameOverCalled = true;
            enabled = false;

            if (gameOverManager != null || GameOverManager.Instance != null)
            {
                (gameOverManager ?? GameOverManager.Instance).StartSanityGameOver();
            }
        }
    }

    // --- HandleInteractions (아이템 상호작용) 메서드 ---
    void HandleInteractions()
    {
        // F 키로 아이템 획득
        if (Input.GetKeyDown(KeyCode.F) && currentInteractableItem != null)
        {
            string itemTag = currentInteractableItem.tag;

            if (itemTag == "AlmondWater")
            {
                AddItem("AlmondWater", 1);
                Destroy(currentInteractableItem);

                currentInteractableItem = null;
                if (interactPromptUI != null) interactPromptUI.enabled = false;
            }
        }

        // ★★★ G 키로 아몬드 워터 사용 (정신력 회복) ★★★
        if (Input.GetKeyDown(KeyCode.G))
        {
            UseItem("AlmondWater");
        }
    }

    // --- 아이템 관리 메서드 ---
    void AddItem(string itemName, int amount)
    {
        if (inventory.ContainsKey(itemName))
        {
            inventory[itemName] += amount;
        }
        else
        {
            inventory.Add(itemName, amount);
        }
        UpdateInventoryUI();
    }

    void UseItem(string itemName)
    {
        if (itemName == "AlmondWater" && inventory.ContainsKey(itemName) && inventory[itemName] > 0)
        {
            // ★ 정신력 최대치로 회복
            currentSanity = maxSanity;
            if (sanitySlider != null) sanitySlider.value = currentSanity;

            inventory[itemName]--;
            if (inventory[itemName] <= 0)
            {
                inventory.Remove(itemName);
            }
            UpdateInventoryUI();
        }
    }

    void UpdateInventoryUI()
    {
        if (inventory.ContainsKey("AlmondWater"))
        {
            int count = inventory["AlmondWater"];
            if (almondWaterUISprite != null) almondWaterUISprite.enabled = true;
            if (almondWaterCountText != null)
            {
                almondWaterCountText.enabled = true;
                almondWaterCountText.text = $"x{count}";
            }
        }
        else
        {
            if (almondWaterUISprite != null) almondWaterUISprite.enabled = false;
            if (almondWaterCountText != null) almondWaterCountText.enabled = false;
        }
    }

    // --- 아이템 획득 범위 감지 ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AlmondWater"))
        {
            currentInteractableItem = other.gameObject;
            if (interactPromptUI != null) interactPromptUI.enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("AlmondWater"))
        {
            if (other.gameObject == currentInteractableItem)
            {
                currentInteractableItem = null;
                if (interactPromptUI != null) interactPromptUI.enabled = false;
            }
        }
    }

    // --- 몬스터 충돌 게임 오버 처리 (MonsterAI1의 OnTriggerEnter가 대신 처리) ---
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 이 로직은 MonsterAI1.cs에서 처리되므로, 비워둡니다.
    }

    // --- HandleStamina ---
    void HandleStamina()
    {
        if (currentStamina <= 0.01f) { isStaminaDepleted = true; }
        if (currentStamina >= maxStamina) { isStaminaDepleted = false; }
        if (Input.GetKey(KeyCode.LeftShift) && currentStamina > 0.01f && !isStaminaDepleted)
        {
            isSprinting = true;
            currentStamina -= staminaConsumeRate * Time.deltaTime;
            currentStamina = Mathf.Max(currentStamina, 0f);
        }
        else { isSprinting = false; }
        if (!isSprinting && currentStamina < maxStamina)
        {
            currentStamina += staminaRechargeRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
        }
        if (staminaSlider != null) { staminaSlider.value = currentStamina; }
    }

    // --- HandleMove ---
    void HandleMove()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0) { velocity.y = -2f; }
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        float currentMoveSpeed = isSprinting ? moveSpeed * sprintSpeedMultiplier : moveSpeed;
        Vector3 move = transform.right * h + transform.forward * v;
        controller.Move(move * currentMoveSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpPower * -2f * gravity);
            if (anim != null) { anim.SetTrigger("JumpTrigger"); }
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // --- HandleLook ---
    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        transform.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        if (cam != null) { cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f); }
    }

    // --- HandleAnimation ---
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
                targetSpeed = isSprinting ? 2.0f : 1.0f;
            }
            else { targetSpeed = 0.0f; }
        }
        else { targetSpeed = 0.0f; }

        anim.SetFloat("Speed", targetSpeed);
    }
}