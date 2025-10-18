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

            // Save finished anomalous data
            if (!user.FinishInterceptAnomalous.TryAdd(req.InterceptAnomalousId, req))
                user.FinishInterceptAnomalous[req.InterceptAnomalousId] = req;

            if (GameData.Instance.InterceptAnomalous.TryGetValue(req.InterceptAnomalousId, out _))
            {
                InterceptionClearResult sRes = InterceptionHelper.Clear(user, 3, req.InterceptAnomalousId, req.DamageDealt);
                response.NormalReward = sRes.NormalReward;
                response.BonusReward = sRes.BonusReward;
                user.ResetableData.InterceptionTickets--;
                user.AddTrigger(Trigger.InterceptClear, 1);
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