using EpinelPS.Data;
using EpinelPS.Database;
using EpinelPS.Utils;

namespace EpinelPS.LobbyServer.Character.Counsel;

/// <summary>
/// Static helper for character bond (Attractive / Counsel) business logic.
/// This class is stateless: user, request and database context are passed as
/// method arguments.
/// </summary>
public static class AttractiveHelper
{
    /// <summary>Checks whether a counsel dialog was completed before.</summary>
    /// <param name="user">Player save data (read only).</param>
    /// <param name="req">Request carrying the CounselTid.</param>
    /// <returns>ResCounseledBefore.IsCounseledBefore; no save-data changes.</returns>
    public static ResCounseledBefore BuildCounseledBeforeResponse(User user, ReqCounseledBefore req)
    {
        // Any bond entry containing this dialog ID counts as completed.
        bool answered = user.BondInfo.Any(x => x.CounselDialogCompleteIds.Contains(req.CounselTid));
        return new ResCounseledBefore { IsCounseledBefore = answered };
    }

    /// <summary>Lists a character's completed counsel dialog IDs.</summary>
    /// <param name="user">Player save data (read only).</param>
    /// <param name="req">Request carrying the NameCode.</param>
    /// <returns>ResListAnsweredAttractiveCounsel with the ID list; empty when no bond entry exists.</returns>
    public static ResListAnsweredAttractiveCounsel ListAnswered(User user, ReqListAnsweredAttractiveCounsel req)
    {
        ResListAnsweredAttractiveCounsel response = new();

        NetUserAttractiveData? bondInfo = user.BondInfo.FirstOrDefault(x => x.NameCode == req.NameCode);
        if (bondInfo != null)
        {
            // The proto field says "CorrectlyAnswered"; we store nodes that granted points.
            response.CorrectlyAnsweredAttractiveCounselTableIdList.AddRange(bondInfo.CounselDialogCompleteIds);
        }

        return response;
    }

    /// <summary>Completes a counsel session and persists the bond-point gain.</summary>
    /// <param name="user">Player save data (BondInfo, daily count).</param>
    /// <param name="req">Request with NameCode, CounselTid and SelectTid.</param>
    /// <param name="db">Database context for the max-level trigger.</param>
    /// <returns>Updated bond entry, EXP before/after and a currency echo.</returns>
    public static ResCharacterCounsel ApplyCounsel(User user, ReqCharacterCounsel req, GameContext db)
    {
        ResCharacterCounsel response = new();

        // One global attempt is consumed; crossing a reset point refills it first.
        ConsumeCounselCount(user);

        foreach (KeyValuePair<CurrencyType, long> currency in user.Currency)
        {
            response.Currencies.Add(new NetUserCurrencyData() { Type = (int)currency.Key, Value = currency.Value });
        }

        NetUserAttractiveData? currentBondInfo = user.BondInfo.FirstOrDefault(x => x.NameCode == req.NameCode);

        // Resolve the gain from the official tables instead of a fixed value.
        (int gain, bool completed) = ResolveCounselResult(currentBondInfo?.Lv ?? 1, req);

        if (currentBondInfo != null)
        {
            int beforeLv = currentBondInfo.Lv;
            int beforeExp = currentBondInfo.Exp;

            currentBondInfo.CounseledCount++;
            if (completed && !currentBondInfo.CounselDialogCompleteIds.Contains(req.CounselTid))
                currentBondInfo.CounselDialogCompleteIds.Add(req.CounselTid);

            // Each character can be advised only once per day.
            currentBondInfo.CanCounselToday = false;

            // No EXP is granted once the bond level cap is reached.
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
            // First counsel for this character: create the bond entry.
            NetUserAttractiveData data = new()
            {
                NameCode = req.NameCode,
                Exp = gain,
                CounseledCount = 1,
                IsFavorites = false,
                CanCounselToday = false,
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

    /// <summary>Skips the dialog and grants a fixed 100 bond EXP.</summary>
    /// <param name="user">Player save data (BondInfo, daily count).</param>
    /// <param name="req">Request; only NameCode is used.</param>
    /// <param name="db">Database context for the max-level trigger.</param>
    /// <returns>Updated bond entry, EXP before/after and a currency echo.</returns>
    public static ResCharacterQuickCounsel ApplyQuickCounsel(User user, ReqCharacterQuickCounsel req, GameContext db)
    {
        // Quick Advise has no SelectTid: no dialog lookup, no completed dialog ID.
        ResCharacterQuickCounsel response = new();
        ConsumeCounselCount(user);

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

            // Same per-character daily limit as the normal counsel flow.
            bondInfo.CanCounselToday = false;
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
                CanCounselToday = false,
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

    /// <summary>Handles gift giving: points, item deduction and level-ups.</summary>
    /// <param name="user">Player save data (inventory, bond entry).</param>
    /// <param name="req">Request with NameCode and the items to give.</param>
    /// <param name="db">Database context for the max-level trigger.</param>
    /// <returns>Updated bond entry, EXP before/after and the inventory after deduction.</returns>
    public static ResCharacterPresent ApplyPresent(User user, ReqCharacterPresent req, GameContext db)
    {
        // Always answer, even without a bond entry, so the client never hangs.
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

        // At the cap: no EXP gain and, importantly, no items consumed.
        bool canGainExp = bondInfo.Lv < user.GetMaxAttractiveLevel(req.NameCode);

        if (canGainExp)
        {
            CharacterRecord? characterRecord = GameData.Instance.CharacterTable.Values.FirstOrDefault(x => x.NameCode == req.NameCode);

            foreach (NetItemData item in req.Items)
            {
                ItemMaterialRecord? materialInfo = GameData.Instance.itemMaterialTable.GetValueOrDefault(item.Tid);

                // Only bond gifts count; skip unknown, zero or insufficient items.
                if (materialInfo == null || materialInfo.ItemSubType != ItemSubType.AttractiveMaterial) continue;
                if (item.Count <= 0) continue;

                DbItemData? userItem = user.Items.FirstOrDefault(x => x.ItemType == item.Tid);
                if (userItem == null || userItem.Count < item.Count) continue;

                // Base value times quantity, then the corporation/squad multiplier.
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

        // Refresh the returned inventory after the deductions above.
        response.Items.Clear();
        response.Items.AddRange(NetUtils.GetUserItems(user));

        JsonDb.Save();

        return response;
    }

    /// <summary>Returns the gift multiplier (5 corporation, 3 squad, else 1).</summary>
    /// <param name="material">Bond gift record from the item table.</param>
    /// <param name="character">Recipient static data; null yields 1.</param>
    /// <returns>The multiplier applied to the base points.</returns>
    private static int GetMaterialBonus(ItemMaterialRecord material, CharacterRecord? character)
    {
        if (character == null) return 1;

        // Corporation gift: MaterialValue is the CorporationType value.
        if (material.MaterialType == MaterialType.Corporation)
        {
            return material.MaterialValue == (int)character.Corporation ? 5 : 1;
        }

        // Squad gift: MaterialValue is a SquadInfoTable id, not the Squad enum.
        if (material.MaterialType == MaterialType.Squad)
        {
            // Prefer the static table lookup.
            if (GameData.Instance.SquadInfoTable.TryGetValue(material.MaterialValue, out var squadInfo)
                && squadInfo.Squad == character.Squad)
            {
                return 3;
            }

            // Fall back to the squad name in the localization key.
            string[] parts = (material.NameLocalkey ?? string.Empty).Split('_');
            if (parts.Length > 2)
            {
                return parts[2].Equals(character.Squad.ToString(), StringComparison.OrdinalIgnoreCase) ? 3 : 1;
            }
        }

        // Universal gifts and all other types get no multiplier.
        return 1;
    }

    /// <summary>Consumes one global daily counsel attempt.</summary>
    /// <param name="user">Player save data (daily count).</param>
    private static void ConsumeCounselCount(User user)
    {
        // Crossing the reset point refills the allowance and restores every CanCounselToday.
        user.ResetDataIfNeeded();

        // Old saves may lack key 1; never let the count go negative.
        int remaining = user.ResetableData.DailyCounselCount.GetValueOrDefault(1, 3);
        if (remaining > 0)
        {
            remaining--;
        }

        // Callers persist via JsonDb.Save(); this method only updates memory.
        user.ResetableData.DailyCounselCount[1] = remaining;
    }

    /// <summary>Resolves the bond-point gain and completion flag from the static tables.</summary>
    /// <param name="currentLv">Current bond level (1 when no entry exists).</param>
    /// <param name="req">Request with CounselTid and SelectTid.</param>
    /// <returns>(Gain, Completed); Gain is 0/100/120, Completed is true when Gain > 0.</returns>
    private static (int Gain, bool Completed) ResolveCounselResult(int currentLv, ReqCharacterCounsel req)
    {
        // CounselTid -> main record (group + level range); SelectTid -> dialog node.
        AttractiveCounselRecord? counsel = GameData.Instance.AttractiveCounselTable.GetValueOrDefault(req.CounselTid);
        AttractiveCounselDialogRecord? dialog = GameData.Instance.CounselDialogs.GetValueOrDefault(req.SelectTid);

        // Missing data or a mismatched group degrades to the old fixed +100.
        if (counsel is null || dialog is null || dialog.GroupId != counsel.CounselGroupId)
        {
            Logging.WriteLine($"Counsel data missing for CounselTid={req.CounselTid}, SelectTid='{req.SelectTid}'; falling back to +100", LogType.Warning);
            return (100, true);
        }

        // This counsel group is only available inside its level range.
        if (currentLv < counsel.AttractiveLevelMin || currentLv > counsel.AttractiveLevelMax)
        {
            Logging.WriteLine($"Counsel {req.CounselTid} requires level {counsel.AttractiveLevelMin}-{counsel.AttractiveLevelMax}, current {currentLv}", LogType.Warning);
            return (0, false);
        }

        // Only nodes that grant points are recorded as completed dialogs.
        int gain = (int)dialog.AttractivePoint;
        return (gain, gain > 0);
    }

    /// <summary>Writes the max-bond-level trigger once, for outpost events.</summary>
    /// <param name="user">Player save data.</param>
    /// <param name="nameCode">Internal character ID.</param>
    /// <param name="currentLv">Current bond level.</param>
    /// <param name="db">Database context used to avoid duplicates.</param>
    private static void AddMaxLevelTrigger(User user, int nameCode, int currentLv, GameContext db)
    {
        // Outpost events can require a character's attractive level to be maxed.
        if (currentLv >= user.GetMaxAttractiveLevel(nameCode) &&
            !db.Triggers.Any(t => t.UserId == user.ID && t.Type == Trigger.CharacterAttractiveLevelMax && t.ConditionId == nameCode))
        {
            user.AddTrigger(Trigger.CharacterAttractiveLevelMax, currentLv, nameCode);
        }
    }

    /// <summary>Converts accumulated EXP into bond levels via AttractiveLevelTable.</summary>
    /// <param name="attractiveData">Bond entry; Lv and Exp are modified in place.</param>
    private static void UpdateAttractiveLevel(NetUserAttractiveData attractiveData)
    {
        // Spend one level's worth of points at a time until EXP runs out or Lv reaches 40.
        while (attractiveData.Lv < 40)
        {
            AttractiveLevelRecord? levelInfo = GameData.Instance.AttractiveLevelTable.Values.FirstOrDefault(x => x.AttractiveLevel == attractiveData.Lv);

            if (levelInfo == null)
            {
                break;
            }

            if (attractiveData.Exp >= levelInfo.AttractivePoint)
            {
                // Deduct only the threshold, so leftover points carry over.
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
