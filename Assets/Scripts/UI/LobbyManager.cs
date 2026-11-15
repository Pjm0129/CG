using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string mainGameSceneName = "MainGame";

    private bool isLoading;

    /// <summary>
    /// PRESS START 버튼에서만 호출합니다.
    /// 화면 클릭이나 다른 UI 클릭으로는 게임이 시작되지 않습니다.
    /// </summary>
    public void StartGame()
    {
        if (isLoading)
            return;

        isLoading = true;

        // 설정창 등이 열려있더라도 정상적인 시간 상태로 시작
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainGameSceneName);
    }

    /// <summary>
    /// 필요할 경우 종료 버튼에서 사용합니다.
    /// </summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit Game");
#else
        Application.Quit();
#endif
    }
}