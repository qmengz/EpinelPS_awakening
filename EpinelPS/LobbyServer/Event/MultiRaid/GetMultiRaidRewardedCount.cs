using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.MultiRaid
{
    [PacketPath("/event/multiraid/getmultiraidrewardedcount")]
    public class MultiRaidRewardedCount : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetMultiRaidRewardedCount req = await ReadData<ReqGetMultiRaidRewardedCount>();
            ResGetMultiRaidRewardedCount response = new()
            {
                RewardedCount = 0
            };
            await WriteDataAsync(response);
        }
    }
}