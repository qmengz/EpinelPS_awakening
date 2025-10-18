using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Intercept
{
    [PacketPath("/intercept/getclearinfo")]
    public class GetInterceptClearInfo : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            // { "intercept": 3, "interceptId": 5, "filterType": 2, "orderType": 1 }
            ReqGetInterceptClearInfo req = await ReadData<ReqGetInterceptClearInfo>();

            User user = GetUser();

            ResGetInterceptClearInfo response = new();

            if (user.FinishInterceptAnomalous.TryGetValue(req.InterceptId, out ReqFinishInterceptAnomalous? other))
            {
                List<NetInterceptClearInfoTeam> slots = [];

                foreach(var character in other.AntiCheatBattleData.Characters)
                {
                    slots.Add(new NetInterceptClearInfoTeam()
                    {
                        Slot = character.Slot,
                        Tid = character.Tid,
                        Lv = character.CharacterSpec.Level,
                        Combat = (int)character.CharacterSpec.Combat,
                    });
                }

                response.Histories.Add(new NetInterceptClearInfo()
                {
                    User = new NetWholeUserData
                    {
                        Usn = (long)user.ID,
                        Server = 1001,
                        Nickname = user.Nickname,
                        Lv = user.userPointData.UserLevel,
                        Icon = user.ProfileIconId,
                        IconPrism = user.ProfileIconIsPrism,
                        Frame = user.ProfileFrame,
                        UserTitleId = user.TitleId,
                    },
                    TeamCombat = 1,
                    ClearedAt = other.AntiCheatBattleTLogAdditionalInfo.ClientLocalTime,
                    Slots = { slots },
                    Step = 1,
                    Damage = other.DamageDealt
                });
            }


            await WriteDataAsync(response);
        }
    }
}