using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive
{
    [PacketPath("/archive/field/enter")]
    public class EnterArchiveField : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqEnterArchiveField req = await ReadData<ReqEnterArchiveField>();
            User user = GetUser();

            ResEnterArchiveField response = new();

            // Retrieve camera data
            if (user.MapJson.TryGetValue(req.MapId, out string? mapJson))
            {
                response.Json = mapJson;
            }


            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}
