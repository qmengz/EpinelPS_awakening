using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/achievementmission/acquire/reward")]
    public class AcquireBHReward : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // bHId, achievementMissionIdList
            ReqAcquireMiniGameBHAchievementMissionReward req = await ReadData<ReqAcquireMiniGameBHAchievementMissionReward>();
            User user = GetUser();

            ResAcquireMiniGameBHAchievementMissionReward response = new();
            //  reward
            //  updatedAchievementMissionDataList

            await WriteDataAsync(response);
        }
    }
}