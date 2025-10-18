using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Intercept
{
    [PacketPath("/intercept/Anomalous/Data")]
    public class GetAnomalousData : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqInterceptAnomalousData req = await ReadData<ReqInterceptAnomalousData>();
            User user = GetUser();

            // Prepare response
            ResInterceptAnomalousData response = new()
            {
                InterceptAnomalousManagerId = 101,
                RemainingTickets = user.ResetableData.InterceptionTickets,
                LastEnteredInterceptAnomalousOrder = 1
            };

            try
            {
                response.LastEnteredInterceptAnomalousOrder = user.EnterInterceptAnomalous.Aggregate((max, pair) => max.Value.AntiCheatBattleTLogAdditionalInfo.ClientLocalTime > pair.Value.AntiCheatBattleTLogAdditionalInfo.ClientLocalTime ? max : pair).Key;
            }
            catch
            {
                Logging.WriteLine("user has not entered intercept anomalies", LogType.Warning);
            }

            // Get the current active anomalous data manager
            GameData.Instance.InterceptAnomalousManager.Values.ToList().ForEach(manager =>
            {
                if (DateTime.Now >= manager.StartDate && DateTime.Now <= manager.EndDate)
                {
                    response.InterceptAnomalousManagerId = manager.Id;
                    // Get all cleared anomalous IDs for this manager
                    GameData.Instance.InterceptAnomalous.Values.ToList().ForEach(anomalous =>
                    {
                        if (anomalous.Group == manager.Group)
                        {
                            response.ClearedInterceptAnomalousIds.Add(anomalous.Id);
                        }
                    });
                }
            });
            // response.ClearedInterceptAnomalousIds.Add([1, 2, 3, 4, 5]);
            await WriteDataAsync(response);
        }
    }
}