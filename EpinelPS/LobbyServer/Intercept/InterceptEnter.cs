using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Intercept
{
    [PacketPath("/intercept/enter")]
    public class EnterInterceptData : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // { "intercept": 2, "interceptId": 5, "teamNumber": 1, "antiCheatAdditionalInfo": { "clientLocalTime": "638940074426135688" } }
            ReqEnterIntercept req = await ReadData<ReqEnterIntercept>();
            User user = GetUser();

            ResEnterIntercept response = new();

            // Save entered intercept data
            if (user.EnterIntercepts.TryAdd(req.Intercept * 10 + req.InterceptId, req))
                user.EnterIntercepts[req.Intercept * 10 + req.InterceptId] = req;

            user.AddTrigger(Trigger.InterceptStart, 1);

            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}