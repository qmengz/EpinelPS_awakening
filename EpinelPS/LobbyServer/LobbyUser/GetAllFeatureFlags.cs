using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.LobbyUser
{
    [PacketPath("/featureflags/all/get")]
    public class GetAllFeatureFlags : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetAllFeatureFlags req = await ReadData<ReqGetAllFeatureFlags>();

            ResGetAllFeatureFlags response = new();
            // For simplicity, all features are set to open (true)
            foreach (var kind in Enum.GetValues<NetFeatureKind>())
            {
                response.Flags.Add(new NetFeatureFlag() { FeatureKind = kind, IsOpen = true });
            }
            await WriteDataAsync(response);
        }
    }
}
