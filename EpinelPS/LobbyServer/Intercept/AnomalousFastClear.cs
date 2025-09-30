using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
using Newtonsoft.Json;

namespace EpinelPS.LobbyServer.Intercept
{
    [PacketPath("/intercept/Anomalous/FastClear")]
    public class AnomalousFastClear : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // { "interceptAnomalousId": 4 }
            ReqFastClearInterceptAnomalous req = await ReadData<ReqFastClearInterceptAnomalous>();
            User user = GetUser();

            ResFastClearInterceptAnomalous response = new();

            long damageDealt = 0;   // 5,890,000,001

            // 获取之前完成的拦截异常数据
            if (user.FinishInterceptAnomalous.TryGetValue(req.InterceptAnomalousId, out ReqFinishInterceptAnomalous? finishData))
            {
                damageDealt = finishData.DamageDealt;
            }
            // 处理奖励
            if (GameData.Instance.InterceptAnomalous.TryGetValue(req.InterceptAnomalousId, out _))
            {
                response.DamageDealt = damageDealt;
                var sRes = InterceptionHelper.Clear(user, 3, req.InterceptAnomalousId, damageDealt);
                response.NormalReward = sRes.NormalReward;
                response.BonusReward = sRes.BonusReward;
                user.ResetableData.InterceptionTickets--;
                user.AddTrigger(Trigger.InterceptClear, 1);
            }
            else
            {
                throw new Exception($"AnomalousFastClear: 找不到指定的异常体, interceptAnomalousId={req.InterceptAnomalousId}");
            }
            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}
