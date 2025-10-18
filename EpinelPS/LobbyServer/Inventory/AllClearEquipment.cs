using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Inventory
{
    [PacketPath("/inventory/allclearequipment")]
    public class AllClearEquipment : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqAllClearEquipment req = await ReadData<ReqAllClearEquipment>();
            User user = GetUser();

            ResAllClearEquipment response = new()
            {
                Csn = req.Csn
            };

            foreach (ItemData item in user.Items.ToArray())
            {
                if (item.Csn == req.Csn)
                {
                    // Check if the item being unequipped is T10
                    if (GameData.Instance.IsT10Equipment(item.ItemType))
                    {
                        
                        response.Items.Add(NetUtils.ToNet(item));
                        continue;
                    }

                    item.Csn = 0;
                    item.Position = 0; 
                    response.Items.Add(NetUtils.ToNet(item));
                }
            }

            
            JsonDb.Save();

            await WriteDataAsync(response);
        }
    }
}