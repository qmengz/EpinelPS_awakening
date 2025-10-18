using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Shop
{
    [PacketPath("/shop/buy")]
    public class BuyShop : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqShopBuyProduct req = await ReadData<ReqShopBuyProduct>();
            User user = GetUser();

            ResShopBuyProduct response = new()
            {
                Result = ShopBuyProductResult.Success // Example success result
            };

            ShopHelper.BuyShopProduct(user, ref response, req.ShopProductTid, req.Quantity);

            await WriteDataAsync(response);
        }
    }
}