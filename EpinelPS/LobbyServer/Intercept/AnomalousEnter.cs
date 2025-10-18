using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Intercept
{
    [PacketPath("/intercept/Anomalous/Enter")]
    public class EnterAnomalousIntercept : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqEnterInterceptAnomalous req = await ReadData<ReqEnterInterceptAnomalous>();

            ResEnterInterceptAnomalous response = new();

            User user = GetUser();

            // Save entered anomalous data
            if (!user.EnterInterceptAnomalous.TryAdd(req.InterceptAnomalousId, req))
                user.EnterInterceptAnomalous[req.InterceptAnomalousId] = req;

            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}
