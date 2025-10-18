using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event
{
    [PacketPath("/event/mission/getclear")]
    public class GetClearedMissions : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetEventMissionClear req = await ReadData<ReqGetEventMissionClear>(); //EventId
            User user = GetUser();

            ResGetEventMissionClear response = new();

            if (!user.EventInfo.TryGetValue(req.EventId, out EventData? userEvent))
            {
                Logging.WriteLine($"User {user.ID} has no data for event {req.EventId}");
                await WriteDataAsync(response);
                return;
            }
            if (userEvent.ClearedStages.Count == 0)
            {
                Logging.WriteLine($"User {user.ID} has no cleared stages for event {req.EventId}");
                await WriteDataAsync(response);
                return;
            }

            var clearedMissions = GameData.Instance.EventMissionListTable.Values.Where(em => userEvent.ClearedStages.Contains(em.ConditionId)).ToList();
            if (clearedMissions.Count == 0)
            {
                Logging.WriteLine($"User {user.ID} has no cleared missions for event {req.EventId}");
                await WriteDataAsync(response);
                return;
            }
            foreach (var mission in clearedMissions)
            {
                response.EventMissionClearList.Add(new NetEventMissionClearData()
                {
                    EventId = req.EventId,
                    EventMissionId = mission.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-1).Ticks
                });
            }

            // TODO
            await WriteDataAsync(response);
        }
    }
}
