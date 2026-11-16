using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Models;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Episode;

/// <summary>Static helper for limited-time Nikke EP mission logic.</summary>
public static class EpisodeHelper
{
    /// <summary>Lists EP mission IDs whose active period contains the current UTC time.</summary>
    /// <returns>ResListValidEpMission with the currently valid mission IDs.</returns>
    public static ResListValidEpMission BuildValidMissionResponse()
    {
        ResListValidEpMission response = new();

        // Mission dates carry no timezone; DateTime compares ticks only.
        DateTime now = DateTime.UtcNow;

        foreach (NikkeEpMissionRecord mission in GameData.Instance.NikkeEpMissionTable.Values.OrderBy(x => x.Id))
        {
            // Only missions inside their window are playable right now.
            if (IsInPeriod(mission, now))
            {
                response.EpMissionIdList.Add(mission.Id);
            }
        }

        return response;
    }

    /// <summary>Lists completed scenario group IDs for one EP mission.</summary>
    /// <param name="user">Player save data (read only).</param>
    /// <param name="req">Request carrying the EpMissionId.</param>
    /// <returns>ResListEpMissionCompletedScenario with the completed group IDs.</returns>
    public static ResListEpMissionCompletedScenario BuildCompletedScenarioResponse(User user, ReqListEpMissionCompletedScenario req)
    {
        ResListEpMissionCompletedScenario response = new();

        if (user.EpMissions.TryGetValue(req.EpMissionId, out var progress))
        {
            response.ScenarioList.AddRange(progress.CompletedScenarioGroupIds);
        }

        return response;
    }

    /// <summary>Marks one scenario group as watched for an EP mission.</summary>
    /// <param name="user">Player save data (modified).</param>
    /// <param name="req">Request with EpMissionId and ScenarioId.</param>
    /// <returns>ResSetEpMissionScenarioComplete with the period validation result.</returns>
    public static ResSetEpMissionScenarioComplete SetScenarioComplete(User user, ReqSetEpMissionScenarioComplete req)
    {
        ResSetEpMissionScenarioComplete response = new();

        NikkeEpMissionRecord? mission = GameData.Instance.NikkeEpMissionTable.GetValueOrDefault(req.EpMissionId);
        if (mission == null || !IsInPeriod(mission))
        {
            response.PeriodValidationResult = EpMissionPeriodValidationResult.UnAcceptable;
            return response;
        }

        // Only scenario groups belonging to this mission can be marked complete.
        bool belongs = GameData.Instance.NikkeEpMissionScenarioGroupTable.Values
            .Any(x => x.GroupId == mission.ScenarioGroup && x.ScenarioGroupId == req.ScenarioId);
        if (!belongs)
        {
            response.PeriodValidationResult = EpMissionPeriodValidationResult.UnAcceptable;
            return response;
        }

        var progress = GetOrCreateProgress(user, req.EpMissionId);

        // Avoid duplicates when the client reports the same group twice.
        if (!progress.CompletedScenarioGroupIds.Contains(req.ScenarioId))
        {
            progress.CompletedScenarioGroupIds.Add(req.ScenarioId);
        }

        JsonDb.Save();
        return response;
    }

    /// <summary>Grants the EP mission reward once all scenario groups are watched.</summary>
    /// <param name="user">Player save data (modified).</param>
    /// <param name="req">Request carrying the EpMissionId.</param>
    /// <returns>ResObtainEpMissionReward with the reward and period validation result.</returns>
    public static ResObtainEpMissionReward ObtainReward(User user, ReqObtainEpMissionReward req)
    {
        ResObtainEpMissionReward response = new();

        NikkeEpMissionRecord? mission = GameData.Instance.NikkeEpMissionTable.GetValueOrDefault(req.EpMissionId);
        if (mission == null || !IsInPeriod(mission))
        {
            response.PeriodValidationResult = EpMissionPeriodValidationResult.UnAcceptable;
            return response;
        }

        if (!user.EpMissions.TryGetValue(req.EpMissionId, out var progress) || progress.RewardReceived)
        {
            return response;
        }

        // Every scenario group of this mission must be watched before claiming.
        if (!AllScenariosCompleted(mission, progress))
        {
            return response;
        }

        RewardRecord? reward = GameData.Instance.GetRewardTableEntry(mission.RewardId);
        if (reward == null)
        {
            Logging.WriteLine($"Reward table entry {mission.RewardId} not found for EP mission {req.EpMissionId}", LogType.Warning);
            return response;
        }

        progress.RewardReceived = true;
        response.Reward = RewardUtils.RegisterRewardsForUser(user, reward);
        JsonDb.Save();

        return response;
    }

    /// <summary>Reports reward status for the requested EP missions.</summary>
    /// <param name="user">Player save data (read only).</param>
    /// <param name="req">Request carrying the EpMissionIdList.</param>
    /// <returns>ResGetEpMissionRewardStatus with one status per requested mission.</returns>
    public static ResGetEpMissionRewardStatus BuildRewardStatusResponse(User user, ReqGetEpMissionRewardStatus req)
    {
        ResGetEpMissionRewardStatus response = new();

        foreach (int missionId in req.EpMissionIdList)
        {
            NikkeEpMissionRecord? mission = GameData.Instance.NikkeEpMissionTable.GetValueOrDefault(missionId);
            if (mission == null) continue;

            user.EpMissions.TryGetValue(missionId, out var progress);

            EpMissionRewardStatus status;
            if (progress?.RewardReceived == true)
            {
                status = EpMissionRewardStatus.Received;
            }
            else if (progress != null && AllScenariosCompleted(mission, progress))
            {
                status = EpMissionRewardStatus.Enabled;
            }
            else
            {
                status = EpMissionRewardStatus.Disabled;
            }

            response.RewardStatusList.Add(new NetEpMissionRewardStatus
            {
                EpMissionId = missionId,
                EpMissionStatus = status
            });
        }

        return response;
    }

    private static bool IsInPeriod(NikkeEpMissionRecord mission) => IsInPeriod(mission, DateTime.UtcNow);

    private static bool IsInPeriod(NikkeEpMissionRecord mission, DateTime now) =>
        now >= mission.StartDate && now <= mission.EndDate;

    private static EpMissionProgress GetOrCreateProgress(User user, int missionId)
    {
        if (!user.EpMissions.TryGetValue(missionId, out var progress))
        {
            progress = new EpMissionProgress();
            user.EpMissions[missionId] = progress;
        }
        return progress;
    }

    private static bool AllScenariosCompleted(NikkeEpMissionRecord mission, EpMissionProgress progress)
    {
        List<string> required = [.. GameData.Instance.NikkeEpMissionScenarioGroupTable.Values
            .Where(x => x.GroupId == mission.ScenarioGroup && !string.IsNullOrEmpty(x.ScenarioGroupId))
            .Select(x => x.ScenarioGroupId!)];

        // Missions without scenario groups cannot be completed.
        return required.Count > 0 && required.All(id => progress.CompletedScenarioGroupIds.Contains(id));
    }
}
