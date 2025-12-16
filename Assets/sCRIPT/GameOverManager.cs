using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    // ★★★ 싱글톤 인스턴스 ★★★
    public static GameOverManager Instance { get; private set; }

    [SerializeField] private Image blackoutImage;
    // 정신력 고갈 시 페이드 아웃 시간 (총 8초)
    public float sanityFadeDuration = 8f;

    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip collisionGameOverSound;

    private bool isGameOver = false;

    void Awake()
    {
        // 싱글톤 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        InitializeManager();
    }

    private void InitializeManager()
    {
        isGameOver = false;

        // 페이드 아웃 이미지 초기 설정 (투명하게)
        if (blackoutImage != null)
        {
            Color color = blackoutImage.color;
            color.a = 0f;
            blackoutImage.color = color;
            blackoutImage.gameObject.SetActive(true);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        StopAllCoroutines();
    }

    // 1. 정신력 고갈 게임 오버: 8초 페이드 아웃 후 씬 재시작
    public void StartSanityGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        StartCoroutine(FadeToBlackAndReloadScene(sanityFadeDuration));
    }

    // 2. 몬스터 충돌 게임 오버: 사운드 재생 후 씬 재시작
    public void StartCollisionGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        float soundDuration = 0f;
        if (audioSource != null && collisionGameOverSound != null)
        {
            audioSource.PlayOneShot(collisionGameOverSound);
            soundDuration = collisionGameOverSound.length;
        }

        StartCoroutine(DelayReloadScene(soundDuration));
    }

    // 화면 페이드 아웃 코루틴
    IEnumerator FadeToBlackAndReloadScene(float delay)
    {
        float timer = 0f;

        while (timer < delay)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / delay);

            Color color = blackoutImage.color;
            color.a = alpha;
            blackoutImage.color = color;

            yield return null;
        }

        ReloadCurrentScene();
    }

    // 딜레이 후 씬 재시작 코루틴
    IEnumerator DelayReloadScene(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReloadCurrentScene();
    }

    private void ReloadCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}