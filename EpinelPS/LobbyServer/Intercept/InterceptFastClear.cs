using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Intercept
{
    [PacketPath("/intercept/fastclear")]
    public class FastClearInterceptData : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqFastClearIntercept req = await ReadData<ReqFastClearIntercept>();

            User user = GetUser();

            if (user.ResetableData.InterceptionTickets == 0)
            {
                Logging.WriteLine("Attempted to fast clear interception when 0 tickets remain", LogType.Warning);
            }

            long damage = 0;
            if (user.ClearIntercepts.TryGetValue(req.Intercept * 10 + req.InterceptId, out ReqClearIntercept? clearData))
            {
                damage = clearData.Damage;
            }

            InterceptionClearResult sRes = InterceptionHelper.Clear(user, req.Intercept, req.InterceptId, damage);
            user.ResetableData.InterceptionTickets--;
            ResFastClearIntercept response = new()
            {
                NormalReward = sRes.NormalReward,
                BonusReward = sRes.BonusReward,
                TicketCount = user.ResetableData.InterceptionTickets,
                MaxTicketCount = JsonDb.Instance.MaxInterceptionCount,
                Result = FastClearResult.Success,
                Damage = damage
            };
            user.AddTrigger(Trigger.InterceptClear, 1);
            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}