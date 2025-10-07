using System;
using UnityEngine;

public static class GameEvents
{
    // 삐에로 미션 카운트가 업데이트될 때 호출될 이벤트
    public static event Action<int> OnPierrotMissionUpdate;
    public static void PierrotMissionUpdate(int count) => OnPierrotMissionUpdate?.Invoke(count);

    // 남은 이동 횟수가 업데이트될 때 호출될 이벤트
    public static event Action<int> OnMoveCountUpdate;
    public static void MoveCountUpdate(int count) => OnMoveCountUpdate?.Invoke(count);

    // 미션 클리어 시 호출될 이벤트
    public static event Action OnMissionComplete;
    public static void MissionComplete() => OnMissionComplete?.Invoke();

    // 점수 획득 시 호출될 이벤트 (획득 점수, 월드 좌표)
    public static event Action<int, Vector3> OnScoreGained;
    public static void ScoreGained(int score, Vector3 position) => OnScoreGained?.Invoke(score, position);

    // 총 점수가 업데이트될 때 호출될 이벤트
    public static event Action<int> OnScoreUpdated;
    public static void ScoreUpdated(int totalScore) => OnScoreUpdated?.Invoke(totalScore);
}
