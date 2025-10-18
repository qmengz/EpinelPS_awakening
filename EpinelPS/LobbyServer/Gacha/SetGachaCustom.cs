using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Gacha
{
    [PacketPath("/Gacha/SetCustom")]
    public class SetGachaCustom : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqSetGachaCustom req = await ReadData<ReqSetGachaCustom>();
            User user = GetUser();

            /// Save the custom data
            if (!user.GachaCustomInfo.TryAdd(req.Type, new GachaCustomData()
            {
                Type = req.Type,
                Custom = [.. req.Custom]
            }))
            {
                user.GachaCustomInfo[req.Type].Custom = [.. req.Custom];
            }

            ResSetGachaCustom response = new();

            await WriteDataAsync(response);
        }
    }
}