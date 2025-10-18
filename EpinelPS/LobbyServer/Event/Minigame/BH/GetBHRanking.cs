using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Event.Minigame.BH
{
    [PacketPath("/event/minigame/bh/ranking/get")]
    public class GetBHRanking : LobbyMsgHandler
    {
        protected override async Task HandleAsync()
        {
            ReqGetMiniGameBHRanking req = await ReadData<ReqGetMiniGameBHRanking>();
            User user = GetUser();

            ResGetMiniGameBHRanking response = new();
            // userGuildRanking
            response.UserGuildRanking = new NetMiniGameBHRankingData()
            {
                // rank
                Rank = 0,
                // score
                Score = 0,
                // user
                User = new NetWholeUserData
                {
                    Usn = (long)user.ID,
                    Server = 1000,
                    Nickname = user.Nickname,
                    Lv = user.userPointData.UserLevel,
                    Icon = user.ProfileIconId,
                    IconPrism = user.ProfileIconIsPrism,
                    Frame = user.ProfileFrame,
                    TeamCombat = 1,
                    LastActionAt = user.LastLogin.Ticks,
                    UserTitleId = user.TitleId,
                },
            };
            // guildRankingList
            response.GuildRankingList.AddRange(new List<NetMiniGameBHRankingData>());

            await WriteDataAsync(response);
        }
    }
}