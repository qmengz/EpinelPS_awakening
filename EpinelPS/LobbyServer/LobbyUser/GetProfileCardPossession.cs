using EpinelPS.Utils;
using EpinelPS.Data;

namespace EpinelPS.LobbyServer.LobbyUser
{
    [PacketPath("/ProfileCard/Possession/Get")]
    public class GetProfileCardPossession : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqProfileCardObjectList req = await ReadData<ReqProfileCardObjectList>();
            User user = GetUser();

            ResProfileCardObjectList response = new();

            Dictionary<int, ProfileCardObjectRecord> ProfileCardObjects = GameData.Instance.ProfileCardObjectTable;
            List<ItemData> userfileCardObjects = [.. user.Items.Where(item =>
                ProfileCardObjects.ContainsKey(item.ItemType))];


            foreach (ItemData item in userfileCardObjects)
            {
                if (ProfileCardObjects.TryGetValue(item.ItemType, out ProfileCardObjectRecord ? ProfileCardObject)) {
                    if (ProfileCardObject.ObjectType.Equals("BackGround"))
                    {
                        response.BackgroundIds.Add(item.ItemType);
                    }
                    else if (ProfileCardObject.ObjectType.Equals("Sticker"))
                    {
                        response.StickerIds.Add(item.ItemType);
                    }
                }
            }

            await WriteDataAsync(response);
        }
    }
}
