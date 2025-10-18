using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.LobbyUser
{
    [PacketPath("/user/scenario/watchalbumlog")]
    public class WatchAlbumScenarioLog : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqWatchAlbumScenarioLog req = await ReadData<ReqWatchAlbumScenarioLog>();

            ResWatchAlbumScenarioLog response = new();

            // TODO: Implement the logic to retrieve and populate the response

            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}