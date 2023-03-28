using AmongUs.GameOptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TOHE.Modules;

internal class CustomRoleSelector
{
    public static Dictionary<PlayerControl, CustomRoles> RoleResult;
    public static IReadOnlyList<CustomRoles> AllRoles => RoleResult.Values.ToList();

    public static void SelectCustomRoles()
    {
        // 开始职业抽取
        RoleResult = new();
        var rd = IRandom.Instance;
        int playerCount = Main.AllAlivePlayerControls.Count();
        int optImpNum = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        int optNeutralNum = 0;
        if (Options.NeutralRolesMaxPlayer.GetInt() > 0 && Options.NeutralRolesMaxPlayer.GetInt() >= Options.NeutralRolesMinPlayer.GetInt())
            optNeutralNum = rd.Next(Options.NeutralRolesMinPlayer.GetInt(), Options.NeutralRolesMaxPlayer.GetInt() + 1);

        int readyRoleNum = 0;
        int readyNeutralNum = 0;

        List<CustomRoles> rolesToAssign = new();

        List<CustomRoles> roleList = new();
        List<CustomRoles> roleOnList = new();
        List<CustomRoles> ImpOnList = new();
        List<CustomRoles> NeutralOnList = new();

        List<CustomRoles> roleRateList = new();
        List<CustomRoles> ImpRateList = new();
        List<CustomRoles> NeutralRateList = new();

        foreach (var cr in Enum.GetValues(typeof(CustomRoles)))
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (role.IsVanilla() || role.IsAdditionRole()) continue;
            if (role is CustomRoles.GM or CustomRoles.NotAssigned) continue;
            for (int i = 0; i < role.GetCount(); i++)
                roleList.Add(role);
        }

        // 职业设置为：优先
        foreach (var role in roleList) if (role.GetMode() == 2)
            {
                if (role.IsImpostor()) ImpOnList.Add(role);
                else if (role.IsNeutral()) NeutralOnList.Add(role);
                else roleOnList.Add(role);
            }
        // 职业设置为：启用
        foreach (var role in roleList) if (role.GetMode() == 1)
            {
                if (role.IsImpostor()) ImpRateList.Add(role);
                else if (role.IsNeutral()) NeutralRateList.Add(role);
                else roleRateList.Add(role);
            }

        // 抽取优先职业（内鬼）
        while (ImpOnList.Count > 0)
        {
            var select = ImpOnList[rd.Next(0, ImpOnList.Count)];
            ImpOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            Logger.Info(select.ToString() + " 加入内鬼职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyRoleNum >= optImpNum) break;
        }
        // 优先职业不足以分配，开始分配启用的职业（内鬼）
        if (readyRoleNum < playerCount && readyRoleNum < optImpNum)
        {
            while (ImpRateList.Count > 0)
            {
                var select = ImpRateList[rd.Next(0, ImpRateList.Count)];
                ImpRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                Logger.Info(select.ToString() + " 加入内鬼职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyRoleNum >= optImpNum) break;
            }
        }

        // 抽取优先职业（中立）
        while (NeutralOnList.Count > 0 && optNeutralNum > 0)
        {
            var select = NeutralOnList[rd.Next(0, NeutralOnList.Count)];
            NeutralOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            readyNeutralNum += select.GetCount();
            Logger.Info(select.ToString() + " 加入中立职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
            if (readyNeutralNum >= optNeutralNum) break;
        }
        // 优先职业不足以分配，开始分配启用的职业（中立）
        if (readyRoleNum < playerCount && readyNeutralNum < optNeutralNum)
        {
            while (NeutralRateList.Count > 0 && optNeutralNum > 0)
            {
                var select = NeutralRateList[rd.Next(0, NeutralRateList.Count)];
                NeutralRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                readyNeutralNum += select.GetCount();
                Logger.Info(select.ToString() + " 加入中立职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
                if (readyNeutralNum >= optNeutralNum) break;
            }
        }

        // 抽取优先职业
        while (roleOnList.Count > 0)
        {
            var select = roleOnList[rd.Next(0, roleOnList.Count)];
            roleOnList.Remove(select);
            rolesToAssign.Add(select);
            readyRoleNum++;
            Logger.Info(select.ToString() + " 加入船员职业待选列表（优先）", "CustomRoleSelector");
            if (readyRoleNum >= playerCount) goto EndOfAssign;
        }
        // 优先职业不足以分配，开始分配启用的职业
        if (readyRoleNum < playerCount)
        {
            while (roleRateList.Count > 0)
            {
                var select = roleRateList[rd.Next(0, roleRateList.Count)];
                roleRateList.Remove(select);
                rolesToAssign.Add(select);
                readyRoleNum++;
                Logger.Info(select.ToString() + " 加入船员职业待选列表", "CustomRoleSelector");
                if (readyRoleNum >= playerCount) goto EndOfAssign;
            }
        }

    // 职业抽取结束
    EndOfAssign:

        // EAC封禁名单玩家开房将被分配为小丑
        if (BanManager.CheckEACList(PlayerControl.LocalPlayer.FriendCode))
        {
            if (!rolesToAssign.Contains(CustomRoles.Jester))
                rolesToAssign.Add(CustomRoles.Jester);
            Main.DevRole.Remove(PlayerControl.LocalPlayer.PlayerId);
            Main.DevRole.Add(PlayerControl.LocalPlayer.PlayerId, CustomRoles.Jester);
        }
        
        // Dev Roles List Edit
        foreach (var dr in Main.DevRole)
        {
            if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;
            if (rolesToAssign.Contains(dr.Value))
            {
                rolesToAssign.Remove(dr.Value);
                rolesToAssign.Insert(0, dr.Value);
                Logger.Info("职业列表提高优先：" + dr.Value, "Dev Role");
                continue;
            }
            for (int i = 0; i < rolesToAssign.Count; i++)
            {
                var role = rolesToAssign[i];
                if (dr.Value.GetMode() != role.GetMode()) continue;
                if (
                    (dr.Value.IsImpostor() && role.IsImpostor()) ||
                    (dr.Value.IsNeutral() && role.IsNeutral()) ||
                    (dr.Value.IsCrewmate() & role.IsCrewmate())
                    )
                {
                    rolesToAssign.RemoveAt(i);
                    rolesToAssign.Insert(0, dr.Value);
                    Logger.Info("覆盖职业列表：" + i + " " + role.ToString() + " => " + dr.Value, "Dev Role");
                    break;
                }
            }
        }

        var AllPlayer = Main.AllAlivePlayerControls.ToList();

        while (AllPlayer.Count() > 0 && rolesToAssign.Count > 0)
        {
            PlayerControl delPc = null;
            foreach (var pc in AllPlayer)
                foreach (var dr in Main.DevRole.Where(x => pc.PlayerId == x.Key))
                {
                    if (dr.Key == PlayerControl.LocalPlayer.PlayerId && Options.EnableGM.GetBool()) continue;
                    var id = rolesToAssign.IndexOf(dr.Value);
                    if (id == -1) continue;
                    RoleResult.Add(pc, rolesToAssign[id]);
                    Logger.Info($"职业优先分配：{AllPlayer[0].GetRealName()} => {rolesToAssign[id]}", "CustomRoleSelector");
                    delPc = pc;
                    rolesToAssign.RemoveAt(id);
                    goto EndOfWhile;
                }

            var roleId = rd.Next(0, rolesToAssign.Count);
            RoleResult.Add(AllPlayer[0], rolesToAssign[roleId]);
            Logger.Info($"职业分配：{AllPlayer[0].GetRealName()} => {rolesToAssign[roleId]}", "CustomRoleSelector");
            AllPlayer.RemoveAt(0);
            rolesToAssign.RemoveAt(roleId);

        EndOfWhile:;
            if (delPc != null)
            {
                AllPlayer.Remove(delPc);
                Main.DevRole.Remove(delPc.PlayerId);
            }
        }

        if (AllPlayer.Count() > 0)
            Logger.Error("职业分配错误：存在未被分配职业的玩家", "CustomRoleSelector");
        if (rolesToAssign.Count > 0)
            Logger.Error("职业分配错误：存在未被分配的职业", "CustomRoleSelector");

    }

    public static int addScientistNum = 0;
    public static int addEngineerNum = 0;
    public static int addShapeshifterNum = 0;
    public static void CalculateVanillaRoleCount()
    {
        // 计算原版特殊职业数量
        addEngineerNum = 0;
        addScientistNum = 0;
        addShapeshifterNum = 0;
        foreach (var role in AllRoles)
        {
            switch (CustomRolesHelper.GetVNRole(role))
            {
                case CustomRoles.Scientist: addScientistNum++; break;
                case CustomRoles.Engineer: addEngineerNum++; break;
                case CustomRoles.Shapeshifter: addShapeshifterNum++; break;
            }
        }
    }

    public static List<CustomRoles> AddonRolesList = new();
    public static void SelectAddonRoles()
    {
        AddonRolesList = new();
        foreach (var cr in Enum.GetValues(typeof(CustomRoles)))
        {
            CustomRoles role = (CustomRoles)Enum.Parse(typeof(CustomRoles), cr.ToString());
            if (!role.IsAdditionRole()) continue;
            if (role is CustomRoles.Madmate && Options.MadmateSpawnMode.GetInt() != 0) continue;
            if (role is CustomRoles.Lovers or CustomRoles.LastImpostor or CustomRoles.Workhorse) continue;
            AddonRolesList.Add(role);
        }
    }
}
