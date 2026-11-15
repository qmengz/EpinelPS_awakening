using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Character.Counsel;

/// <summary>
/// 角色好感度（Attractive / Counsel）业务逻辑的静态辅助类。
/// 六个 [GameRequest] 入口只负责「读请求 → 调用本类 → 写回响应」，
/// 所有存档修改、升级计算与 JsonDb.Save() 都集中在这里。
/// 本类不保存任何请求状态，玩家、请求、数据库上下文全部由方法参数传入。
/// </summary>
public static class AttractiveHelper
{
    /// <summary>
    /// 判断某段咨询对话（CounselTid）此前是否已经做过，供两个 check 接口共用。
    /// 做法：遍历玩家所有角色的好感度档案，只要其中一条的
    /// CounselDialogCompleteIds 含有该 ID，就认为做过。
    /// </summary>
    /// <param name="user">玩家存档，只读。</param>
    /// <param name="req">请求，使用其中的 CounselTid（咨询组 ID）。</param>
    /// <returns>
    /// ResCounseledBefore：IsCounseledBefore 为 true 表示之前做过，false 表示没做过。
    /// 本方法不修改存档、不写盘。
    /// </returns>
    public static ResCounseledBefore BuildCounseledBeforeResponse(User user, ReqCounseledBefore req)
    {
        bool answered = user.BondInfo.Any(x => x.CounselDialogCompleteIds.Contains(req.CounselTid));
        return new ResCounseledBefore { IsCounseledBefore = answered };
    }

    /// <summary>
    /// 列出指定角色已完成（拿到过好感点）的咨询对话 ID，
    /// 客户端据此显示完成标记、避免重复出题。
    /// </summary>
    /// <param name="user">玩家存档，只读。</param>
    /// <param name="req">请求，使用其中的 NameCode（角色内部编号）。</param>
    /// <returns>
    /// ResListAnsweredAttractiveCounsel：CorrectlyAnsweredAttractiveCounselTableIdList
    /// 里是已完成对话的 ID 列表；该角色还没有好感度档案时返回空列表。
    /// 本方法不写盘。
    /// </returns>
    /// <remarks>
    /// 字段名带「CorrectlyAnswered（正确回答）」，但服务端记录的是「拿到过好感点的节点」，
    /// 两者语义并不完全一致。
    /// </remarks>
    public static ResListAnsweredAttractiveCounsel ListAnswered(User user, ReqListAnsweredAttractiveCounsel req)
    {
        ResListAnsweredAttractiveCounsel response = new();

        NetUserAttractiveData? bondInfo = user.BondInfo.FirstOrDefault(x => x.NameCode == req.NameCode);
        if (bondInfo != null)
        {
            response.CorrectlyAnsweredAttractiveCounselTableIdList.AddRange(bondInfo.CounselDialogCompleteIds);
        }

        return response;
    }

    /// <summary>
    /// 处理「正式做完一段咨询」：按官方对话表算出本次好感点，写入角色档案并存盘。
    /// 步骤：消耗一次每日咨询机会 → 回显玩家货币 → 解析本次得分（0 / 100 / 120）
    /// → 找到或新建该角色的好感度档案 → 累计咨询次数、记录已完成对话、加经验并升级
    /// → 满级时写触发记录 → JsonDb.Save()。角色已达到好感度上限时不再累加经验。
    /// </summary>
    /// <param name="user">玩家存档，会被修改（BondInfo、每日咨询次数等）。</param>
    /// <param name="req">请求，含 NameCode、CounselTid、SelectTid（玩家选中的对话节点）。</param>
    /// <param name="db">当前请求的数据库上下文，用于查重并写入满级触发记录。</param>
    /// <returns>
    /// ResCharacterCounsel：Attractive 为更新后的好感度档案，Exp 为升级前后对比
    /// （含 GainExp 本次获得点数），Currencies 为玩家全部货币的回显（不扣钱）。
    /// </returns>
    /// <remarks>
    /// 查不到官方数据表时会降级为固定 +100，并打一条 Warning 日志。
    /// 会先调用 ResetDataIfNeeded() 处理跨天重置，并消耗一次每日咨询次数；
    /// CanCounselToday 按扣减后的剩余次数设置。服务端不做硬拦截，
    /// 次数为 0 时由客户端依据剩余次数禁止发起咨询。
    /// </remarks>
    public static ResCharacterCounsel ApplyCounsel(User user, ReqCharacterCounsel req, GameContext db)
    {
        ResCharacterCounsel response = new();
        int remainingCounselCount = ConsumeCounselCount(user);

        foreach (KeyValuePair<CurrencyType, long> currency in user.Currency)
        {
            response.Currencies.Add(new NetUserCurrencyData() { Type = (int)currency.Key, Value = currency.Value });
        }

        NetUserAttractiveData? currentBondInfo = user.BondInfo.FirstOrDefault(x => x.NameCode == req.NameCode);
        (int gain, bool completed) = ResolveCounselResult(currentBondInfo?.Lv ?? 1, req);

        if (currentBondInfo != null)
        {
            int beforeLv = currentBondInfo.Lv;
            int beforeExp = currentBondInfo.Exp;

            currentBondInfo.CounseledCount++;
            if (completed && !currentBondInfo.CounselDialogCompleteIds.Contains(req.CounselTid))
                currentBondInfo.CounselDialogCompleteIds.Add(req.CounselTid);
            currentBondInfo.CanCounselToday = remainingCounselCount > 0;
            if (currentBondInfo.Lv < user.GetMaxAttractiveLevel(req.NameCode))
            {
                currentBondInfo.Exp += gain;
                UpdateAttractiveLevel(currentBondInfo);
            }

            AddMaxLevelTrigger(user, req.NameCode, currentBondInfo.Lv, db);

            response.Attractive = currentBondInfo;
            response.Exp = new NetIncreaseExpData
            {
                NameCode = currentBondInfo.NameCode,
                BeforeLv = beforeLv,
                BeforeExp = beforeExp,
                CurrentLv = currentBondInfo.Lv,
                CurrentExp = currentBondInfo.Exp,
                GainExp = gain
            };
        }
        else
        {
            NetUserAttractiveData data = new()
            {
                NameCode = req.NameCode,
                Exp = gain,
                CounseledCount = 1,
                IsFavorites = false,
                CanCounselToday = remainingCounselCount > 0,
                Lv = 1
            };
            if (completed) data.CounselDialogCompleteIds.Add(req.CounselTid);
            UpdateAttractiveLevel(data);
            user.BondInfo.Add(data);
            response.Attractive = data;
            response.Exp = new NetIncreaseExpData
            {
                NameCode = data.NameCode,
                BeforeLv = 1,
                BeforeExp = 0,
                CurrentLv = 1,
                CurrentExp = gain,
                GainExp = gain
            };
        }

        JsonDb.Save();

        return response;
    }

    /// <summary>
    /// 处理「一键咨询 / 跳过对话」：不播放对话，直接给该角色固定 100 点好感经验并存盘。
    /// 与 ApplyCounsel 的区别：请求里没有 SelectTid，无法查官方对话表，也不记录
    /// 已完成对话 ID（没有具体对话，记了反而会误标记）。
    /// 同样会消耗一次每日咨询机会、按剩余次数设置 CanCounselToday，
    /// 并在好感度满级时写触发记录，和正式咨询保持一致。
    /// </summary>
    /// <param name="user">玩家存档，会被修改（BondInfo、每日咨询次数）。</param>
    /// <param name="req">请求，只使用其中的 NameCode。</param>
    /// <param name="db">当前请求的数据库上下文，用于写满级触发记录。</param>
    /// <returns>
    /// ResCharacterQuickCounsel：Attractive 为更新后的档案，Exp 为升级前后对比
    /// （GainExp 固定 100），Currencies 为玩家全部货币的回显。
    /// </returns>
    public static ResCharacterQuickCounsel ApplyQuickCounsel(User user, ReqCharacterQuickCounsel req, GameContext db)
    {
        ResCharacterQuickCounsel response = new();
        int remainingCounselCount = ConsumeCounselCount(user);

        foreach (KeyValuePair<CurrencyType, long> currency in user.Currency)
        {
            response.Currencies.Add(new NetUserCurrencyData() { Type = (int)currency.Key, Value = currency.Value });
        }

        NetUserAttractiveData? bondInfo = user.BondInfo.FirstOrDefault(x => x.NameCode == req.NameCode);

        if (bondInfo != null)
        {
            int beforeLv = bondInfo.Lv;
            int beforeExp = bondInfo.Exp;

            bondInfo.CounseledCount++;
            bondInfo.CanCounselToday = remainingCounselCount > 0;
            if (bondInfo.Lv < user.GetMaxAttractiveLevel(req.NameCode))
            {
                bondInfo.Exp += 100;
                UpdateAttractiveLevel(bondInfo);
            }

            response.Attractive = bondInfo;
            response.Exp = new NetIncreaseExpData
            {
                NameCode = bondInfo.NameCode,
                BeforeLv = beforeLv,
                BeforeExp = beforeExp,
                CurrentLv = bondInfo.Lv,
                CurrentExp = bondInfo.Exp,
                GainExp = 100
            };
        }
        else
        {
            NetUserAttractiveData data = new()
            {
                NameCode = req.NameCode,
                Exp = 100,
                CounseledCount = 1,
                IsFavorites = false,
                CanCounselToday = remainingCounselCount > 0,
                Lv = 1
            };
            UpdateAttractiveLevel(data);
            user.BondInfo.Add(data);
            response.Attractive = data;
            response.Exp = new NetIncreaseExpData
            {
                NameCode = data.NameCode,
                BeforeLv = 1,
                BeforeExp = 0,
                CurrentLv = 1,
                CurrentExp = 100,
                GainExp = 100
            };
        }

        AddMaxLevelTrigger(user, req.NameCode, response.Attractive.Lv, db);

        JsonDb.Save();

        return response;
    }

    /// <summary>
    /// 处理「送礼物」：计算好感点、扣掉背包里的礼物、加经验升级，并返回扣完后的背包。
    /// 只认 ItemSubType.AttractiveMaterial 的好感度道具；基础点数 = ItemValue 乘以数量。
    /// 企业礼物用 MaterialValue 与角色企业精确比对，命中乘以 5；
    /// 部队礼物按本地化键里的部队名比对，命中乘以 3；
    /// 通用礼物（MaterialType.All）本身基础点数更高，不加成。
    /// 送礼不消耗每日咨询次数。
    /// </summary>
    /// <param name="user">玩家存档，会被修改（背包、好感度档案）。</param>
    /// <param name="req">请求，含 NameCode 与要送的道具列表 Items。</param>
    /// <param name="db">当前请求的数据库上下文，用于写满级触发记录。</param>
    /// <returns>
    /// ResCharacterPresent：Attractive 为更新后的档案，Exp 为升级前后对比，
    /// Items 为扣完道具后的完整背包。角色还没有好感度档案时返回空响应与当前背包
    /// （不再不回包）；已到好感度上限时 GainExp 为 0、不扣道具也不加经验。
    /// </returns>
    public static ResCharacterPresent ApplyPresent(User user, ReqCharacterPresent req, GameContext db)
    {
        ResCharacterPresent response = new();
        response.Items.AddRange(NetUtils.GetUserItems(user));

        NetUserAttractiveData? bondInfo = user.BondInfo.FirstOrDefault(x => x.NameCode == req.NameCode);
        if (bondInfo == null)
        {
            return response;
        }

        int beforeLv = bondInfo.Lv;
        int beforeExp = bondInfo.Exp;
        int totalExpGained = 0;
        bool canGainExp = bondInfo.Lv < user.GetMaxAttractiveLevel(req.NameCode);

        if (canGainExp)
        {
            CharacterRecord? characterRecord = GameData.Instance.CharacterTable.Values.FirstOrDefault(x => x.NameCode == req.NameCode);

            foreach (NetItemData item in req.Items)
            {
                ItemMaterialRecord? materialInfo = GameData.Instance.itemMaterialTable.GetValueOrDefault(item.Tid);
                if (materialInfo == null || materialInfo.ItemSubType != ItemSubType.AttractiveMaterial) continue;
                if (item.Count <= 0) continue;

                DbItemData? userItem = user.Items.FirstOrDefault(x => x.ItemType == item.Tid);
                if (userItem == null || userItem.Count < item.Count) continue;

                int expGained = materialInfo.ItemValue * item.Count;
                expGained *= GetMaterialBonus(materialInfo, characterRecord);
                totalExpGained += expGained;

                userItem.Count -= item.Count;
                if (userItem.Count <= 0)
                {
                    user.Items.Remove(userItem);
                }
            }

            bondInfo.Exp += totalExpGained;
            UpdateAttractiveLevel(bondInfo);
        }

        AddMaxLevelTrigger(user, req.NameCode, bondInfo.Lv, db);

        response.Attractive = bondInfo;
        response.Exp = new NetIncreaseExpData
        {
            NameCode = bondInfo.NameCode,
            BeforeLv = beforeLv,
            BeforeExp = beforeExp,
            CurrentLv = bondInfo.Lv,
            CurrentExp = bondInfo.Exp,
            GainExp = totalExpGained
        };

        response.Items.Clear();
        response.Items.AddRange(NetUtils.GetUserItems(user));

        JsonDb.Save();

        return response;
    }

    /// <summary>
    /// 按官方 ItemMaterialTable 计算送礼的加成倍率（命中返回 5 或 3，否则返回 1）。
    /// 企业礼物的 MaterialValue 正好等于 CorporationType 枚举数值，直接数值比对；
    /// 部队礼物的 MaterialValue 是 SquadInfoTable 的 id，查表拿到 Squad 枚举后与角色比对，
    /// 表缺失或查不到时回退到本地化键里的部队名做忽略大小写的比对（段数不足则跳过）。
    /// 其余类型（含通用礼物 All、以及不属于好感度道具的 FavoriteItem）返回 1。
    /// </summary>
    /// <param name="material">道具表里的好感度礼物记录。</param>
    /// <param name="character">收礼角色的静态资料；查不到时返回 1。</param>
    /// <returns>加成倍率：企业命中 5、部队命中 3、其余 1。</returns>
    private static int GetMaterialBonus(ItemMaterialRecord material, CharacterRecord? character)
    {
        if (character == null) return 1;

        // 是否是企业礼物
        if (material.MaterialType == MaterialType.Corporation)
        {
            return material.MaterialValue == (int)character.Corporation ? 5 : 1;
        }

        // 是否是部队礼物
        if (material.MaterialType == MaterialType.Squad)
        {
            // 先查表
            if (GameData.Instance.SquadInfoTable.TryGetValue(material.MaterialValue, out var squadInfo)
                && squadInfo.Squad == character.Squad)
            {
                return 3;
            }

            // 表里没，再查本地化键
            string[] parts = (material.NameLocalkey ?? string.Empty).Split('_');
            if (parts.Length > 2)
            {
                return parts[2].Equals(character.Squad.ToString(), StringComparison.OrdinalIgnoreCase) ? 3 : 1;
            }
        }

        return 1;
    }

    /// <summary>
    /// 消耗一次每日咨询机会，返回扣减后的剩余次数。
    /// 先调用 user.ResetDataIfNeeded()：若已跨过每日重置点，它会自动把次数补成
    /// 「基础 3 次 + 前哨基地等级加成」；否则保持原值，不会重复补满。
    /// 次数从 user.ResetableData.DailyCounselCount[1] 读取（老存档没有该键时按 3 兜底），
    /// 只有大于 0 才减 1，避免出现负数。
    /// </summary>
    /// <param name="user">玩家存档，会被修改（每日咨询次数）。</param>
    /// <returns>扣减后剩余的咨询次数；已用完时为 0。</returns>
    /// <remarks>
    /// 本方法不写盘，调用方后续已有 JsonDb.Save()。
    /// 服务端不做硬拦截：次数为 0 时，由客户端依据剩余次数禁止发起咨询。
    /// </remarks>
    private static int ConsumeCounselCount(User user)
    {
        user.ResetDataIfNeeded();

        int remaining = user.ResetableData.DailyCounselCount.GetValueOrDefault(1, 3);
        if (remaining > 0)
        {
            remaining--;
        }

        user.ResetableData.DailyCounselCount[1] = remaining;
        return remaining;
    }

    /// <summary>
    /// 按官方静态表解析「这次咨询该给多少好感点、算不算完成」。
    /// 用 CounselTid 查主表得到咨询组 ID 与可用等级范围，用 SelectTid 查对话分片
    /// 得到该节点的 attractive_point；校验两者组 ID 一致、当前等级在范围内。
    /// </summary>
    /// <param name="currentLv">角色当前好感度等级（还没有档案时按 1 级算）。</param>
    /// <param name="req">请求，含 CounselTid 与 SelectTid。</param>
    /// <returns>
    /// 二元组 (Gain, Completed)：
    /// Gain 是本次应加的好感点（普通对白 0、选项 100 或 120；查不到数据时降级为 100）；
    /// Completed 表示是否要把 CounselTid 记进已完成对话列表（Gain 大于 0 时为 true）。
    /// </returns>
    /// <remarks>查不到数据或等级不符时会打 Warning 日志；本方法不修改任何存档。</remarks>
    private static (int Gain, bool Completed) ResolveCounselResult(int currentLv, ReqCharacterCounsel req)
    {
        AttractiveCounselRecord? counsel = GameData.Instance.AttractiveCounselTable.GetValueOrDefault(req.CounselTid);
        AttractiveCounselDialogRecord? dialog = GameData.Instance.CounselDialogs.GetValueOrDefault(req.SelectTid);

        if (counsel is null || dialog is null || dialog.GroupId != counsel.CounselGroupId)
        {
            Logging.WriteLine($"Counsel data missing for CounselTid={req.CounselTid}, SelectTid='{req.SelectTid}'; falling back to +100", LogType.Warning);
            return (100, true);
        }

        if (currentLv < counsel.AttractiveLevelMin || currentLv > counsel.AttractiveLevelMax)
        {
            Logging.WriteLine($"Counsel {req.CounselTid} requires level {counsel.AttractiveLevelMin}-{counsel.AttractiveLevelMax}, current {currentLv}", LogType.Warning);
            return (0, false);
        }

        int gain = (int)dialog.AttractivePoint;
        return (gain, gain > 0);
    }

    /// <summary>
    /// 角色好感度达到上限时，写一条 CharacterAttractiveLevelMax 触发记录，
    /// 供前哨基地等「需要某角色好感度满级」的事件判定使用；已存在则不重复写。
    /// </summary>
    /// <param name="user">玩家存档，触发记录会写到其关联的触发表中。</param>
    /// <param name="nameCode">角色内部编号。</param>
    /// <param name="currentLv">角色当前好感度等级。</param>
    /// <param name="db">数据库上下文，用于查询是否已有相同触发记录。</param>
    /// <returns>无返回值（void）；可能向触发表写入一条记录。</returns>
    private static void AddMaxLevelTrigger(User user, int nameCode, int currentLv, GameContext db)
    {
        // Outpost events can require a character's attractive level to be maxed
        if (currentLv >= user.GetMaxAttractiveLevel(nameCode) &&
            !db.Triggers.Any(t => t.UserId == user.ID && t.Type == Trigger.CharacterAttractiveLevelMax && t.ConditionId == nameCode))
        {
            user.AddTrigger(Trigger.CharacterAttractiveLevelMax, currentLv, nameCode);
        }
    }

    /// <summary>
    /// 按 AttractiveLevelTable 把累计的好感点兑换成等级：达到当前等级所需的
    /// AttractivePoint 就扣掉点数并升一级，直到点数不够、没有下一级数据或到 40 级。
    /// </summary>
    /// <param name="attractiveData">要升级的好感度档案，其 Lv 与 Exp 会被直接修改。</param>
    /// <returns>无返回值（void）；结果直接反映在传入对象的 Lv 与 Exp 上。</returns>
    private static void UpdateAttractiveLevel(NetUserAttractiveData attractiveData)
    {
        while (attractiveData.Lv < 40)
        {
            AttractiveLevelRecord? levelInfo = GameData.Instance.AttractiveLevelTable.Values.FirstOrDefault(x => x.AttractiveLevel == attractiveData.Lv);

            if (levelInfo == null)
            {
                break;
            }

            if (attractiveData.Exp >= levelInfo.AttractivePoint)
            {
                attractiveData.Exp -= levelInfo.AttractivePoint;
                attractiveData.Lv++;
            }
            else
            {
                break;
            }
        }
    }
}
