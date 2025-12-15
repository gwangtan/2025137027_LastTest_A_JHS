using UnityEngine;

public class CameraRotation : MonoBehaviour
{
    // ====== Inspector에서 설정 가능한 변수 ======
    [Header("Rotation Settings")]
    public float mouseSensitivity = 100f;
    public Transform playerBody; // PlayerMovement 스크립트가 부착된 플레이어의 Transform

    [Header("Vertical Clamp")]
    public float minVerticalAngle = -45f;
    public float maxVerticalAngle = 85f;

    private float xRotation = 0f;

    private void Start()
    {
        // 마우스 커서를 중앙에 고정하고 보이지 않게 합니다.
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        if (playerBody == null) return;

        // 1. 마우스 입력 값 가져오기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. 상하 회전 (카메라 자체의 X축 회전)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        // 현재 오브젝트(Camera Holder)의 로컬 회전을 적용
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 3. 좌우 회전 (플레이어 몸체(Body)의 Y축 회전)
        // 오직 마우스 입력으로만 플레이어의 이동 기준 방향을 변경합니다.
        playerBody.Rotate(Vector3.up * mouseX);
    }
}