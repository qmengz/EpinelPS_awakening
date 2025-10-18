using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Character
{
    [PacketPath("/character/synchrodevice/deviceoneclick")]
    public class SynchroDeviceOneClick : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // { "synchroLv": 1001 }
            ReqSynchroDeviceOneClick req = await ReadData<ReqSynchroDeviceOneClick>();
            User user = GetUser();
            ResSynchroDeviceOneClick response = new();

            user.SynchroDeviceLevel = req.SynchroLv;
            response.SynchroLv = req.SynchroLv;

            user.AddTrigger(Trigger.CharacterLevelUpCount, 1);

            JsonDb.Save();
            await WriteDataAsync(response);
        }
    }
}
