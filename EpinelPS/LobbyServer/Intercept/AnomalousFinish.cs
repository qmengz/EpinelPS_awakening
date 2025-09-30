using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Intercept
{
    [PacketPath("/intercept/Anomalous/Finish")]
    public class FinishAnomalousIntercept : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqFinishInterceptAnomalous req = await ReadData<ReqFinishInterceptAnomalous>();

            ResFinishInterceptAnomalous response = new();

            User user = GetUser();

            // 记录完成的异常拦截数据
            if (!user.FinishInterceptAnomalous.TryAdd(req.InterceptAnomalousId, req))
                user.FinishInterceptAnomalous[req.InterceptAnomalousId] = req;

            if (GameData.Instance.InterceptAnomalous.TryGetValue(req.InterceptAnomalousId, out _))
            {
                InterceptionClearResult sRes = InterceptionHelper.Clear(user, 3, req.InterceptAnomalousId, req.DamageDealt);
                response.NormalReward = sRes.NormalReward;
                response.BonusReward = sRes.BonusReward;
                user.ResetableData.InterceptionTickets--;
            }
            else
            {
                Logging.WriteLine($"AnomalousFastClear: 找不到指定的异常体, interceptAnomalousId={req.InterceptAnomalousId}");
            }

            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}