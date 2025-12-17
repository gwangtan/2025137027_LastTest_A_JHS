using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Movement & Stamina")]
    public float moveSpeed = 5f;
    public float sprintSpeedMultiplier = 1.8f;
    public float mouseSensitivity = 2.5f;
    public float maxStamina = 10f;
    [SerializeField] private Slider staminaSlider;
    private float currentStamina;
    private bool isSprinting;
    private bool isExhausted = false; // 스태미나 0일 때 페널티 상태

    [Header("Sanity Settings")]
    public float maxSanity = 100f;
    public float currentSanity = 100f;
    [SerializeField] private Slider sanitySlider;
    public AudioSource sanityAudioSource;
    public AudioClip lowSanitySound;
    public float sanityThreshold = 40f;

    [Header("Inventory UI")]
    public Image almondWaterUISprite;
    public Text almondWaterCountText;
    public Image keyUISprite;
    public Text keyCountText;
    public Image finalKeyUISprite;
    public Text finalKeyStatusText;

    [Header("Interaction UI")]
    public Text interactPromptText;
    public GameObject interactPromptPanel;

    [Header("Animations")]
    [SerializeField] private Animator animator; // 자식 오브젝트의 애니메이터 연결

    private CharacterController controller;
    private Transform cam;
    private float xRotation = 0f;
    private Dictionary<string, int> inventory = new Dictionary<string, int>();
    private bool hasFinalKey = false;
    private GameObject currentInteractable;
    private bool isMonsterChasing = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        cam = GetComponentInChildren<Camera>().transform;

        // 인스펙터에서 연결 안했을 경우를 대비해 자식에서 검색
        if (animator == null) animator = GetComponentInChildren<Animator>();

        currentStamina = maxStamina;
        currentSanity = maxSanity;

        if (staminaSlider) staminaSlider.maxValue = maxStamina;
        if (sanitySlider) { sanitySlider.maxValue = maxSanity; sanitySlider.value = maxSanity; }

        UpdateInventoryUI();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMove();
        HandleLook();
        HandleStamina();
        HandleSanityLogic();
        HandleInteractions();
    }

    void HandleMove()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        // 이동 입력 벡터
        Vector3 moveDir = new Vector3(h, 0, v);
        float inputMagnitude = moveDir.magnitude; // 입력 세기 (0~1)

        // 스태미나 고갈 시 달리기 금지
        float speedMultiplier = (isSprinting && !isExhausted) ? sprintSpeedMultiplier : 1.0f;
        float finalSpeed = inputMagnitude * moveSpeed * speedMultiplier;

        Vector3 move = transform.right * h + transform.forward * v;
        controller.Move(move * (moveSpeed * speedMultiplier) * Time.deltaTime);

        // ★ 애니메이션 업데이트 (Speed 파라미터 활용)
        if (animator != null)
        {
            // 입력이 있을 때만 Speed 값을 전달 (0이면 Idle, 1이면 Walk, 1.8이면 Run 형태의 블렌드 트리)
            float animSpeed = isMoving() ? (isSprinting && !isExhausted ? sprintSpeedMultiplier : 1.0f) : 0f;
            animator.SetFloat("Speed", animSpeed);

            // 땅에 닿아있는지 여부
            animator.SetBool("IsGround", controller.isGrounded);
        }
    }

    // 이동 입력이 있는지 확인하는 헬퍼 함수
    bool isMoving() => Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f;

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleStamina()
    {
        isSprinting = Input.GetKey(KeyCode.LeftShift) && isMoving() && !isExhausted;

        if (isSprinting)
        {
            currentStamina -= Time.deltaTime * 2f;
            if (currentStamina <= 0) { currentStamina = 0; isExhausted = true; }
        }
        else
        {
            currentStamina += Time.deltaTime * 1.5f;
            if (currentStamina >= maxStamina) { currentStamina = maxStamina; isExhausted = false; }
        }
        if (staminaSlider) staminaSlider.value = currentStamina;
    }

    // --- (이하 SanityLogic, Interactions, InventoryUI 등은 이전과 동일하며 누락 없이 포함) ---
    void HandleSanityLogic()
    {
        currentSanity -= 0.5f * Time.deltaTime;
        if (currentSanity <= 0) GameOverManager.Instance.StartSanityGameOver();
        if (sanitySlider) sanitySlider.value = currentSanity;

        if (currentSanity <= sanityThreshold)
        {
            if (!sanityAudioSource.isPlaying) { sanityAudioSource.clip = lowSanitySound; sanityAudioSource.Play(); }
            sanityAudioSource.volume = Mathf.InverseLerp(sanityThreshold, 0, currentSanity);
        }
        else if (sanityAudioSource.isPlaying)
        {
            sanityAudioSource.volume = Mathf.Lerp(sanityAudioSource.volume, 0, Time.deltaTime);
            if (sanityAudioSource.volume < 0.01f) sanityAudioSource.Stop();
        }
    }

    void HandleInteractions()
    {
        if (Input.GetKeyDown(KeyCode.F) && currentInteractable != null)
        {
            string tag = currentInteractable.tag;
            if (tag == "AlmondWater") { AddItem("AlmondWater", 1); Destroy(currentInteractable); }
            else if (tag == "Key") { AddItem("Key", 1); Destroy(currentInteractable); }
            else if (tag == "Workbench")
            {
                if (GetItemCount("Key") >= 3)
                {
                    inventory["Key"] -= 3;
                    hasFinalKey = true;
                    currentInteractable.GetComponent<Workbench>()?.PlayCraftEffect();
                    UpdateInventoryUI();
                    SetPromptText(currentInteractable.tag);
                }
            }
            else if (tag == "ExitZone" && hasFinalKey) SceneManager.LoadScene("ClearScene");
        }
        if (Input.GetKeyDown(KeyCode.G)) UseAlmondWater();

        // 점프 애니메이션 트리거 (기존 Jump 파라미터 사용 시)
        if (Input.GetKeyDown(KeyCode.Space) && controller.isGrounded && animator != null)
        {
            animator.SetTrigger("Jump");
        }
    }

    void SetPromptText(string tag)
    {
        if (interactPromptText == null) return;
        int k = GetItemCount("Key");
        switch (tag)
        {
            case "AlmondWater": interactPromptText.text = "F를 눌러 아몬드 워터 수집"; break;
            case "Key": interactPromptText.text = "F를 눌러 열쇠 수집"; break;
            case "Workbench":
                if (hasFinalKey) interactPromptText.text = "이미 열쇠를 완성했습니다";
                else interactPromptText.text = (k >= 3) ? "F를 눌러 완전한 열쇠 조합" : $"열쇠가 부족합니다 ({k}/3)";
                break;
            case "ExitZone": interactPromptText.text = hasFinalKey ? "F를 눌러 탈출" : "완전한 열쇠가 필요합니다"; break;
        }
    }

    public void SetMonsterChaseState(bool isChasing)
    {
        if (isChasing && !isMonsterChasing) { currentSanity -= 30f; currentSanity = Mathf.Max(currentSanity, 0); }
        isMonsterChasing = isChasing;
    }

    void AddItem(string name, int amt) { if (inventory.ContainsKey(name)) inventory[name] += amt; else inventory[name] = amt; UpdateInventoryUI(); }
    int GetItemCount(string name) => inventory.ContainsKey(name) ? inventory[name] : 0;
    void UseAlmondWater() { if (GetItemCount("AlmondWater") > 0) { inventory["AlmondWater"]--; currentSanity = Mathf.Min(currentSanity + 50f, 100f); UpdateInventoryUI(); } }

    void UpdateInventoryUI()
    {
        int aCount = GetItemCount("AlmondWater");
        if (almondWaterUISprite) almondWaterUISprite.enabled = aCount > 0;
        if (almondWaterCountText) { almondWaterCountText.enabled = aCount > 0; almondWaterCountText.text = "x" + aCount; }
        int kCount = GetItemCount("Key");
        if (keyUISprite) keyUISprite.enabled = kCount > 0;
        if (keyCountText) { keyCountText.enabled = true; keyCountText.text = kCount + "/3"; }
        if (finalKeyUISprite) finalKeyUISprite.enabled = hasFinalKey;
        if (finalKeyStatusText) { finalKeyStatusText.enabled = hasFinalKey; finalKeyStatusText.text = hasFinalKey ? "READY" : ""; }
    }

    private void OnTriggerEnter(Collider other) { currentInteractable = other.gameObject; if (interactPromptPanel) interactPromptPanel.SetActive(true); SetPromptText(other.tag); }
    private void OnTriggerExit(Collider other) { currentInteractable = null; if (interactPromptPanel) interactPromptPanel.SetActive(false); }
}