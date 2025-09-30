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

            // 记录进入的异常拦截数据
            if (!user.EnterInterceptAnomalous.TryAdd(req.InterceptAnomalousId, req))
                user.EnterInterceptAnomalous[req.InterceptAnomalousId] = req;

            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}
