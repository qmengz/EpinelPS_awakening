using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
using log4net;
using Newtonsoft.Json;

namespace EpinelPS.LobbyServer.Outpost.Recycle
{
    [PacketPath("/outpost/RecycleRoom/PersonalResearchLevelUp")]
    public class PersonalResearchLevelUp : LobbyMsgHandler
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(PersonalResearchLevelUp));

        protected override async Task HandleAsync()
        {
            /*
             * Req Contains:
             * Tid: int value, research tId
             * Items: int value, used items.
             */
            ReqPersonalResearchRecycleLevelUp req = await ReadData<ReqPersonalResearchRecycleLevelUp>();
            User user = GetUser();
            ResPersonalResearchRecycleLevelUp response = new();

            // Find the personal research stat record
            RecycleResearchStatRecord? personalResearchStat = GameData.Instance.RecycleResearchStats.Values.FirstOrDefault(e => e.RecycleType == RecycleType.Personal);
            if (personalResearchStat != null)
            {
                log.Debug($"PersonalResearchLevelUp: Found personal research stat - {JsonConvert.SerializeObject(personalResearchStat)}");
                user.ResearchProgress.TryGetValue(personalResearchStat.Id, out RecycleRoomResearchProgress? progress);
                // Check progress is null, null means research is not unlocked.
                if (progress != null)
                {
                    log.Debug($"PersonalResearchLevelUp: Found research progress - {JsonConvert.SerializeObject(progress)}");
                    progress.Level += req.LevelUpCount;
                    progress.Hp += req.LevelUpCount * personalResearchStat.Hp;
                    response.Recycle = new NetUserRecycleRoomData
                    {
                        Tid = personalResearchStat.Id,
                        Lv = progress.Level,
                    };
                    log.Debug($"PersonalResearchLevelUp: Updated research progress - {JsonConvert.SerializeObject(progress)}");
                }
            }

            JsonDb.Save();

            await WriteDataAsync(response);
        }
    }
}
