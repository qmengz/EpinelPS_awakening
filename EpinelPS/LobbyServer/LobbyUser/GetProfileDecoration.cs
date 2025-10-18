using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.LobbyUser
{
    [PacketPath("/ProfileCard/DecorationLayout/Get")]
    public class GetProfileDecoration : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqProfileCardDecorationLayout req = await ReadData<ReqProfileCardDecorationLayout>();
            User user = GetUser();

            // default layout
            ProfileCardDecorationLayout layout = new()
            {
                ShowCharacterSpine = true,
                BackgroundId = 201008,
            };

            // if user has a decoration, use its layout
            if (user.ProfileCardDecoration.Layout != null)
            {
                layout = user.ProfileCardDecoration.Layout;
            }

            ResProfileCardDecorationLayout r = new()
            {
                Layout = layout
            };
            await WriteDataAsync(r);
        }
    }
}
