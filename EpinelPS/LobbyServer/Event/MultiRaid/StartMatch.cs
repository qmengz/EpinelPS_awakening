using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.MultiRaid
{
    [PacketPath("/event/multiraid/startmatch")]
    public class StartMatch : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqStartMatch req = await ReadData<ReqStartMatch>();
            ResStartMatch response = new()
            {
                TicketId = "123456", // TODO generate ticket id
                Result = StartMatchResult.Success
            };
            await WriteDataAsync(response);
        }
    }
}