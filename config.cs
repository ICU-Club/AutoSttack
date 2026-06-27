using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace AutoAttackPlugin;

public class Config
{
    public bool 插件总开关 = true;
    public int 默认弹幕类型 = 121;
    public int 默认基础伤害 = 50;
    public float 默认击退力度 = 2.0f;
    public int 默认发射冷却 = 60;
    public float 默认飞行速度 = 16.0f;
    public float 默认索敌范围 = 800.0f;
    public bool 默认启用武器伤害映射 = false;
    public float 默认武器伤害系数 = 1.0f;
    public int 默认敌人DebuffID = 0;
    public int 默认敌人Debuff持续时间 = 300;
    public int 默认玩家BuffID = 0;
    public int 默认玩家Buff持续时间 = 300;

    public Dictionary<int, PetConfig> 宠物配置表 = new();
    public Dictionary<int, List<int>> 玩家宠物绑定 = new();

    public Config()
    {
        var pets = new[]
        {
            (18, "暗影珠"), (72, "蓝仙灵"), (86, "粉仙灵"), (87, "绿仙灵"),
            (197, "骷髅王头"), (198, "黄蜂宝宝"), (199, "提基幽魂"), (200, "宠物蜥蜴"),
            (208, "鹦鹉"), (209, "松露人"), (210, "树苗"), (211, "妖灵"),
            (236, "恐龙宝宝"), (266, "史莱姆宝宝"), (268, "弹簧眼"), (269, "雪人宝宝"),
            (492, "魔法灯笼"), (499, "脸怪宝宝"), (500, "猩红之心"), (653, "同伴方块"),
            (701, "龙蛋"), (702, "闪烁灯芯"), (703, "飞翔Gato"), (815, "小鹰身女妖"),
            (816, "耳廓狐"), (817, "闪光蝴蝶"), (818, "白虎"), (821, "小恶魔"),
            (825, "小熊猫"), (859, "狼人宝宝"), (881, "史莱姆王"), (882, "克苏鲁之眼"),
            (883, "世界吞噬者"), (884, "克苏鲁之脑"), (885, "骷髅王"), (886, "蜂后"),
            (887, "毁灭者"), (888, "双子魔眼"), (889, "机械骷髅王"), (890, "世纪之花"),
            (891, "石巨人"), (892, "猪龙鱼公爵"), (893, "教徒"), (894, "月总"),
            (895, "仙灵女王"), (896, "南瓜王"), (897, "常绿尖叫怪"), (898, "冰雪女王"),
            (899, "火星宠物"), (900, "哥布林战旗"), (901, "黑暗魔法师"), (934, "史莱姆皇后"),
            (956, "伯尼"), (957, "格罗姆"), (958, "独眼巨鹿"), (959, "猪人"),
            (960, "切斯特"), (994, "祝尼魔"), (998, "蓝鸡"), (1050, "斧头仙灵"),
            (1056, "滚石"), (1090, "彩虹滚石"), (1095, "帕鲁宠物奇莉"), (353, "圣诞鬼灵精"),
            (379, "蜘蛛宝宝"), (759, "小鸟宝宝"), (838, "小恶魔"), (970, "阿比盖尔")
        };
        foreach (var (id, name) in pets)
        {
            宠物配置表[id] = new PetConfig { 宠物名称 = name, 启用此宠物攻击 = true };
        }
    }

    public static Config Read(string path)
    {
        if (!File.Exists(path)) return new Config();
        var json = File.ReadAllText(path);
        var cfg = JsonConvert.DeserializeObject<Config>(json) ?? new Config();
        cfg.宠物配置表 ??= new Dictionary<int, PetConfig>();
        cfg.玩家宠物绑定 ??= new Dictionary<int, List<int>>();
        
        foreach (var kvp in cfg.宠物配置表)
        {
            var petCfg = kvp.Value;
            if (string.IsNullOrEmpty(petCfg.宠物名称) || petCfg.宠物名称 == "未命名宠物")
            {
                var name = AutoAttack.GetPetNameById(kvp.Key);
                petCfg.宠物名称 = name ?? null;
            }
        }
        
        return cfg;
    }

    public void Write(string path)
    {
        var json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(path, json);
    }
}

public class PetConfig
{
    public string? 宠物名称 = null;
    public int 弹幕类型 = 121;
    public int 基础伤害 = 50;
    public float 击退力度 = 2.0f;
    public int 发射冷却 = 60;
    public float 飞行速度 = 16.0f;
    public float 索敌范围 = 800.0f;
    public float AI参数0 = 0f;
    public float AI参数1 = 0f;
    public float AI参数2 = 0f;
    public bool 启用武器伤害映射 = false;
    public float 武器伤害系数 = 1.0f;
    public bool 启用此宠物攻击 = true;
    public int 敌人DebuffID = 0;
    public int 敌人Debuff持续时间 = 300;
    public int 玩家BuffID = 0;
    public int 玩家Buff持续时间 = 300;

    public PetConfig Clone() => (PetConfig)MemberwiseClone();
}
