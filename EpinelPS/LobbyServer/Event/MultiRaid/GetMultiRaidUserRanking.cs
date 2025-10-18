using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.MultiRaid
{
    [PacketPath("/event/multiraid/getmultiraiduserranking")]
    public class GetMultiRaidUserRanking : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetMultiRaidUserRanking req = await ReadData<ReqGetMultiRaidUserRanking>();
            ResGetMultiRaidUserRanking response = new()
            {
                UserRanking = new NetMultiRaidRankingData
                {

                },
                Total = 1,
            };

            await WriteDataAsync(response);
        }
    }
}