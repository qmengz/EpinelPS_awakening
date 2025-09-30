using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.SoloraId
{
    [PacketPath("/soloraid/getperiod")]
    public class GetSoloraidPeriod : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetSoloRaidPeriod req = await ReadData<ReqGetSoloRaidPeriod>();

            ResGetSoloRaidPeriod response = new()
            {
                Period = new NetSoloRaidPeriodData
                {
                    VisibleDate = DateTime.Now.AddDays(-10).Ticks,
                    StartDate = DateTime.Now.AddDays(-5).Ticks,
                    EndDate = DateTime.Now.AddDays(5).Ticks,
                    DisableDate = DateTime.Now.AddDays(10).Ticks,
                    SettleDate = DateTime.Now.AddDays(15).Ticks,
                }
            };
            // TODO
            await WriteDataAsync(response);
        }
    }
}
