using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Character
{
    [PacketPath("/character/synchrodevice/registoneclick")]
    public class SynchroRegisterOneClick : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqSynchroRegisterOneClick req = await ReadData<ReqSynchroRegisterOneClick>();
            User user = GetUser();

            ResSynchroRegisterOneClick response = new();

            response.Slots.AddRange(user.SynchroSlots.Where(item => item.CharacterSerialNumber != 0).Select(item => new NetSynchroSlot()
            {
                Slot = item.Slot,
                Csn = item.CharacterSerialNumber,
                AvailableRegisterAt = item.AvailableAt,
            }));
            response.IsSynchro = false;


            await WriteDataAsync(response);
        }
    }   
}