using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

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

            // get damage dealt from finish data
            if (user.FinishInterceptAnomalous.TryGetValue(req.InterceptAnomalousId, out ReqFinishInterceptAnomalous? finishData))
            {
                damageDealt = finishData.DamageDealt;
            }
            // process rewards
            if (GameData.Instance.InterceptAnomalous.TryGetValue(req.InterceptAnomalousId, out _))
            {
                response.DamageDealt = damageDealt;
                var sRes = InterceptionHelper.Clear(user, 3, req.InterceptAnomalousId, damageDealt);
                response.NormalReward = sRes.NormalReward;
                response.BonusReward = sRes.BonusReward;
                user.ResetableData.InterceptionTickets--;
                user.AddTrigger(Trigger.InterceptClear, 1);
                user.AddTrigger(Trigger.InterceptStart, 1);
            }
            else
            {
                Logging.WriteLine($"AnomalousFastClear: InterceptAnomalousId {req.InterceptAnomalousId} not found", LogType.Warning);
            }
            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}
