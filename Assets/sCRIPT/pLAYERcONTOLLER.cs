using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // ====== Inspector에서 설정 가능한 변수 ======
    [Header("Movement Settings")]
    public float moveSpeed = 5.0f;
    public float rotationSpeed = 500f; // Slerp에 사용될 회전 속도 (선택 사항)

    [Header("Gravity and Jump")]
    public float gravity = 9.81f;
    public float jumpHeight = 1.0f;

    [Header("Camera Reference")]
    // Camera Holder (CameraRotation 스크립트가 부착된 오브젝트)의 Transform
    public Transform cameraHolder;

    // ====== 컴포넌트 참조 ======
    private CharacterController controller;
    private Vector3 moveDirection;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (cameraHolder == null) return; // 카메라 참조가 없으면 작동 중지

        // 1. 입력 처리
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 inputVector = new Vector3(horizontalInput, 0, verticalInput).normalized;

        // 2. 지면에 닿아 있는지 확인 및 중력 적용
        if (controller.isGrounded)
        {
            // A. 이동 방향 계산 (카메라 방향 기준)
            Vector3 forward = cameraHolder.forward;
            Vector3 right = cameraHolder.right;

            // Y축 회전만 사용하기 위해 Y축을 0으로 고정 후 정규화
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            // 최종 이동 방향: 카메라 방향을 기준으로 입력 벡터를 변환
            Vector3 desiredMove = forward * verticalInput + right * horizontalInput;

            // 이동 속도 적용
            moveDirection = desiredMove * moveSpeed;

            // 점프 로직
            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = Mathf.Sqrt(2 * jumpHeight * gravity);
            }
        }
        else
        {
            // 공중에 있을 때 중력 적용
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // 3. CharacterController를 이용한 이동 실행
        controller.Move(moveDirection * Time.deltaTime);

        // 4. 회전 로직 (제거됨): 플레이어의 Y축 회전은 마우스 입력만으로 CameraRotation 스크립트에서 담당합니다.
    }
}