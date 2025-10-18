using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Inventory
{
    [PacketPath("/inventory/wearequipmentlist")]
    public class WearEquipmentList : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqWearEquipmentList req = await ReadData<ReqWearEquipmentList>();
            User user = GetUser();

            ResWearEquipmentList response = new();

            // TODO optimize
            foreach (long item2 in req.IsnList)
            {
                int pos = NetUtils.GetItemPos(user, item2);

                // Check if the item being equipped is T10
                ItemData? itemToCheck = user.Items.FirstOrDefault(x => x.Isn == item2);
                if (itemToCheck != null && GameData.Instance.IsT10Equipment(itemToCheck.ItemType))
                {
                    // If trying to equip a T10 item, check if there's already a T10 item in that position
                    bool hasT10InPosition = user.Items.Any(x => x.Position == pos && x.Csn == req.Csn && GameData.Instance.IsT10Equipment(x.ItemType));
                    if (hasT10InPosition)
                    {
                        // Don't allow replacing T10 equipment
                        continue;
                    }
                }

                // Check if item still exists after previous operations
                itemToCheck = user.Items.FirstOrDefault(x => x.Isn == item2);
                if (itemToCheck == null)
                {
                    // Item no longer exists, skip this iteration
                    continue;
                }

                // unequip previous items
                foreach (ItemData item in user.Items.ToArray())
                {
                    // Check if the item being unequipped is T10
                    if (GameData.Instance.IsT10Equipment(item.ItemType))
                    {
                        continue;
                    }
                    if (item.Position == pos && item.Csn == req.Csn)
                    {
                        item.Csn = 0;
                        item.Position = 0;
                        response.Items.Add(NetUtils.ToNet(item));
                    }
                }

                // Find the item to equip
                ItemData? targetItem = user.Items.FirstOrDefault(x => x.Isn == item2);
                if (targetItem != null)
                {
                    // Handle case where we have multiple copies of the same item
                    ItemData? equippedItem = null;
                    if (targetItem.Count > 1)
                    {
                        // Reduce count of original item
                        targetItem.Count--;
                        response.Items.Add(NetUtils.ToNet(targetItem));

                        // Create a new item instance to equip
                        equippedItem = new ItemData
                        {
                            ItemType = targetItem.ItemType,
                            Isn = user.GenerateUniqueItemId(),
                            Level = targetItem.Level,
                            Exp = targetItem.Exp,
                            Count = 1,
                            Corp = targetItem.Corp,
                            Position = pos // Set the position for the new item
                        };
                        user.Items.Add(equippedItem);
                    }
                    else
                    {
                        // Use the existing item
                        equippedItem = targetItem;
                    }

                    // equip the item
                    equippedItem.Csn = req.Csn;
                    equippedItem.Position = pos;
                    response.Items.Add(NetUtils.ToNet(equippedItem));
                }
            }

            // Ensure all requested items are in the response
            // This helps the client track the specific items that were requested
            foreach (long requestedIsn in req.IsnList)
            {
                bool requestedItemAdded = response.Items.Any(x => x.Isn == requestedIsn);
                if (!requestedItemAdded)
                {
                    ItemData? requestedItem = user.Items.FirstOrDefault(x => x.Isn == requestedIsn);
                    if (requestedItem != null)
                    {
                        response.Items.Add(NetUtils.ToNet(requestedItem));
                    }
                    else
                    {
                        // If item not found, add it with count 0 to indicate it was processed
                        response.Items.Add(new NetUserItemData()
                        {
                            Isn = requestedIsn,
                            Count = 0
                        });
                    }
                }
            }

            // Add all other equipped items for this character to the response
            // This helps the client synchronize the full equipment state
            foreach (ItemData item in user.Items)
            {
                if (item.Csn == req.Csn && item.Csn != 0)
                {
                    // Check if this item was already added in the loop above
                    bool alreadyAdded = response.Items.Any(x => x.Isn == item.Isn);
                    if (!alreadyAdded)
                    {
                        response.Items.Add(NetUtils.ToNet(item));
                    }
                }
            }

            JsonDb.Save();

            await WriteDataAsync(response);
        }
    }
}
