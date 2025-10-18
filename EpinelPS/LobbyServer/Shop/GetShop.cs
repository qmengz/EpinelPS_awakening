using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Shop
{
    [PacketPath("/shop/get")]
    public class GetShop : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetShop req = await ReadData<ReqGetShop>();

            ResGetShop response = new()
            {
                Shop = ShopHelper.InitShopData((ShopCategoryType)req.ShopCategory)
            };
            // TODO

            await WriteDataAsync(response);
        }
    }
}
