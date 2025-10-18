using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/finish")]
    public class FinishBH : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // bHId, finishData
            ReqFinishMiniGameBH req = await ReadData<ReqFinishMiniGameBH>();
            User user = GetUser();

            ResFinishMiniGameBH response = new();
            //   reward
            response.Reward = new()
            {

            };
            //   dailyAccumulatedScore
            response.DailyAccumulatedScore = req.FinishData.Score; // Example assignment
            //   isDailyRewarded
            response.IsDailyRewarded = false; // Example assignment
            //   isNewHighScore
            response.IsNewHighScore = false; // Example assignment

            await WriteDataAsync(response);
        }
    }
}