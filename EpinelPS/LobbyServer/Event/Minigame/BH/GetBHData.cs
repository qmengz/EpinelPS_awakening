using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/get/data")]
    public class GetBHData : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetMiniGameBHData req = await ReadData<ReqGetMiniGameBHData>();
            User user = GetUser();

            ResGetMiniGameBHData response = new();
            //   bHData
            response.BHData = new NetMiniGameBHData()
            {
                // dailyAccumulatedScore
                DailyAccumulatedScore = 0,
                // isDailyRewarded
                IsDailyRewarded = false,
                // totalGold
                TotalGold = 0
            };
            //   achievementMissionDataList
            response.AchievementMissionDataList.AddRange([]);
            //   collectionList
            response.CollectionList.AddRange([]);
            //   outGamePassiveList
            response.OutGamePassiveList.AddRange([]);

            await WriteDataAsync(response);
        }
    }
}