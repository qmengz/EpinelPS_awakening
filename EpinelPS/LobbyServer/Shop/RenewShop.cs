using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Shop
{
    [PacketPath("/shop/renew")]
    public class RenewShop : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqShopRenew req = await ReadData<ReqShopRenew>();
            ResShopRenew response = new()
            {
                // Renew Shop Data
                Shop = ShopHelper.InitShopData((ShopCategoryType)req.ShopCategory)
            };

            await WriteDataAsync(response);
        }
    }
}
