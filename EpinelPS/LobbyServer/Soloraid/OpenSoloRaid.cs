using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;
using Newtonsoft.Json;

namespace EpinelPS.LobbyServer.Soloraid;

[PacketPath("/soloraid/open")]
public class OpenSoloRaid : LobbyMsgHandler
{
    protected override async Task HandleAsync()
    {
        // { "raidId": 1000030, "raidLevel": 1 }
        ReqOpenSoloRaid req = await ReadData<ReqOpenSoloRaid>();
        User user = GetUser();

        // Create a unique key for the raid and level
        long key = long.Parse($"{req.RaidId}{req.RaidLevel:D2}");
        int statEnhanceId = 230000;
        long levelHp = 0;
        if (GameData.Instance.SoloRaidManagerTable.TryGetValue(req.RaidId, out SoloRaidManagerRecord? manager))
        {
            SoloRaidPresetRecord? presetData = GameData.Instance.SoloRaidPresetTable.Values.FirstOrDefault(r => r.PresetGroupId == manager.MonsterPreset && r.WaveOrder == req.RaidLevel);
            var statEnhance = GameData.Instance.MonsterStatEnhanceTable.Values.Where(m => m.Lv == presetData.MonsterStageLv && m.GroupId == statEnhanceId);
            if (statEnhance != null)
            {
                levelHp = statEnhance.Sum(m => m.LevelHp);
            }
        }

        if (user.SoloRaidData.TryGetValue(key, out _))
        {
            user.SoloRaidData[key].RaidOpenCount += 1;
            user.SoloRaidData[key].Hp = levelHp;
            user.SoloRaidData[key].TotalDamage = 0;
        }
        else
        {
            user.SoloRaidData.Add(key, new SoloRaidInfo
            {
                RaidId = req.RaidId,
                RaidLevel = req.RaidLevel,
                RaidOpenCount = 1,
                Hp = levelHp,
                TotalDamage = 0,
                PeriodResult = SoloRaidPeriodResult.Success,
            });
        }

        ResOpenSoloRaid response = new()
        {
            PeriodResult = SoloRaidPeriodResult.Success,
            RaidOpenCount = 1,
        };

        JsonDb.Save();
        await WriteDataAsync(response);
    }
}