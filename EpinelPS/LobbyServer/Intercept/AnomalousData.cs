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

            // 这里先写死，后续根据活动时间和用户数据进行调整

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
                Logging.WriteLine("用户没有进入过拦截异常", LogType.Warning);
            }

            // 获取当前活动的异常数据管理器
            GameData.Instance.InterceptAnomalousManager.Values.ToList().ForEach(manager =>
            {
                if (DateTime.Now >= manager.StartDate && DateTime.Now <= manager.EndDate)
                {
                    response.InterceptAnomalousManagerId = manager.Id;
                    // 获取已清除的异常数据ID
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