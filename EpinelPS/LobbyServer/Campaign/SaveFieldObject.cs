using EpinelPS.Database;
using EpinelPS.Data;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Campaign
{
    [PacketPath("/campaign/savefieldobject")]
    public class SaveFieldObject : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqSaveCampaignFieldObject req = await ReadData<ReqSaveCampaignFieldObject>();
            User user = GetUser();

            ResSaveCampaignFieldObject response = new();

            Logging.WriteLine($"save {req.MapId} with {req.FieldObject.PositionId}", LogType.Debug);

            // Save collected objects
            if (user.FieldInfoNew.TryGetValue(req.MapId, out FieldInfoNew? field))
            {
                // Remove any existing object with the same PositionId, Type, and Json before adding the new one
                field.CompletedObjects.RemoveAll(obj => obj.PositionId == req.FieldObject.PositionId && obj.Type == req.FieldObject.Type && obj.Json == req.FieldObject.Json);
                field.CompletedObjects.Add(new NetFieldObject() { PositionId = req.FieldObject.PositionId, Json = req.FieldObject.Json, Type = req.FieldObject.Type });
            }
            else
            {
                // If the map doesn't exist, create a new FieldInfoNew and add it to the user's FieldInfoNew dictionary
                field = new FieldInfoNew();
                user.FieldInfoNew.Add(req.MapId, field);
            }
           
            JsonDb.Save();

            await WriteDataAsync(response);
        }
    }
}
