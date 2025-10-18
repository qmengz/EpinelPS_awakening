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

            ResProfileCardObjectList response = new();
            foreach (var item in GameData.Instance.ProfileCardObjectTable.Values)
            {
                if (item.ObjectType == ObjectType.BackGround)
                {
                    response.BackgroundIds.Add(item.Id);
                }
                else if (item.ObjectType == ObjectType.Sticker)
                {
                    response.StickerIds.Add(item.Id);
                }
            }

            await WriteDataAsync(response);
        }
    }
}
