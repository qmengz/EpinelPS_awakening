using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.PartyMatch
{
    [PacketPath("/partymatch/createparty")]
    public class CreateParty : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqCreateParty req = await ReadData<ReqCreateParty>();
            User user = GetUser();

            ResCreateParty response = new()
            {
                PartyAssignment = new NetPartyAssignment
                {
                    PartyId = "123456",
                    Assignment = new NetAssignment
                    {
                        Host = "0.0.0.0",
                        Port = 443,
                    },
                },
                Code = "Party123456",
                Error = CreatePartyError.Success
            };

            await WriteDataAsync(response);
        }
    }
}