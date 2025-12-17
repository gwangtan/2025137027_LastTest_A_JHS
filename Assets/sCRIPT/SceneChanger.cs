using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리에 필수

public class SceneChanger : MonoBehaviour
{
    // 버튼 클릭 시 호출할 함수
    public void GoToSampleScene()
    {
        // "SampleScene"이라는 이름의 씬으로 이동
        SceneManager.LoadScene("SampleScene");
    }

    // (선택 사항) 게임 종료 버튼용 함수
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("게임 종료"); // 에디터 확인용
    }
}