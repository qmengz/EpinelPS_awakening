
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Archive
{
    [PacketPath("/archive/field/save")]
    public class SaveArchiveField : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqSaveArchiveFieldRawData req = await ReadData<ReqSaveArchiveFieldRawData>();
            User user = GetUser();

            // Save camera data
            if (user.MapJson.ContainsKey(req.MapId))
                user.MapJson[req.MapId] = req.Json;
            else
                user.MapJson.Add(req.MapId, req.Json);

            ResSaveArchiveFieldRawData response = new();

            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}