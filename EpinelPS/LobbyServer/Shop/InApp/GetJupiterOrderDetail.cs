using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Shop.InApp
{
    [PacketPath("/inappshop/jupiter/getorderdetail")]
    public class GetJupiterOrderDetail : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetJupiterOrderDetail req = await ReadData<ReqGetJupiterOrderDetail>();
            User user = GetUser();

            ResGetJupiterOrderDetail response = new()
            {
                Status = "Success",
            };

            await WriteDataAsync(response);
        }
    }
}