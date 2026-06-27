using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace AutoAttackPlugin;

public enum AttackMode { 最近敌人 = 0, 反击模式 = 1 }

[ApiVersion(2, 1)]
public class AutoAttack : TerrariaPlugin
{
    public override string Name => "MultiPetAutoAttack";
    public override Version Version => new(1, 0, 2);
    public override string Author => "星梦";
    public override string Description => "宠物自动攻击";

    private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "AutoAttack.json");
    private static Config Cfg = new();
    private static readonly Dictionary<int, PlayerState> States = new();

    private static readonly Dictionary<string, int> PetNameToProjId = new(StringComparer.OrdinalIgnoreCase)
    {
        ["暗影珠"] = 18, ["ShadowOrb"] = 18,
        ["蓝仙灵"] = 72, ["BlueFairy"] = 72, ["仙灵铃铛"] = 72,
        ["粉仙灵"] = 86, ["PinkFairy"] = 86,
        ["绿仙灵"] = 87, ["GreenFairy"] = 87,
        ["骷髅王头"] = 197, ["BabySkeletronHead"] = 197,
        ["黄蜂宝宝"] = 198, ["BabyHornet"] = 198,
        ["提基幽魂"] = 199, ["TikiSpirit"] = 199,
        ["宠物蜥蜴"] = 200, ["PetLizard"] = 200,
        ["鹦鹉"] = 208, ["Parrot"] = 208,
        ["松露人"] = 209, ["Truffle"] = 209,
        ["树苗"] = 210, ["Sapling"] = 210,
        ["妖灵"] = 211, ["Wisp"] = 211,
        ["恐龙宝宝"] = 236, ["BabyDino"] = 236,
        ["史莱姆宝宝"] = 266, ["BabySlime"] = 266,
        ["弹簧眼"] = 268, ["EyeSpring"] = 268,
        ["雪人宝宝"] = 269, ["BabySnowman"] = 269,
        ["魔法灯笼"] = 492, ["MagicLantern"] = 492,
        ["脸怪宝宝"] = 499, ["BabyFaceMonster"] = 499,
        ["猩红之心"] = 500, ["CrimsonHeart"] = 500,
        ["同伴方块"] = 653, ["CompanionCube"] = 653,
        ["龙蛋"] = 701, ["DD2PetDragon"] = 701,
        ["闪烁灯芯"] = 702, ["DD2PetGhost"] = 702,
        ["飞翔Gato"] = 703, ["DD2PetGato"] = 703,
        ["小鹰身女妖"] = 815, ["LilHarpy"] = 815,
        ["耳廓狐"] = 816, ["FennecFox"] = 816,
        ["闪光蝴蝶"] = 817, ["GlitteryButterfly"] = 817,
        ["白虎"] = 818, ["WhiteTiger"] = 818,
        ["小恶魔"] = 821, ["BabyImp"] = 821,
        ["小熊猫"] = 825, ["BabyRedPanda"] = 825,
        ["狼人宝宝"] = 859, ["BabyWerewolf"] = 859,
        ["史莱姆王"] = 881, ["KingSlimePet"] = 881,
        ["克苏鲁之眼"] = 882, ["EyeOfCthulhuPet"] = 882,
        ["世界吞噬者"] = 883, ["EaterOfWorldsPet"] = 883,
        ["克苏鲁之脑"] = 884, ["BrainOfCthulhuPet"] = 884,
        ["骷髅王"] = 885, ["SkeletronPet"] = 885,
        ["蜂后"] = 886, ["QueenBeePet"] = 886,
        ["毁灭者"] = 887, ["DestroyerPet"] = 887,
        ["双子魔眼"] = 888, ["TwinsPet"] = 888,
        ["机械骷髅王"] = 889, ["SkeletronPrimePet"] = 889,
        ["世纪之花"] = 890, ["PlanteraPet"] = 890,
        ["石巨人"] = 891, ["GolemPet"] = 891,
        ["猪龙鱼公爵"] = 892, ["DukeFishronPet"] = 892,
        ["教徒"] = 893, ["LunaticCultistPet"] = 893,
        ["月总"] = 894, ["MoonLordPet"] = 894,
        ["仙灵女王"] = 895, ["FairyQueenPet"] = 895,
        ["南瓜王"] = 896, ["PumpkingPet"] = 896,
        ["常绿尖叫怪"] = 897, ["EverscreamPet"] = 897,
        ["冰雪女王"] = 898, ["IceQueenPet"] = 898,
        ["火星宠物"] = 899, ["MartianPet"] = 899,
        ["哥布林战旗"] = 900, ["DD2OgrePet"] = 900,
        ["黑暗魔法师"] = 901, ["DD2BetsyPet"] = 901,
        ["史莱姆皇后"] = 934, ["QueenSlimePet"] = 934,
        ["伯尼"] = 956, ["BerniePet"] = 956,
        ["格罗姆"] = 957, ["GlommerPet"] = 957,
        ["独眼巨鹿"] = 958, ["DeerclopsPet"] = 958,
        ["猪人"] = 959, ["PigPet"] = 959,
        ["切斯特"] = 960, ["ChesterPet"] = 960,
        ["祝尼魔"] = 994, ["JunimoPet"] = 994,
        ["蓝鸡"] = 998, ["BlueChickenPet"] = 998,
        ["斧头仙灵"] = 1050, ["AxeFairyPet"] = 1050,
        ["滚石"] = 1056, ["BoulderPet"] = 1056,
        ["彩虹滚石"] = 1090, ["RainbowBoulderPet"] = 1090,
        ["帕鲁宠物奇莉"] = 1095, ["PalworldPetChillet"] = 1095
    };

    private static readonly HashSet<int> PetProjIds = new(PetNameToProjId.Values);

    public static string? GetPetNameById(int projId)
    {
        foreach (var kvp in PetNameToProjId)
        {
            if (kvp.Value == projId)
                return kvp.Key;
        }
        return null;
    }

    private const string PermUse = "autoattack.use";
    private const string PermAdmin = "autoattack.admin";
    private const string PermBind = "autoattack.bind";

    public AutoAttack(Main game) : base(game) { }

    public override void Initialize()
    {
        LoadConfig();
        ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        GeneralHooks.ReloadEvent += OnReload;
        TShockAPI.GetDataHandlers.PlayerDamage.Register(OnPlayerDamage);
        ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
        ServerApi.Hooks.NetGreetPlayer.Register(this, OnJoin);

        Commands.ChatCommands.Add(new Command(PermUse, CmdToggle, "autotoggle", "atog")
        { HelpText = "开启/关闭自动攻击，或指定宠物：/atog <宠物名>" });
        Commands.ChatCommands.Add(new Command(PermBind, CmdBind, "autobind")
        { HelpText = "绑定宠物配置：/autobind <宠物名> [弹幕类型] [伤害] [DebuffID] [BuffID]" });
        Commands.ChatCommands.Add(new Command(PermAdmin, CmdConfig, "autocfg")
        { HelpText = "修改配置：/autocfg <参数名> <值> [宠物名]" });
        Commands.ChatCommands.Add(new Command(PermUse, CmdList, "autolist")
        { HelpText = "列出当前宠物的攻击配置" });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            GeneralHooks.ReloadEvent -= OnReload;
            TShockAPI.GetDataHandlers.PlayerDamage.UnRegister(OnPlayerDamage);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnJoin);
        }
        base.Dispose(disposing);
    }

    private void OnJoin(GreetPlayerEventArgs e)
    {
        if (!States.ContainsKey(e.Who))
            States[e.Who] = new PlayerState();
    }

    private void OnUpdate(EventArgs _)
    {
        if (!Cfg.插件总开关) return;

        foreach (var tsp in TShock.Players.Where(p => p?.Active == true))
        {
            if (!States.TryGetValue(tsp.Index, out var state) || !state.全局启用) continue;

            var pets = FindPlayerPets(tsp.TPlayer);
            var target = ResolveTarget(tsp.TPlayer, state, pets);
            if (target == null) continue;

            foreach (var pet in pets)
            {
                var petId = pet.type;
                if (!Cfg.宠物配置表.ContainsKey(petId))
                    Cfg.宠物配置表[petId] = CreateDefaultPetConfig(petId);

                var petCfg = Cfg.宠物配置表[petId];
                if (!petCfg.启用此宠物攻击) continue;

                if (state.PetCooldowns.TryGetValue(petId, out var cd) && cd > 0)
                {
                    state.PetCooldowns[petId] = cd - 1;
                    continue;
                }

                if ((pet.Center - target.Center).LengthSquared() < petCfg.索敌范围 * petCfg.索敌范围)
                {
                    FireProjectile(tsp.TPlayer, target, pet, petCfg);
                    state.PetCooldowns[petId] = petCfg.发射冷却;
                }
            }
        }
    }

    private NPC? ResolveTarget(Player player, PlayerState state, List<Projectile> pets)
    {
        NPC? target = null;

        if (state.反击目标索引 >= 0)
        {
            var revengeNpc = Main.npc[state.反击目标索引];
            if (revengeNpc.active && revengeNpc.CanBeChasedBy())
            {
                foreach (var pet in pets)
                {
                    var petCfg = Cfg.宠物配置表[pet.type];
                    if ((pet.Center - revengeNpc.Center).LengthSquared() < petCfg.索敌范围 * petCfg.索敌范围)
                    {
                        target = revengeNpc;
                        break;
                    }
                }
            }
            else
            {
                state.反击目标索引 = -1;
            }
        }

        if (target == null && pets.Count > 0)
        {
            float range = Cfg.宠物配置表.TryGetValue(pets[0].type, out var c) ? c.索敌范围 : Cfg.默认索敌范围;
            target = FindNearestTarget(player, player.Center, range);
        }

        return target;
    }

    private static bool IsPetProjectile(int projType) => PetProjIds.Contains(projType);

    private List<Projectile> FindPlayerPets(Player player)
    {
        var pets = new List<Projectile>();
        for (int i = 0; i < Main.maxProjectiles; i++)
        {
            var proj = Main.projectile[i];
            if (proj.active && proj.owner == player.whoAmI && IsPetProjectile(proj.type) && !proj.minion && !proj.sentry)
                pets.Add(proj);
        }
        return pets;
    }

    private NPC? FindNearestTarget(Player player, Vector2 sourcePos, float range)
    {
        float rangeSq = range * range;
        NPC? nearest = null;
        float minDist = float.MaxValue;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (!npc.active || !npc.CanBeChasedBy()) continue;
            var distSq = (npc.Center - sourcePos).LengthSquared();
            if (distSq < rangeSq && distSq < minDist)
            {
                minDist = distSq;
                nearest = npc;
            }
        }
        return nearest;
    }

    private void FireProjectile(Player player, NPC target, Projectile pet, PetConfig cfg)
    {
        int damage = cfg.基础伤害;
        if (cfg.启用武器伤害映射 && player.HeldItem.damage > 0)
            damage = (int)(player.GetWeaponDamage(player.HeldItem) * cfg.武器伤害系数) + cfg.基础伤害;

        var direction = (target.Center - pet.Center).SafeNormalize(Vector2.Zero);
        var velocity = direction * cfg.飞行速度;

        var spawnSource = new EntitySource_DebugCommand();

        int projIndex = Projectile.NewProjectile(
            spawnSource, pet.Center, velocity,
            cfg.弹幕类型, damage, cfg.击退力度,
            player.whoAmI, cfg.AI参数0, cfg.AI参数1, cfg.AI参数2);

        if (projIndex >= 0 && projIndex < Main.maxProjectiles)
        {
            TSPlayer.All.SendData((PacketTypes)27, "", projIndex);

            if (cfg.敌人DebuffID > 0)
                target.AddBuff(cfg.敌人DebuffID, cfg.敌人Debuff持续时间);
            if (cfg.玩家BuffID > 0)
                player.AddBuff(cfg.玩家BuffID, cfg.玩家Buff持续时间);
        }
    }

    private void OnPlayerDamage(object? _, TShockAPI.GetDataHandlers.PlayerDamageEventArgs e)
    {
        if (e.PlayerDeathReason == null) return;

        int? npcIdx = GetNpcIndexFromDeathReason(e.PlayerDeathReason);
        if (npcIdx.HasValue && npcIdx.Value >= 0 && npcIdx.Value < Main.maxNPCs)
        {
            var attacker = Main.npc[npcIdx.Value];
            if (attacker.active && States.TryGetValue(e.ID, out var st))
            {
                st.反击目标索引 = npcIdx.Value;
                return;
            }
        }

        var tsPlayer = TShock.Players[e.ID];
        if (tsPlayer?.TPlayer == null) return;

        var playerPos = tsPlayer.TPlayer.Center;
        float nearestDist = 400f * 400f;
        int nearestNpc = -1;

        for (int i = 0; i < Main.maxNPCs; i++)
        {
            var npc = Main.npc[i];
            if (!npc.active || !npc.CanBeChasedBy()) continue;
            var distSq = (npc.Center - playerPos).LengthSquared();
            if (distSq < nearestDist)
            {
                nearestDist = distSq;
                nearestNpc = i;
            }
        }

        if (nearestNpc >= 0 && States.TryGetValue(e.ID, out var state))
            state.反击目标索引 = nearestNpc;
    }

    private static int? GetNpcIndexFromDeathReason(PlayerDeathReason reason)
    {
        try
        {
            var prop = reason.GetType().GetProperty("SourceNPCIndex");
            if (prop != null) return prop.GetValue(reason) as int?;

            var field = reason.GetType().GetField("_sourceNPCIndex", BindingFlags.NonPublic | BindingFlags.Instance)
                     ?? reason.GetType().GetField("sourceNPCIndex", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if (field != null) return field.GetValue(reason) as int?;
        }
        catch { }
        return null;
    }

    private void OnLeave(LeaveEventArgs e)
    {
        States.Remove(e.Who);
        Cfg.玩家宠物绑定.Remove(e.Who);
    }

    private void CmdBind(CommandArgs args)
    {
        if (args.Parameters.Count == 0)
        {
            args.Player.SendInfoMessage("用法: /autobind <宠物名> [弹幕类型] [伤害] [敌人DebuffID] [玩家BuffID]\n" +
                "示例: /autobind 暗影珠 132 100 204 1\n" +
                "DebuffID: 20=中毒 21=着火 24=诅咒 30=流血 31=困惑 36=暗影焰 44=霜冻 70=破甲\n" +
                "BuffID: 1=黑曜石皮 2=再生 3=敏捷 4=铁皮 5=魔能 6=威力 7=恢复 13=攻击增益 26=生命增益\n" +
                "支持宠物: 暗影珠、仙灵、恐龙宝宝、史莱姆宝宝等");
            return;
        }

        var petName = args.Parameters[0];
        if (!PetNameToProjId.TryGetValue(petName, out var projId))
        {
            args.Player.SendErrorMessage($"未知宠物: {petName}。使用 /autolist 查看支持的宠物列表。");
            return;
        }

        var petCfg = Cfg.宠物配置表.ContainsKey(projId)
            ? Cfg.宠物配置表[projId]
            : CreateDefaultPetConfig(projId);
        petCfg.宠物名称 = petName;

        if (args.Parameters.Count >= 2 && int.TryParse(args.Parameters[1], out var pt)) petCfg.弹幕类型 = pt;
        if (args.Parameters.Count >= 3 && int.TryParse(args.Parameters[2], out var dmg)) petCfg.基础伤害 = dmg;
        if (args.Parameters.Count >= 4 && int.TryParse(args.Parameters[3], out var debuffId)) petCfg.敌人DebuffID = debuffId;
        if (args.Parameters.Count >= 5 && int.TryParse(args.Parameters[4], out var buffId)) petCfg.玩家BuffID = buffId;

        Cfg.宠物配置表[projId] = petCfg;

        if (!Cfg.玩家宠物绑定.ContainsKey(args.Player.Index))
            Cfg.玩家宠物绑定[args.Player.Index] = new List<int>();
        if (!Cfg.玩家宠物绑定[args.Player.Index].Contains(projId))
            Cfg.玩家宠物绑定[args.Player.Index].Add(projId);

        Cfg.Write(ConfigPath);
        args.Player.SendSuccessMessage($"[AutoAttack] 已为  {petName}  绑定配置:");
        args.Player.SendInfoMessage($"弹幕:{petCfg.弹幕类型} 伤害:{petCfg.基础伤害} Debuff:{petCfg.敌人DebuffID} Buff:{petCfg.玩家BuffID}");
    }

    private void CmdToggle(CommandArgs args)
    {
        var idx = args.Player.Index;
        if (!States.TryGetValue(idx, out var state))
            States[idx] = state = new PlayerState();

        if (args.Parameters.Count > 0)
        {
            var petName = args.Parameters[0];
            if (!PetNameToProjId.TryGetValue(petName, out var projId))
            {
                args.Player.SendErrorMessage($"未知宠物: {petName}");
                return;
            }
            if (Cfg.宠物配置表.TryGetValue(projId, out var cfg))
            {
                cfg.启用此宠物攻击 = !cfg.启用此宠物攻击;
                args.Player.SendSuccessMessage($"[AutoAttack]  {petName}  的攻击已{(cfg.启用此宠物攻击 ? "开启" : "关闭")}");
            }
            else args.Player.SendErrorMessage($"该宠物尚未绑定配置，请先使用 /autobind {petName}");
            return;
        }

        state.全局启用 = !state.全局启用;
        args.Player.SendSuccessMessage($"[AutoAttack] 自动攻击系统已{(state.全局启用 ? "§a开启 " : " 关闭 ")}");
        if (state.全局启用) args.Player.SendInfoMessage("提示: 使用 /autobind <宠物名> 为宠物绑定攻击配置");
    }

    private void CmdList(CommandArgs args)
    {
        var msg = new System.Text.StringBuilder();
        msg.AppendLine(" [AutoAttack] 当前宠物配置列表:");

        if (Cfg.宠物配置表.Count == 0)
        {
            msg.AppendLine("暂无配置，使用 /autobind <宠物名> 创建");
        }
        else
        {
            foreach (var kvp in Cfg.宠物配置表)
            {
                var c = kvp.Value;
                var status = c.启用此宠物攻击 ? "§a[开] " : " [关] ";
                var db = c.敌人DebuffID > 0 ? $" Debuff:{c.敌人DebuffID}" : "";
                var bf = c.玩家BuffID > 0 ? $" Buff:{c.玩家BuffID}" : "";
                msg.AppendLine($"{status}  {c.宠物名称} (ID:{kvp.Key}) 弹幕:{c.弹幕类型} 伤:{c.基础伤害}{db}{bf}");
            }
        }
        msg.AppendLine(" 支持的宠物: 暗影珠、仙灵、恐龙宝宝、UFO、魔法灯笼等");
        args.Player.SendInfoMessage(msg.ToString());
    }

    private void CmdConfig(CommandArgs args)
    {
        if (args.Parameters.Count < 2)
        {
            args.Player.SendInfoMessage("用法: /autocfg <参数名> <值> [宠物名]\n" +
                "参数: 弹幕类型、基础伤害、发射冷却、索敌范围、武器系数、\n" +
                "      敌人DebuffID、敌人Debuff时长、玩家BuffID、玩家Buff时长\n" +
                "示例: /autocfg 弹幕类型 132 暗影珠\n" +
                "      /autocfg 敌人DebuffID 204");
            return;
        }

        var key = args.Parameters[0];
        var value = args.Parameters[1];

        PetConfig? targetCfg = null;
        string targetName = "全局";

        if (args.Parameters.Count >= 3)
        {
            var petName = args.Parameters[2];
            if (!PetNameToProjId.TryGetValue(petName, out var projId))
            {
                args.Player.SendErrorMessage($"未知宠物: {petName}");
                return;
            }
            if (!Cfg.宠物配置表.TryGetValue(projId, out targetCfg))
            {
                targetCfg = CreateDefaultPetConfig(projId);
                targetCfg.宠物名称 = petName;
                Cfg.宠物配置表[projId] = targetCfg;
            }
            targetName = petName;
        }

        try
        {
            ApplyConfigValue(key, value, targetCfg, targetName, args.Player);
            Cfg.Write(ConfigPath);
        }
        catch (Exception ex)
        {
            args.Player.SendErrorMessage($"参数错误: {ex.Message}");
        }
    }

    private void ApplyConfigValue(string key, string value, PetConfig? target, string targetName, TSPlayer player)
    {
        bool isGlobal = target == null;
        switch (key)
        {
            case "弹幕类型":
                if (isGlobal) Cfg.默认弹幕类型 = int.Parse(value); else target!.弹幕类型 = int.Parse(value);
                break;
            case "基础伤害":
                if (isGlobal) Cfg.默认基础伤害 = int.Parse(value); else target!.基础伤害 = int.Parse(value);
                break;
            case "发射冷却":
                if (isGlobal) Cfg.默认发射冷却 = int.Parse(value); else target!.发射冷却 = int.Parse(value);
                break;
            case "索敌范围":
                if (isGlobal) Cfg.默认索敌范围 = float.Parse(value); else target!.索敌范围 = float.Parse(value);
                break;
            case "飞行速度":
                if (isGlobal) Cfg.默认飞行速度 = float.Parse(value); else target!.飞行速度 = float.Parse(value);
                break;
            case "武器系数":
                if (isGlobal) Cfg.默认武器伤害系数 = float.Parse(value); else target!.武器伤害系数 = float.Parse(value);
                break;
            case "击退力度":
                if (isGlobal) break; else target!.击退力度 = float.Parse(value);
                break;
            case "启用武器映射":
                if (isGlobal) Cfg.默认启用武器伤害映射 = bool.Parse(value.ToLower()); else target!.启用武器伤害映射 = bool.Parse(value.ToLower());
                break;
            case "敌人DebuffID":
                if (isGlobal) Cfg.默认敌人DebuffID = int.Parse(value); else target!.敌人DebuffID = int.Parse(value);
                break;
            case "敌人Debuff时长":
                if (isGlobal) Cfg.默认敌人Debuff持续时间 = int.Parse(value); else target!.敌人Debuff持续时间 = int.Parse(value);
                break;
            case "玩家BuffID":
                if (isGlobal) Cfg.默认玩家BuffID = int.Parse(value); else target!.玩家BuffID = int.Parse(value);
                break;
            case "玩家Buff时长":
                if (isGlobal) Cfg.默认玩家Buff持续时间 = int.Parse(value); else target!.玩家Buff持续时间 = int.Parse(value);
                break;
            default:
                player.SendErrorMessage($"未知参数: {key}");
                return;
        }
        player.SendSuccessMessage($"[AutoAttack] {targetName} 的  {key}  已设为  {value} ");
    }

    private static PetConfig CreateDefaultPetConfig(int projId)
    {
        string name = PetNameToProjId.FirstOrDefault(kvp => kvp.Value == projId).Key ?? "未知宠物";
        return new PetConfig
        {
            宠物名称 = name,
            弹幕类型 = Cfg.默认弹幕类型,
            基础伤害 = Cfg.默认基础伤害,
            击退力度 = Cfg.默认击退力度,
            发射冷却 = Cfg.默认发射冷却,
            飞行速度 = Cfg.默认飞行速度,
            索敌范围 = Cfg.默认索敌范围,
            启用武器伤害映射 = Cfg.默认启用武器伤害映射,
            武器伤害系数 = Cfg.默认武器伤害系数,
            启用此宠物攻击 = true,
            敌人DebuffID = Cfg.默认敌人DebuffID,
            敌人Debuff持续时间 = Cfg.默认敌人Debuff持续时间,
            玩家BuffID = Cfg.默认玩家BuffID,
            玩家Buff持续时间 = Cfg.默认玩家Buff持续时间
        };
    }

    private static void LoadConfig()
    {
        Cfg = Config.Read(ConfigPath);
        Cfg.Write(ConfigPath);
    }

    private void OnReload(ReloadEventArgs? e)
    {
        LoadConfig();
        e?.Player.SendSuccessMessage("[AutoAttack] 多宠物配置重载完成");
    }
}

public sealed class PlayerState
{
    public bool 全局启用 = false;
    public int 反击目标索引 = -1;
    public Dictionary<int, int> PetCooldowns = new();
}
