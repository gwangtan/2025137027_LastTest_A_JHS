using UnityEngine;

public class MonsterAI : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float runSpeed = 5.5f;
    public float detectionRadius = 15f;
    public LayerMask obstacleLayer;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip ambientSound, chaseSound;
    [SerializeField] private Animator animator; // ★ 몬스터 애니메이터 연결

    private Transform player;
    private PlayerController playerCtrl;
    private bool isChasing = false;

    void Start()
    {
        player = GameObject.FindWithTag("Player").transform;
        if (player != null) playerCtrl = player.GetComponent<PlayerController>();

        if (animator == null) animator = GetComponentInChildren<Animator>(); // 자동 할당 시도

        if (audioSource)
        {
            audioSource.loop = true;
            audioSource.maxDistance = detectionRadius * 1.5f;
            audioSource.spatialBlend = 1.0f;
        }
    }

    void Update()
    {
        float dist = Vector3.Distance(transform.position, player.position);

        // 사운드 볼륨 조절
        if (audioSource) audioSource.volume = Mathf.InverseLerp(detectionRadius * 2, 0, dist);

        bool canSee = IsPlayerInSight(dist);

        // 상태 전환
        if (canSee && !isChasing) StartChase();
        else if (!canSee && isChasing) StopChase();

        // 이동 및 애니메이션 처리
        if (isChasing)
        {
            MoveTowards(runSpeed);
            UpdateAnimation(true); // ★ 추격 중 (Run)
        }
        else
        {
            Patrol();
            UpdateAnimation(false); // ★ 순찰 중 (Walk/Idle)
        }
    }

    // ★ 몬스터 애니메이션 파라미터 업데이트
    void UpdateAnimation(bool chasing)
    {
        if (animator == null) return;

        // IsRunning 파라미터를 통해 Run/Walk 전환
        // Speed 파라미터를 통해 블렌드 트리 제어도 가능하도록 구성
        animator.SetBool("IsRunning", chasing);
        animator.SetFloat("Speed", chasing ? runSpeed : walkSpeed);
    }

    bool IsPlayerInSight(float dist)
    {
        if (dist > detectionRadius) return false;
        RaycastHit hit;
        Vector3 dir = (player.position - transform.position).normalized;
        if (Physics.Raycast(transform.position + Vector3.up, dir, out hit, detectionRadius, obstacleLayer))
        {
            return hit.transform.CompareTag("Player");
        }
        return false;
    }

    void StartChase()
    {
        isChasing = true;
        if (audioSource) { audioSource.clip = chaseSound; audioSource.Play(); }
        if (playerCtrl) playerCtrl.SetMonsterChaseState(true);
    }

    void StopChase()
    {
        isChasing = false;
        if (audioSource) { audioSource.clip = ambientSound; audioSource.Play(); }
        if (playerCtrl) playerCtrl.SetMonsterChaseState(false);
    }

    void MoveTowards(float speed)
    {
        transform.position = Vector3.MoveTowards(transform.position, player.position, speed * Time.deltaTime);
        // 플레이어를 바라보게 회전
        Vector3 targetDir = new Vector3(player.position.x, transform.position.y, player.position.z);
        transform.LookAt(targetDir);
    }

    void Patrol()
    {
        // 간단한 배회 로직 (필요 시 추가)
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) GameOverManager.Instance.StartCollisionGameOver();
    }
}