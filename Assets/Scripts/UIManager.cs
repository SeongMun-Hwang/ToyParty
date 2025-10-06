using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI pierrotMissionText;
    public TextMeshProUGUI moveCountText;
    public GameObject clearPanel;

    private void Start()
    {
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 이벤트 구독
        GameEvents.OnPierrotMissionUpdate += UpdatePierrotText;
        GameEvents.OnMoveCountUpdate += UpdateMoveCountText;
        GameEvents.OnMissionComplete += ShowClearPanel;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        GameEvents.OnPierrotMissionUpdate -= UpdatePierrotText;
        GameEvents.OnMoveCountUpdate -= UpdateMoveCountText;
        GameEvents.OnMissionComplete -= ShowClearPanel;
    }

    private void UpdatePierrotText(int count)
    {
        if (pierrotMissionText != null)
        {
            pierrotMissionText.text = count.ToString();
        }
    }

    private void UpdateMoveCountText(int count)
    {
        if (moveCountText != null)
        {
            moveCountText.text = count.ToString();
        }
    }

    private void ShowClearPanel()
    {
        if (clearPanel != null)
        {
            clearPanel.SetActive(true);
        }
    }
    public void OnRestartBtnClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
