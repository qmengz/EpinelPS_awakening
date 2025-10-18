using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/reddot/get/data")]
    public class GetBHRedDotData : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // bHId
            ReqGetMiniGameBHRedDotData req = await ReadData<ReqGetMiniGameBHRedDotData>();
            User user = GetUser();

            ResGetMiniGameBHRedDotData response = new()
            {
                // missionRewardExists
                MissionRewardExists = false
            };

            await WriteDataAsync(response);
        }
    }
}