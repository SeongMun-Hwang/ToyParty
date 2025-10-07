using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using DG.Tweening;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI pierrotMissionText;
    public TextMeshProUGUI moveCountText;
    public GameObject clearPanel;
    public GameObject GameOverPanel; 

    [Header("Score UI")]
    public TextMeshProUGUI scoreTmp;
    public Image scoreProgressImg;
    public List<Image> scoreStars;
    public Sprite completeScoreStar;
    public GameObject scorePopupPrefab; // 점수 팝업으로 사용할 프리팹

    [Header("Score Settings")]
    public int targetScore = 10000; // 목표 점수

    private int _totalScore = 0;

    private void Start()
    {
        if (clearPanel != null)
        {
            clearPanel.SetActive(false);
        }
        if (GameOverPanel != null) 
        {
            GameOverPanel.SetActive(false);
        }
        UpdateScoreUI(0); // 시작할 때 점수 UI 초기화
    }

    private void OnEnable()
    {
        // 이벤트 구독
        GameEvents.OnPierrotMissionUpdate += UpdatePierrotText;
        GameEvents.OnMoveCountUpdate += UpdateMoveCountText;
        GameEvents.OnMissionComplete += ShowClearPanel;
        GameEvents.OnScoreUpdated += UpdateScoreUI;
        GameEvents.OnScoreGained += ShowScorePopup;
        GameEvents.OnGameOver += ShowGameOverPanel;
    }

    private void OnDisable()
    {
        // 이벤트 구독 해제 (메모리 누수 방지)
        GameEvents.OnPierrotMissionUpdate -= UpdatePierrotText;
        GameEvents.OnMoveCountUpdate -= UpdateMoveCountText;
        GameEvents.OnMissionComplete -= ShowClearPanel;
        GameEvents.OnScoreUpdated -= UpdateScoreUI;
        GameEvents.OnScoreGained -= ShowScorePopup;
        GameEvents.OnGameOver -= ShowGameOverPanel;
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

    private void ShowGameOverPanel()
    {
        if (GameOverPanel != null)
        {
            GameOverPanel.SetActive(true);
        }
    }

    public void OnRestartBtnClicked()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void UpdateScoreUI(int newScore)
    {
        _totalScore = newScore;
        if (scoreTmp != null)
        {
            scoreTmp.text = _totalScore.ToString();
        }

        if (scoreProgressImg != null)
        {
            scoreProgressImg.fillAmount = (float)_totalScore / targetScore;
        }

        // 목표 점수에 따른 별 이미지 변경
        if (completeScoreStar != null && scoreStars != null)
        {
            if (_totalScore >= targetScore)
            {
                scoreStars[2].sprite = completeScoreStar;
            }
            if (_totalScore >= targetScore * 2 / 3)
            {
                scoreStars[1].sprite = completeScoreStar;
            }
            if (_totalScore >= targetScore * 1 / 3)
            {
                scoreStars[0].sprite = completeScoreStar;
            }
        }
    }

    private void ShowScorePopup(int score, Vector3 position)
    {
        if (scorePopupPrefab == null) return;

        GameObject popup = Instantiate(scorePopupPrefab, position, Quaternion.identity);
        TextMeshPro tmp = popup.GetComponent<TextMeshPro>();
        if (tmp == null)
        {
            tmp = popup.GetComponentInChildren<TextMeshPro>();
        }

        if (tmp != null)
        {
            tmp.text = score.ToString();
            // 애니메이션
            Sequence sequence = DOTween.Sequence();
            sequence.Append(popup.transform.DOMoveY(position.y + 1f, 1f).SetEase(Ease.OutQuad));
            sequence.Join(tmp.DOFade(0, 1f).SetEase(Ease.InQuad));
            sequence.OnComplete(() => Destroy(popup));
        }
    }
}
