using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.PartyMatch
{
    [PacketPath("/partymatch/listfriendparty")]
    public class ListFriendParty : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqListFriendParty req = await ReadData<ReqListFriendParty>();
            User user = GetUser();

            ResListFriendParty response = new()
            {
                FriendPartyList =
                {
                    new NetAvailableParty
                    {
                        User = new NetWholeUserData
                        {
                            Usn = 1001,
                            Server = 10001,
                            Lv = 50,
                            Icon = user.ProfileIconId,
                            IconPrism = user.ProfileIconIsPrism,
                            Frame = user.ProfileFrame,
                        },
                        PartyId = "123456",
                        Code = "Party",
                    },
                }
            };
            // TODO
            await WriteDataAsync(response);
        }
    }
}