using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Shop.InApp
{
    [PacketPath("/inappshop/jupiter/buyproduct")]
    public class BuyJupiterProduct : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqBuyJupiterProduct req = await ReadData<ReqBuyJupiterProduct>();
            User user = GetUser();

            ResBuyJupiterProduct response = new()
            {
                ReferenceId = "10001",
                RedirectUrl = "https://127.0.0.1/redirect"
            };


            await WriteDataAsync(response);
        }
    }
}