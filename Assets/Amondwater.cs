using UnityEngine;

/// <summary>
/// 아몬드 워터 아이템의 콜라이더를 설정하고, 상호작용 가능한 상태임을 표시합니다.
/// 플레이어가 획득할 때까지 존재하는 역할을 합니다.
/// </summary>
public class AlmondWaterItem : MonoBehaviour
{
    private Collider itemCollider;

    void Awake()
    {
        // 1. Collider 컴포넌트 찾기
        itemCollider = GetComponent<Collider>();

        if (itemCollider == null)
        {
            // 콜라이더가 없으면 에러 로그를 출력하고 비활성화합니다.
            Debug.LogError($"[AlmondWaterItem] 오브젝트 '{gameObject.name}'에 Collider 컴포넌트가 없습니다. 플레이어와 상호작용하려면 필요합니다.");
            enabled = false;
            return;
        }

        // 2. 콜라이더 설정 검증
        if (!itemCollider.isTrigger)
        {
            // 플레이어의 OnTriggerEnter와 충돌 로직을 사용하기 위해 Is Trigger가 필수입니다.
            Debug.LogWarning($"[AlmondWaterItem] 오브젝트 '{gameObject.name}'의 Collider는 Is Trigger가 활성화되어야 합니다. 자동으로 설정합니다.");
            itemCollider.isTrigger = true;
        }

        // 3. 태그 설정 검증 (가장 중요)
        if (gameObject.tag != "AlmondWater")
        {
            Debug.LogError($"[AlmondWaterItem] 오브젝트 '{gameObject.name}'의 태그가 'AlmondWater'가 아닙니다. PlayerController가 이 아이템을 인식할 수 없습니다. 에디터에서 태그를 수정해 주세요.");
            // 스크립트에서 강제로 태그를 변경하는 것은 권장되지 않으므로 경고만 출력합니다.
        }
    }

    // 이 스크립트는 Awake()에서 유효성 검사만 하고,
    // 실제 획득 로직(F 키 입력, 인벤토리 추가, 오브젝트 파괴)은 
    // PlayerController의 OnTriggerEnter와 HandleInteractions에서 처리합니다.
}