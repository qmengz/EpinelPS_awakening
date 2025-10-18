using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.PartyMatch
{
    [PacketPath("/partymatch/joinparty")]
    public class JoinParty : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqJoinParty req = await ReadData<ReqJoinParty>();
            User user = GetUser();

            ResJoinParty response = new()
            {
                Error = JoinPartyError.Success,
                PartyAssignment = new NetPartyAssignment
                {
                    PartyId = req.PartyId,
                    Assignment = new NetAssignment
                    {
                        Host = "0.0.0.0",
                        Port = 443,
                    },
                },
                Code = "Party" + req.PartyId,
            };

            await WriteDataAsync(response);
        }
    }
}