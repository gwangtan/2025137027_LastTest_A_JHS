using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance { get; private set; }

    [Header("Sanity Death Settings (Slow)")]
    [SerializeField] private Image sanityBlackoutImage; // 정신력 고갈용 이미지 (예: 서서히 흐려지는 효과)
    public float sanityFadeDuration = 8f;

    [Header("Monster Collision Settings (Instant)")]
    [SerializeField] private Image monsterBlackoutImage; // 몬스터 충돌용 이미지 (예: 피 튀기는 이미지나 완전 검정)
    [SerializeField] private AudioClip collisionGameOverSound;
    public float monsterRestartDelay = 1.0f; // 1초 뒤 재시작

    [Header("General Settings")]
    [SerializeField] private AudioSource audioSource;

    private bool isGameOver = false;

    void Awake()
    {
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

        // 두 이미지 모두 초기에는 투명하게 설정
        if (sanityBlackoutImage != null) SetImageAlpha(sanityBlackoutImage, 0f);
        if (monsterBlackoutImage != null) SetImageAlpha(monsterBlackoutImage, 0f);

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        StopAllCoroutines();
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        img.gameObject.SetActive(true);
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    // 1. 정신력 고갈: 전용 이미지로 8초간 서서히 암전
    public void StartSanityGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // 몬스터 전용 이미지는 끄고 정신력 이미지만 활성화
        if (monsterBlackoutImage != null) monsterBlackoutImage.gameObject.SetActive(false);

        StartCoroutine(FadeToBlackAndReloadScene(sanityFadeDuration));
    }

    // 2. 몬스터 충돌: 전용 이미지를 즉시 띄우고 사운드 재생
    public void StartCollisionGameOver()
    {
        if (isGameOver) return;
        isGameOver = true;

        // 정신력 이미지는 끄고 몬스터 이미지를 즉시 꽉 채움
        if (sanityBlackoutImage != null) sanityBlackoutImage.gameObject.SetActive(false);
        if (monsterBlackoutImage != null)
        {
            SetImageAlpha(monsterBlackoutImage, 1f);
        }

        if (audioSource != null && collisionGameOverSound != null)
        {
            audioSource.PlayOneShot(collisionGameOverSound);
        }

        StartCoroutine(DelayReloadScene(monsterRestartDelay));
    }

    IEnumerator FadeToBlackAndReloadScene(float delay)
    {
        float timer = 0f;
        while (timer < delay)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Clamp01(timer / delay);
            if (sanityBlackoutImage != null) SetImageAlpha(sanityBlackoutImage, alpha);
            yield return null;
        }
        ReloadCurrentScene();
    }

    IEnumerator DelayReloadScene(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ReloadCurrentScene();
    }

    private void ReloadCurrentScene()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}