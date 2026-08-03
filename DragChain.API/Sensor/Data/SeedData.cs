using Microsoft.EntityFrameworkCore;
using DragChain.API.Sensor.Models;

namespace DragChain.API.Sensor.Data;

public static class SeedData
{
    public static IReadOnlyList<RuleProduct> BuildDefaultRuleProducts()
    {
        var products = BuildDefaultRules().RuleProducts;
        return products
            .Select(item => new RuleProduct
            {
                Id = item.Id,
                RuleId = item.RuleId,
                ProductId = item.ProductId,
                Quantity = item.Quantity
            })
            .ToList();
    }

    public static void Configure(ModelBuilder modelBuilder)
    {
        SeedSensorTypes(modelBuilder);
        SeedProducts(modelBuilder);
        SeedScenarios(modelBuilder);
        SeedRules(modelBuilder);
        SeedProcessScenarios(modelBuilder);
    }

    private static void SeedSensorTypes(ModelBuilder mb)
    {
        var types = new (string id, string name)[]
        {
            ("proximity", "近接开关"), ("proximity_large", "近接开关(大黄)"),
            ("proximity_18", "近接开关(φ18)"), ("proximity_30", "近接开关(φ30)"),
            ("proximity_flush", "近接开关(齐平式)"),
            ("photoelectric", "光电传感器/对照光电"), ("photoelectric-bg", "背景抑制光电"),
            ("diffuse", "漫反射光电"), ("reflective", "镜片式光电"),
            ("capacitive", "静电容"), ("capacitive_small", "静电容(φ12)"),
            ("fiber", "光纤传感器"), ("fiber_m3", "光纤(M3)"), ("fiber_m6", "光纤(M6)"),
            ("slot", "槽型光电"), ("laser", "激光感应器"), ("grating", "安全光栅"),
            ("switch", "极限开关"), ("lock", "电子锁"), ("color_sensor", "颜色传感器"),
            ("vacuum_gauge", "真空数显表"), ("other", "其他")
        };
        mb.Entity<SensorType>().HasData(types.Select(t => new SensorType { Id = t.id, Name = t.name }).ToArray());
    }

    private static void SeedProducts(ModelBuilder mb)
    {
        var products = new (string code, string model, string name, string brand, string type, string? spec)[]
        {
            ("p1", "FAB-18D16N1-D3", "标准近接开关", "马赫", "proximity_18", "φ18mm, 16mm距离, NPN输出"),
            ("p2", "FBB-18D12N2-D3", "齐平式近接开关", "马赫", "proximity_flush", "φ18mm, 12mm距离, 齐平式"),
            ("p3", "KB-3020N", "大黄近接开关", "精通", "proximity_large", "φ30mm, 20mm距离"),
            ("p4", "KN-1705N", "标准近接开关", "精通", "proximity", "φ17mm, 5mm距离"),
            ("p4b", "KN-SO3N2", "侧边近接开关", "精通", "proximity", "侧边感应, N.C, φ17mm"),
            ("p4c", "I1CN-M3040N-LQ3U2", "φ30近接开关", "-", "proximity_30", "φ30mm, 40mm距离, 宏恒胜专用"),
            ("p5", "GD-B31N", "背景抑制光电", "京东方", "photoelectric-bg", "3-300mm检测距离, NPN输出"),
            ("p6", "EZ-GB52S", "背景抑制光电", "奥普特", "photoelectric-bg", "0-300mm检测距离"),
            ("p7", "PZ-G51N", "对照光电", "基恩士", "photoelectric", "20M检测距离, NPN输出"),
            ("p8", "PZ-G61N", "镜片式光电", "基恩士", "reflective", "4.2M检测距离"),
            ("p9", "GTB2S", "漫反射光电", "SICK", "diffuse", "0-120mm检测距离"),
            ("p10", "EC18-N20", "标准静电容", "精通", "capacitive", "φ18mm, 2-20mm距离"),
            ("p11", "TAPT-12D08N1", "加密静电容", "马赫", "capacitive_small", "φ12mm, 8mm距离"),
            ("p12", "PM-L45", "槽型光电", "松下", "slot", "标准型, NPN输出"),
            ("p13", "PM-Y45", "槽型光电", "松下", "slot", "短型, 125系列自制模组专用"),
            ("p14", "FU-6F+FS-N18N", "M6光纤", "基恩士", "fiber_m6", "M6规格, 标准间距, 滚轮间距>55mm"),
            ("p15", "FU-35FA", "M3光纤", "基恩士", "fiber_m3", "M3规格, 加密间距, 滚轮间距<20mm"),
            ("p15b", "FU-32", "光纤传感器", "基恩士", "fiber", "1M, 插框手臂夹爪确认"),
            ("p15c", "GU-31", "光纤传感器", "京东方", "fiber", "插框手臂托爪确认"),
            ("p16", "CHG-1005D", "激光感应器", "创视知联", "laser", "50-65mm检测距离, 三点寻边"),
            ("p17", "JG-C50", "激光感应器", "京东方", "laser", "35-200mm检测距离, 三点寻边"),
            ("p18", "安全光栅", "安全光栅", "汇科", "grating", "常闭, AGV退出确认"),
            ("p19", "AZ-108", "极限开关", "-", "switch", "标准极限开关"),
            ("p20", "QSD-XD7EM", "电子锁", "固测", "lock", "NG门锁, 防止拉门被撞开"),
            ("p21", "EZ-T52", "对照光电", "奥普特", "photoelectric", "10M检测距离, 常开"),
            ("p22", "OPT-EZ-UT56-W2-06", "对照光电", "奥普特", "photoelectric", "固定式对照光电"),
            ("p23", "待定", "颜色判别传感器", "待定", "color_sensor", "需搭配色标, 区分板架与板件"),
            ("p24", "DPB01N-P-100-100KPa", "真空数显表", "台达", "vacuum_gauge", "含支架, 无孔制程检测"),
            ("p25", "LM2-Q1000T", "激光传感器", "-", "laser", "L插架前方检测")
        };

        mb.Entity<Product>().HasData(
            products.Select((p, i) => new Product { Id = i + 1, Code = p.code, Model = p.model, Name = p.name, Brand = p.brand, Type = p.type, Spec = p.spec }).ToArray()
        );
    }

    private static void SeedScenarios(ModelBuilder mb)
    {
        // 6 scenarios
        mb.Entity<Scenario>().HasData(
            new { Id = 1, Code = "s1", Name = "输送机构", Icon = "🚚", Desc = "①输送配置标准SOP V2.0", SortOrder = 1 },
            new { Id = 2, Code = "s2", Name = "拍板机构", Icon = "🔧", Desc = "②输送拍板安装SOP V2.0", SortOrder = 2 },
            new { Id = 3, Code = "s3", Name = "手臂机构", Icon = "🦾", Desc = "③手臂掉板完板检知SOP", SortOrder = 3 },
            new { Id = 4, Code = "s4", Name = "模组机构", Icon = "🔩", Desc = "④模组&升降感应器配置SOP", SortOrder = 4 },
            new { Id = 5, Code = "s5", Name = "台车板架", Icon = "📋", Desc = "⑤台车板架确认工位V2.0", SortOrder = 5 },
            new { Id = 6, Code = "s6", Name = "DM/NG板架", Icon = "⚡", Desc = "⑥DM&NG板架感应器配置SOP", SortOrder = 6 }
        );

        // Functions
        var funcs = new (int scenarioId, string code, string name, string icon, string? note)[]
        {
            (1, "f1", "独立输送段", "➡️", null), (1, "f2", "收板机", "📥", null), (1, "f3", "放板机", "📤", null),
            (1, "f4", "可掀式", "🔄", null), (1, "f5", "SB-100太阳式翻板机", "🔁", null), (1, "f6", "SB-309G翻板机", "🔁", null),
            (1, "f7", "SB-503暂存机", "⏱️", null), (1, "f8", "ST-103转向机", "↪️", null), (1, "f9", "三点寻边", "📐", null),
            (1, "f10", "特殊输送", "⚡", null),
            (2, "f11", "拍板光纤选型", "〰️", "根据滚轮间距选择光纤规格"), (2, "f12", "电机拍板", "⚙️", null),
            (2, "f13", "伺服拍板", "🎯", null), (2, "f14", "NG反转拍板", "↩️", null),
            (3, "f20", "掉板检测", "⚠️", "根据制程选择感应器类型"), (3, "f21", "完板检测", "✅", "根据载具类型选择完板方式"),
            (4, "f30", "手臂横移模组", "⬌", null), (4, "f31", "手臂升降模组", "⬆️", null), (4, "f32", "调宽模组", "↔️", null),
            (4, "f33", "台车工位模组", "🚗", null), (4, "f34", "移栽模组", "🔀", null), (4, "f35", "拍板模组定位", "📍", null), (4, "f36", "升降工位", "⬇️", null),
            (5, "f40", "台车到位确认", "🚗", null), (5, "f41", "板架确认", "📦", null),
            (6, "f50", "暂存板架检测", "🗃️", null), (6, "f51", "DM滑台检测", "➡️", null), (6, "f52", "NG板架检测", "🛑", null)
        };

        mb.Entity<ScenarioFunction>().HasData(
            funcs.Select((f, i) => new ScenarioFunction { Id = i + 1, Code = f.code, Name = f.name, Icon = f.icon, Note = f.note, ScenarioId = f.scenarioId, SortOrder = i + 1 }).ToArray()
        );

        // Build function code -> id map
        var funcIdMap = new Dictionary<string, int>();
        for (int i = 0; i < funcs.Length; i++) funcIdMap[funcs[i].code] = i + 1;

        // Conditions
        var condData = new (string funcCode, string code, string name, string? note)[]
        {
            // s1: 输送机构
            ("f1", "c1a", "单段输送（自带PLC）", "入料确认+出料确认"), ("f1", "c1b", "多段输送（自带PLC）", "入料1+入料2+出料"),
            ("f1", "c1c", "单段输送（搭配主设备）", "共用入/出料"), ("f1", "c1d", "多段输送（搭配主设备）", "入/出料1+入/出料2"),
            ("f2", "c2a", "收板机（平板/BOX/TRAY载具）", "入料+拍板起始"), ("f2", "c2b", "收板机（L-RACK载具）", "入料+反转到位+过冲保护"),
            ("f2", "c2c", "收板机（上顶平板）", "入料+减速+拍板起始"), ("f2", "c2d", "收板机（电镀制程）", "光电+近接FAB配置"),
            ("f3", "c3a", "放板机（无NG反转）", "拍板起始+出料"), ("f3", "c3b", "放板机（NG反转）", "拍板起始+出料+NG反转到位"),
            ("f3", "c3c", "放板机（上顶平板）", "拍板起始+出料"), ("f3", "c3d", "放板机（电镀制程）", "光电+近接FAB配置"),
            ("f4", "c4a", "可掀式（滚筒输送）", "入/出料确认+有料确认"), ("f4", "c4b", "可掀式（皮带输送）", "入/出料确认+有料确认"),
            ("f5", "c5a", "SB-100标准间距（滚轮≥20mm）", "4颗感应器, 静电容φ18"), ("f5", "c5b", "SB-100加密间距（滚轮<20mm）", "需φ12静电容TAPT"),
            ("f5", "c5c", "SB-100磁力环输送", "确认φ18能否安装"),
            ("f6", "c6a", "SB-309G翻板机", "入料+翻板保护1/2+出料, 光电+对照"),
            ("f7", "c7a", "SB-503暂存机（标准）", "入料+挡板到位+出料"), ("f7", "c7b", "SB-503暂存机（中间防卡板）", "增加暂存防卡光电"),
            ("f8", "c8a", "ST-103转向机（无搭配）", "入料+中心定位+挡板+出料"), ("f8", "c8b", "ST-103转向机（带延伸段）", "增加延伸入/出料确认"),
            ("f9", "c9a", "三点寻边（标准模式）", "入料+减速+到位+激光1/2"), ("f9", "c9b", "三点寻边（测板宽模式）", "同标准模式"),
            ("f10", "c10a", "电镀制程配置", "光电+近接FAB替代光电+对照"), ("f10", "c10b", "鹏鼎HC01带平板", "入料+防卡+减速+拍板起始"),
            ("f10", "c10c", "读码触发输送", "拍板起始+读码触发+出料"), ("f10", "c10d", "小板件多列进板", "优先使用安全光栅"),
            ("f10", "c10e", "三点式输送", "入料+到位, 斜对照安装"), ("f10", "c10f", "延伸输送段", "本体+延伸入/出料"),
            // s2: 拍板机构
            ("f11", "c11a", "滚轮间距>55mm，平板开口>10mm", "M6光纤 FU-6F+FS-N18N"), ("f11", "c11b", "滚轮间距<55mm或平板开口<10mm", "M3光纤 FU-35FA"),
            ("f12", "c12a", "电机中心/靠边拍板", "4颗M6光纤+张/夹定位KN"), ("f12", "c12b", "电机序列拍板", "左右张/夹定位各2颗KN"),
            ("f13", "c13a", "伺服中心拍板", "4颗M6光纤+张/夹定位PM-L45"), ("f13", "c13b", "伺服序列拍板", "左右张/夹定位各2颗PM-L45"),
            ("f13", "c13c", "伺服拍板（光纤防呆模式）", "光纤防呆+张/夹定位PM-L45"), ("f13", "c13d", "伺服拍板（无光纤模式-鹏鼎）", "仅张/夹定位PM-L45"),
            ("f13", "c13e", "伺服拍板（大小板模式）", "左右光纤+小板+大板定位"),
            ("f14", "c14a", "NG反转（大小板位置差别不大）", "一套光纤兼容"), ("f14", "c14b", "NG反转（大小板位置差别大）", "需多套光纤"),
            // s3: 手臂机构
            ("f20", "c20a", "电镀制程", "掉板检测1+2:近接FAB(对角)+测高KN"), ("f20", "c20b", "非电镀制程", "掉板检测1+2:静电容EC18(对角)+测高KN"),
            ("f20", "c20c", "无孔制程（集成真空）", "掉板检测1+2:真空数显DPB01N"), ("f20", "c20d", "隔纸设备", "纸板判别1+2:近接FAB+掉板:光电GD+EZ+测高KN"),
            ("f20", "c20e", "ESG勾Tray（无吸真空）", "掉TRAY检知1+2:槽型PM-L45(对角)+Tray确认光电GD"),
            ("f20", "c20f", "FPC软板", "掉板检测1+2:光电GD-B31N(对角, 禁止静电容)"),
            ("f20", "c20g", "插框手臂", "FU-32夹爪+GU-31托爪+防压PM-L45+防呆FBB"),
            ("f20", "c20h", "宏恒胜吸盘（测高式）", "掉板1+2:PM-L45+纸板判别1+2:φ30近接I1CN+测高1+2:KN"),
            ("f21", "c21a", "L-RACK有孔可装镜片（优先）", "镜片PZ-G61N"), ("f21", "c21b", "L-RACK有孔不能装镜片", "光电GD-B31N"),
            ("f21", "c21c", "L-RACK可贴色标", "颜色传感器"), ("f21", "c21e", "金属平板有孔可装镜片", "镜片PZ-G61N"),
            ("f21", "c21f", "金属平板无孔不能装镜片", "光电GD-B31N"), ("f21", "c21g", "电木/非金属平板", "近接FAB-18D16N1"),
            ("f21", "c21h", "BOX载具（马达带入）", "对照PZ-G51N"), ("f21", "c21i", "TRAY载具", "镜片PZ-G61N板架+对照PZ-G51N牙叉"),
            ("f21", "c21j", "插框载具", "对照PZ-G51N前后"), ("f21", "c21k", "L插架普通到位", "对照光电PZ-G51N"),
            ("f21", "c21l", "L插架PIN定位（普通）", "到位确认1+2:大黄KB+正反判别EZ"), ("f21", "c21m", "L插架PIN定位（精密）", "到位确认1+2:齐平式FBB+正反判别EZ"),
            // s4: 模组机构
            ("f30", "c30a", "标准横移模组", "前极限+原点+后极限, PM-L45"),
            ("f31", "c31a", "标准模组", "上极限+原点+下极限, PM-L45"), ("f31", "c31b", "自制模组（125系列）", "上极限+原点+下极限, PM-Y45"),
            ("f32", "c32a", "伺服调宽模组", "前极限+原点+后极限, PM-L45"), ("f32", "c32b", "电缸调宽", "原点+下极限"),
            ("f33", "c33a", "牙叉升降模组", "上极限+完/满料预警+下减速+原点+下极限"), ("f33", "c33b", "牙叉暂存升降模组", "上极限+原点+下极限, PM-Y45"),
            ("f33", "c33c", "双模组升降（非龙门）", "左右各3颗PM-L45"), ("f33", "c33d", "双模组升降（龙门）", "防呆原点+区域判断"),
            ("f34", "c34a", "常规移栽模组", "前极限+原点+后极限"), ("f34", "c34b", "多工位对接移栽", "定点1/2/3/n"),
            ("f35", "c35a", "伺服中心拍板", "张定位+夹定位PM-L45"), ("f35", "c35b", "伺服序列拍板", "左右张/夹定位PM-L45"),
            ("f36", "c36a", "牙叉升降工位", "定位PZ+有料确认1+2+安全确认+台车确认"),
            ("f36", "c36b", "牙叉暂存工位", "有料确认1+2+满料确认"), ("f36", "c36c", "牙叉板台工位", "定位+极限+有料确认1+2+安全确认"),
            ("f36", "c36d", "板/纸台升降（三相马达）", "定位+有料+极限"), ("f36", "c36e", "板/纸台升降（单相马达）", "漫反射定位+有料+极限"),
            // s5: 台车板架
            ("f40", "c40a", "固定台车（外部对接）", "2颗大黄KB-3020N"), ("f40", "c40b", "固定台车（内部对接无导引）", "2颗大黄KB-3020N"),
            ("f40", "c40c", "固定台车（内部对接有导引）", "1颗大黄KB-3020N"), ("f40", "c40d", "油压台车（外部对接）", "2颗大黄KB-3020N"),
            ("f40", "c40e", "气缸举升台车（外部对接）", "2颗大黄KB-3020N"), ("f40", "c40f", "一体式举升台车（内部对接）", "1颗大黄KB-3020N"),
            ("f40", "c40g", "AGV台车（光栅能检测）", "对照PZ-G51N+安全光栅"), ("f40", "c40h", "AGV台车（光栅不能检测）", "对照PZ-G51N+安全光栅+额外对照"),
            ("f40", "c40i", "AGV台车（深南/CCTC镜片）", "AGV到位PZ-G51N+退出PZ-G51N+进入PZ-G61N"),
            ("f41", "c41a", "L-RACK扣锁", "2颗大黄KB-3020N"), ("f41", "c41b", "L-RACK PIN定位（普通）", "2颗大黄KB-3020N"),
            ("f41", "c41c", "L-RACK PIN定位（精密）", "2颗齐平式FBB-18D12N2"), ("f41", "c41d", "金属平板扣锁", "2颗大黄KB-3020N"),
            ("f41", "c41e", "电木平板（非金属）", "对照光电PZ-G51N"), ("f41", "c41f", "BOX扣锁（马达带入）", "BOX有料确认1+2对照PZ-G51N"),
            ("f41", "c41g", "BOX扣锁（手动推入）", "到位+有料确认"), ("f41", "c41h", "TRAY扣锁", "TRAY有料确认1+2对照PZ-G51N"),
            ("f41", "c41i", "插框工位（输送运转）", "入料+到位对照PZ-G51N"), ("f41", "c41j", "L插架（普通到位）", "对照光电PZ-G51N"),
            ("f41", "c41k", "L插架（PIN定位-普通）", "到位确认1+2:大黄KB+正反判别EZ"), ("f41", "c41l", "L插架（PIN定位-精密）", "到位确认1+2:齐平式FBB+正反判别EZ"),
            // s6: DM/NG板架
            ("f50", "c50a", "斜立式L-RACK暂存", "镜片PZ-G61N+近接KN到位"), ("f50", "c50b", "移栽平台", "有料确认1:GD+有料确认2:FAB"),
            ("f50", "c50c", "多层暂存架", "每层镜片PZ-G61N+顶部光电GD"),
            ("f51", "c51a", "DM滑台", "镜片PZ-G61N+对照PZ-G51N"), ("f51", "c51b", "多层滑台", "每层镜片PZ-G61N"),
            ("f52", "c52a", "NG板架（固定拉门）", "镜片PZ-G61N+电子锁QSD"), ("f52", "c52b", "暂存牙叉满料确认", "有料确认1+2+满料确认PZ-G51N")
        };

        mb.Entity<FunctionCondition>().HasData(
            condData.Select((c, i) => new FunctionCondition { Id = i + 1, Code = c.code, Name = c.name, Note = c.note, FunctionId = funcIdMap[c.funcCode], SortOrder = i + 1 }).ToArray()
        );
    }

    private static void SeedRules(ModelBuilder mb)
    {
        var (ruleEntities, ruleProductEntities) = BuildDefaultRules();
        mb.Entity<SelectionRule>().HasData(ruleEntities.ToArray());
        mb.Entity<RuleProduct>().HasData(ruleProductEntities.ToArray());
    }

    private static (List<SelectionRule> Rules, List<RuleProduct> RuleProducts) BuildDefaultRules()
    {
        // Build product code -> id map
        var productCodes = new[] { "p1", "p2", "p3", "p4", "p4b", "p4c", "p5", "p6", "p7", "p8", "p9", "p10", "p11", "p12", "p13", "p14", "p15", "p15b", "p15c", "p16", "p17", "p18", "p19", "p20", "p21", "p22", "p23", "p24", "p25" };
        var productIdMap = new Dictionary<string, int>();
        for (int i = 0; i < productCodes.Length; i++) productIdMap[productCodes[i]] = i + 1;

        // Build condition code -> id map
        var condCodes = new[] {
            "c1a","c1b","c1c","c1d","c2a","c2b","c2c","c2d","c3a","c3b","c3c","c3d","c4a","c4b",
            "c5a","c5b","c5c","c6a","c7a","c7b","c8a","c8b","c9a","c9b",
            "c10a","c10b","c10c","c10d","c10e","c10f",
            "c11a","c11b","c12a","c12b","c13a","c13b","c13c","c13d","c13e","c14a","c14b",
            "c20a","c20b","c20c","c20d","c20e","c20f","c20g","c20h",
            "c21a","c21b","c21c","c21e","c21f","c21g","c21h","c21i","c21j","c21k","c21l","c21m",
            "c30a","c31a","c31b","c32a","c32b","c33a","c33b","c33c","c33d","c34a","c34b","c35a","c35b",
            "c36a","c36b","c36c","c36d","c36e",
            "c40a","c40b","c40c","c40d","c40e","c40f","c40g","c40h","c40i",
            "c41a","c41b","c41c","c41d","c41e","c41f","c41g","c41h","c41i","c41j","c41k","c41l",
            "c50a","c50b","c50c","c51a","c51b","c52a","c52b"
        };
        var condIdMap = new Dictionary<string, int>();
        for (int i = 0; i < condCodes.Length; i++) condIdMap[condCodes[i]] = i + 1;

        // Function code -> id map (same as above)
        var funcCodes = new[] { "f1","f2","f3","f4","f5","f6","f7","f8","f9","f10","f11","f12","f13","f14","f20","f21","f30","f31","f32","f33","f34","f35","f36","f40","f41","f50","f51","f52" };
        var funcIdMap = new Dictionary<string, int>();
        for (int i = 0; i < funcCodes.Length; i++) funcIdMap[funcCodes[i]] = i + 1;

        // Scenario code -> id
        var scenarioIdMap = new Dictionary<string, int> { { "s1", 1 }, { "s2", 2 }, { "s3", 3 }, { "s4", 4 }, { "s5", 5 }, { "s6", 6 } };

        // Rules: (code, scenarioCode, funcCode, condCode, note, productCodeWithQty[])
        var rules = new (string code, string scenarioCode, string funcCode, string condCode, string? note, string[] products)[]
        {
            ("r1001","s1","f1","c1a","入料确认+出料确认 | 作用:1.触发省电模式 2.切换输送速度 3.卡板检知", new[]{"p5:2","p21:2"}),
            ("r1002","s1","f1","c1b","入料1+入料2+出料", new[]{"p5:3","p21:3"}),
            ("r1003","s1","f1","c1c","共用入/出料确认", new[]{"p5:1","p21:1"}),
            ("r1004","s1","f1","c1d","入/出料1+入/出料2", new[]{"p5:3","p21:3"}),
            ("r2001","s1","f2","c2a","入料确认+拍板起始(光电GD+光电EZ)", new[]{"p5:2","p21:1","p6:1"}),
            ("r2002","s1","f2","c2b","入料+反转到位+过冲保护", new[]{"p5:3","p21:2","p6:1"}),
            ("r2003","s1","f2","c2c","入料+减速+拍板起始", new[]{"p5:2","p21:1","p6:2"}),
            ("r2004","s1","f2","c2d","电镀制程:近接FAB+对照EZ-T52", new[]{"p1:2","p21:1","p6:1"}),
            ("r3001","s1","f3","c3a","拍板起始+出料确认", new[]{"p5:2","p6:1","p21:1"}),
            ("r3002","s1","f3","c3b","拍板起始+出料+NG反转到位", new[]{"p5:3","p6:2","p21:1"}),
            ("r3003","s1","f3","c3c","拍板起始+出料", new[]{"p5:2","p6:1","p21:1"}),
            ("r3004","s1","f3","c3d","电镀制程:近接FAB+光电EZ", new[]{"p1:2","p6:1","p21:1"}),
            ("r4001","s1","f4","c4a","入/出料确认(GD+EZ)+有料确认(GD+EZ) | 注意:有料时可掀式不可下降", new[]{"p5:3","p6:3"}),
            ("r4002","s1","f4","c4b","入/出料确认(GD+EZ)+有料确认(GD+EZ)", new[]{"p5:3","p6:3"}),
            ("r5001","s1","f5","c5a","省电+入料(静电容φ18)+出料(静电容φ18)", new[]{"p5:3","p21:1","p10:2"}),
            ("r5002","s1","f5","c5b","滚轮间距<20mm:需φ12静电容TAPT-12D08N1", new[]{"p5:3","p21:1","p11:2"}),
            ("r5003","s1","f5","c5c","磁力环:确认φ18能否安装", new[]{"p5:3","p21:1","p11:2"}),
            ("r6001","s1","f6","c6a","入料+翻板保护1/2+出料 | 注意:使用光电+对照配置", new[]{"p5:4","p6:2","p21:2"}),
            ("r7001","s1","f7","c7a","入料+挡板到位(GTB2S+对照)+出料 | 挡板对照需固定位置", new[]{"p5:2","p21:2","p9:1","p22:1"}),
            ("r7002","s1","f7","c7b","增加暂存防卡光电(GTB2S两颗)防止压伤板件", new[]{"p5:2","p21:2","p9:3","p22:1"}),
            ("r8001","s1","f8","c8a","入料+中心定位1+2(FU-6F)+挡板+出料", new[]{"p5:2","p21:2","p14:2","p9:1","p22:1"}),
            ("r8002","s1","f8","c8b","增加延伸入/出料确认", new[]{"p5:3","p6:1","p9:2","p14:2","p21:3","p22:2"}),
            ("r9001","s1","f9","c9a","入料+减速+到位(近接FAB)+激光", new[]{"p5:3","p21:3","p1:1","p16:2","p17:2"}),
            ("r9002","s1","f9","c9b","测板宽模式", new[]{"p5:3","p21:3","p1:1","p16:2","p17:2"}),
            ("r10001","s1","f10","c10a","电镀制程配置", new[]{"p1:3","p6:2","p21:1"}),
            ("r10002","s1","f10","c10b","HC01:入料+防卡(FU-6F)+减速+拍板起始", new[]{"p5:4","p6:2","p14:1","p21:1"}),
            ("r10003","s1","f10","c10c","拍板起始+读码触发+出料", new[]{"p5:3","p6:2","p21:1"}),
            ("r10004","s1","f10","c10d","小板件多列:优先使用安全光栅", new[]{"p18:2"}),
            ("r10005","s1","f10","c10e","三点式输送:入料+到位 | 注意:斜对照安装", new[]{"p5:2","p21:2"}),
            ("r10006","s1","f10","c10f","本体+延伸入/出料确认", new[]{"p5:4","p6:2","p21:2"}),
            // 拍板
            ("r11001","s2","f11","c11a","M6光纤FU-6F:滚轮间距>55mm", new[]{"p14:4"}),
            ("r11002","s2","f11","c11b","M3光纤FU-35FA:滚轮间距<55mm", new[]{"p15:4"}),
            ("r11101","s2","f12","c12a","4颗M6光纤+张/夹定位KN-1705N", new[]{"p14:4","p4:2"}),
            ("r11102","s2","f12","c12b","左右张/夹定位各2颗KN-1705N", new[]{"p14:4","p4:4"}),
            ("r11201","s2","f13","c13a","4颗M6光纤+张/夹定位PM-L45", new[]{"p14:4","p12:2"}),
            ("r11202","s2","f13","c13b","左右张/夹定位各2颗PM-L45", new[]{"p14:4","p12:4"}),
            ("r11203","s2","f13","c13c","光纤防呆+张/夹定位PM-L45", new[]{"p14:2","p12:2"}),
            ("r11204","s2","f13","c13d","鹏鼎等无光纤模式", new[]{"p12:2"}),
            ("r11205","s2","f13","c13e","左右光纤+小板+大板定位PM-L45", new[]{"p14:2","p12:3"}),
            ("r11301","s2","f14","c14a","大小板位置差别不大:一套光纤装于共用位置", new[]{"p14:2","p12:2"}),
            ("r11302","s2","f14","c14b","大小板位置差别大:需多套光纤", new[]{"p14:4","p12:4"}),
            // 手臂
            ("r20001","s3","f20","c20a","电镀制程", new[]{"p1:2","p4b:1"}),
            ("r20002","s3","f20","c20b","非电镀", new[]{"p10:2","p4b:1"}),
            ("r20003","s3","f20","c20c","无孔制程(集成真空)", new[]{"p24:2"}),
            ("r20004","s3","f20","c20d","隔纸", new[]{"p1:2","p5:1","p6:1","p4b:1"}),
            ("r20005","s3","f20","c20e","ESG勾Tray", new[]{"p12:2","p5:1"}),
            ("r20006","s3","f20","c20f","FPC软板禁止静电容", new[]{"p5:2"}),
            ("r20007","s3","f20","c20g","插框:夹爪+托爪+防压+防呆", new[]{"p15b:2","p15c:2","p12:1","p2:2"}),
            ("r20008","s3","f20","c20h","宏恒胜", new[]{"p12:2","p4c:2","p4b:2"}),
            ("r21001","s3","f21","c21a","L-RACK有孔可装镜片(优先)", new[]{"p8:1"}),
            ("r21002","s3","f21","c21b","L-RACK有孔不能装", new[]{"p5:1"}),
            ("r21003","s3","f21","c21c","L-RACK可贴色标", new[]{"p23:1"}),
            ("r21005","s3","f21","c21e","金属平板有孔", new[]{"p8:1"}),
            ("r21006","s3","f21","c21f","金属平板无孔", new[]{"p5:1"}),
            ("r21007","s3","f21","c21g","电木平板", new[]{"p1:1"}),
            ("r21009","s3","f21","c21h","BOX载具", new[]{"p7:2"}),
            ("r21010","s3","f21","c21i","TRAY载具", new[]{"p8:1","p7:1"}),
            ("r21011","s3","f21","c21j","插框载具", new[]{"p7:2"}),
            ("r21012","s3","f21","c21k","L插架普通到位", new[]{"p7:1"}),
            ("r21013","s3","f21","c21l","L插架PIN定位(普通)", new[]{"p3:2","p6:1"}),
            ("r21014","s3","f21","c21m","L插架PIN定位(精密)", new[]{"p2:2","p6:1"}),
            // 模组
            ("r30001","s4","f30","c30a","横移模组", new[]{"p12:3"}),
            ("r31001","s4","f31","c31a","标准升降", new[]{"p12:3"}),
            ("r31002","s4","f31","c31b","125系列自制", new[]{"p13:3"}),
            ("r32001","s4","f32","c32a","伺服调宽", new[]{"p12:3"}),
            ("r32002","s4","f32","c32b","电缸调宽", new[]{"p12:2"}),
            ("r33001","s4","f33","c33a","牙叉升降", new[]{"p12:5"}),
            ("r33002","s4","f33","c33b","牙叉暂存", new[]{"p13:3"}),
            ("r33003","s4","f33","c33c","双模组非龙门", new[]{"p12:6"}),
            ("r33004","s4","f33","c33d","双模组龙门", new[]{"p12:7"}),
            ("r34001","s4","f34","c34a","常规移栽", new[]{"p12:3"}),
            ("r34002","s4","f34","c34b","多工位对接", new[]{"p12:6"}),
            ("r35001","s4","f35","c35a","伺服中心拍板", new[]{"p12:2"}),
            ("r35002","s4","f35","c35b","伺服序列拍板", new[]{"p12:4"}),
            ("r36001","s4","f36","c36a","牙叉升降工位", new[]{"p7:4","p5:2","p3:1"}),
            ("r36002","s4","f36","c36b","牙叉暂存工位", new[]{"p7:3"}),
            ("r36003","s4","f36","c36c","牙叉板台工位", new[]{"p7:1","p5:3","p19:2","p4:2"}),
            ("r36004","s4","f36","c36d","板/纸台三相", new[]{"p7:1","p5:2","p19:2","p4:2"}),
            ("r36005","s4","f36","c36e","板/纸台单相", new[]{"p5:3","p19:2","p4:2"}),
            // 台车板架
            ("r40001","s5","f40","c40a","固定台车外部对接", new[]{"p3:2"}),
            ("r40002","s5","f40","c40b","固定台车内部对接无导引", new[]{"p3:2"}),
            ("r40003","s5","f40","c40c","固定台车内部对接有导引", new[]{"p3:1"}),
            ("r40004","s5","f40","c40d","油压台车外部对接", new[]{"p3:2"}),
            ("r40005","s5","f40","c40e","气缸举升台车外部对接", new[]{"p3:2"}),
            ("r40006","s5","f40","c40f","一体式举升台车内部对接", new[]{"p3:1"}),
            ("r40007","s5","f40","c40g","AGV台车(光栅能检测)", new[]{"p7:1","p18:1"}),
            ("r40008","s5","f40","c40h","AGV台车(光栅不能检测)", new[]{"p7:2","p18:1"}),
            ("r40009","s5","f40","c40i","深南/CCTC镜片方案", new[]{"p7:2","p8:1"}),
            ("r41001","s5","f41","c41a","L-RACK扣锁", new[]{"p3:2"}),
            ("r41002","s5","f41","c41b","L-RACK PIN定位(普通)", new[]{"p3:2"}),
            ("r41003","s5","f41","c41c","L-RACK PIN定位(精密)", new[]{"p2:2"}),
            ("r41004","s5","f41","c41d","金属平板扣锁", new[]{"p3:2"}),
            ("r41005","s5","f41","c41e","电木平板(非金属)", new[]{"p7:1"}),
            ("r41006","s5","f41","c41f","BOX马达带入", new[]{"p7:2"}),
            ("r41007","s5","f41","c41g","BOX手动推入", new[]{"p7:2"}),
            ("r41008","s5","f41","c41h","TRAY扣锁", new[]{"p7:2"}),
            ("r41009","s5","f41","c41i","插框输送", new[]{"p7:2"}),
            ("r41010","s5","f41","c41j","L插架普通到位", new[]{"p7:1"}),
            ("r41011","s5","f41","c41k","L插架PIN(普通)", new[]{"p3:2","p6:1"}),
            ("r41012","s5","f41","c41l","L插架PIN(精密)", new[]{"p2:2","p6:1"}),
            // DM/NG板架
            ("r50001","s6","f50","c50a","斜立式L-RACK", new[]{"p8:1","p4:1"}),
            ("r50002","s6","f50","c50b","移栽平台", new[]{"p5:2","p1:2"}),
            ("r50003","s6","f50","c50c","多层暂存架", new[]{"p8:1","p5:1"}),
            ("r51001","s6","f51","c51a","DM滑台", new[]{"p8:1","p7:1"}),
            ("r51002","s6","f51","c51b","多层滑台", new[]{"p8:1"}),
            ("r52001","s6","f52","c52a","NG板架固定拉门", new[]{"p8:1","p20:1"}),
            ("r52002","s6","f52","c52b","暂存牙叉", new[]{"p7:3"})
        };

        var ruleEntities = new List<SelectionRule>();
        var ruleProductEntities = new List<RuleProduct>();
        int ruleId = 0;
        int rpId = 0;

        foreach (var r in rules)
        {
            ruleId++;
            ruleEntities.Add(new SelectionRule
            {
                Id = ruleId,
                Code = r.code,
                ScenarioId = scenarioIdMap[r.scenarioCode],
                FunctionId = funcIdMap[r.funcCode],
                ConditionId = condIdMap[r.condCode],
                Note = r.note
            });

            foreach (var pq in r.products)
            {
                var parts = pq.Split(':');
                var pCode = parts[0];
                var qty = parts.Length > 1 ? int.Parse(parts[1]) : 1;
                rpId++;
                ruleProductEntities.Add(new RuleProduct
                {
                    Id = rpId,
                    RuleId = ruleId,
                    ProductId = productIdMap[pCode],
                    Quantity = qty
                });
            }
        }

        return (ruleEntities, ruleProductEntities);
    }

    private static void SeedProcessScenarios(ModelBuilder mb)
    {
        mb.Entity<ProcessScenario>().HasData(
            new { Id = 1, Code = "ps1", Name = "电镀制程（含开料/外层前处理）", Icon = "⚡", Desc = "金属含量高, 近接替代光电检测", SopSource = "输送SOP第五章 + 手臂SOP第二章第1节", Category = (string?)null, SortOrder = 1 },
            new { Id = 2, Code = "ps2", Name = "非电镀制程（普通制程）", Icon = "🔧", Desc = "标准光电+静电容配置, PCB有孔板件的标准制程", SopSource = "输送SOP第一章 + 手臂SOP第二章第2节", Category = (string?)null, SortOrder = 2 },
            new { Id = 3, Code = "ps3", Name = "无孔制程（特殊）", Icon = "⭕", Desc = "集成真空/中央真空时使用真空数显检测", SopSource = "手臂SOP第二章第3节", Category = (string?)null, SortOrder = 3 },
            new { Id = 4, Code = "ps4", Name = "隔纸设备（特殊）", Icon = "📄", Desc = "需区分纸和板, 近接纸板判别+光电掉板", SopSource = "手臂SOP第二章第4节", Category = (string?)null, SortOrder = 4 },
            new { Id = 5, Code = "ps5", Name = "ESG勾Tray（无吸真空）", Icon = "📦", Desc = "无吸真空, 使用槽型光电掉Tray", SopSource = "手臂SOP第二章第5节", Category = (string?)null, SortOrder = 5 },
            new { Id = 6, Code = "ps6", Name = "插框手臂", Icon = "🔩", Desc = "夹爪抓板方式, 光纤确认+槽型防压+近接防呆", SopSource = "手臂SOP第二章第6节", Category = (string?)null, SortOrder = 6 },
            new { Id = 7, Code = "ps7", Name = "FPC软板（板件类型）", Icon = "🔶", Desc = "板件类型分支, 禁止静电容掉板, 使用BGS漫反射光电", SopSource = "手臂SOP第一章选型逻辑图(类型维度)", Category = "board_type", SortOrder = 7 }
        );

        mb.Entity<AffectedMechanism>().HasData(
            // ps1 - 电镀
            new { Id = 1, ProcessScenarioId = 1, MechanismCode = "s1", MechanismName = "输送机构", ChangeDesc = "入出料确认: 对照EZ-T52 + 近接FAB-18D16N1-D3 (近接替代光电)", ChangeDescDetail = "拍板减速/有料确认: 光电GD-B31N + 近接FAB-18D16N1-D3", ChangeDescDetail2 = "拍板起始: 光电GD-B31N + 近接FAB-18D16N1-D3", InstallNote = "近接用在有上顶小白轮时需固定在上顶机构同步升降; 有上顶平板时平板开孔25×35mm", Condition = (string?)null, RelatedConditions = "c10a" },
            new { Id = 2, ProcessScenarioId = 1, MechanismCode = "s3", MechanismName = "手臂掉板", ChangeDesc = "掉板检测1+2: 近接FAB-18D16N1-D3(对角安装两颗)", ChangeDescDetail = "吸盘测高: 近接KN-SO3N2侧边(N.C)", ChangeDescDetail2 = (string?)null, InstallNote = (string?)null, Condition = (string?)null, RelatedConditions = "c20a" },
            // ps2 - 非电镀
            new { Id = 3, ProcessScenarioId = 2, MechanismCode = "s1", MechanismName = "输送机构", ChangeDesc = "入出料确认: 对照EZ-T52 + 光电GD-B31N", ChangeDescDetail = "拍板减速/有料确认: 光电GD-B31N + 光电EZ-GB52S", ChangeDescDetail2 = "拍板起始: 光电GD-B31N + 光电EZ-GB52S", InstallNote = (string?)null, Condition = (string?)null, RelatedConditions = "c1a" },
            new { Id = 4, ProcessScenarioId = 2, MechanismCode = "s3", MechanismName = "手臂掉板", ChangeDesc = "掉板检测1+2: 静电容EC18-N20(对角安装两颗)", ChangeDescDetail = "吸盘测高: 近接KN-SO3N2侧边(N.C)", ChangeDescDetail2 = (string?)null, InstallNote = (string?)null, Condition = (string?)null, RelatedConditions = "c20b" },
            // ps3 - 无孔
            new { Id = 5, ProcessScenarioId = 3, MechanismCode = "s3", MechanismName = "手臂掉板", ChangeDesc = "掉板检测1+2: 真空数显表DPB01N(依真空产生器数量)", ChangeDescDetail = "吸盘测高: 近接KN-SO3N2侧边(N.C)", ChangeDescDetail2 = (string?)null, InstallNote = (string?)null, Condition = "集成真空或中央真空时", RelatedConditions = "c20c" },
            // ps4 - 隔纸
            new { Id = 6, ProcessScenarioId = 4, MechanismCode = "s3", MechanismName = "手臂掉板", ChangeDesc = "纸板判别1+2: 近接FAB-18D16N1-D3", ChangeDescDetail = "掉板检测: 背景抑制光电GD-B31N + 背景抑制光电EZ-GB52S", ChangeDescDetail2 = "吸盘测高: 近接KN-SO3N2侧边(N.C)", InstallNote = (string?)null, Condition = (string?)null, RelatedConditions = "c20d" },
            // ps5 - ESG
            new { Id = 7, ProcessScenarioId = 5, MechanismCode = "s3", MechanismName = "手臂掉板", ChangeDesc = "掉TRAY检知1+2: 槽型光电PM-L45(对角安装两颗)", ChangeDescDetail = "TRAY有板确认: 背景抑制光电GD-B31N", ChangeDescDetail2 = (string?)null, InstallNote = (string?)null, Condition = (string?)null, RelatedConditions = "c20e" },
            // ps6 - 插框
            new { Id = 8, ProcessScenarioId = 6, MechanismCode = "s3", MechanismName = "手臂掉板", ChangeDesc = "上夹爪有板确认1+2: 光纤FU-32(基恩士)", ChangeDescDetail = "下托爪有板确认1+2: 光纤GU-31(京东方)", ChangeDescDetail2 = "防压光电: 槽型PM-L45 + 前进防呆1+2: 近接FBB-18D12N2-D3", InstallNote = (string?)null, Condition = (string?)null, RelatedConditions = "c20g" },
            // ps7 - FPC
            new { Id = 9, ProcessScenarioId = 7, MechanismCode = "s3", MechanismName = "手臂掉板", ChangeDesc = "掉板检测1+2: BGS漫反射光电(对角安装两颗)", ChangeDescDetail = "FPC板子软容易下垂, 禁止使用静电容掉板容易感应不良", ChangeDescDetail2 = (string?)null, InstallNote = (string?)null, Condition = (string?)null, RelatedConditions = "c20f" }
        );

        mb.Entity<UnaffectedMechanism>().HasData(
            // ps1
            new { Id = 1, ProcessScenarioId = 1, MechanismCode = "s2" },
            new { Id = 2, ProcessScenarioId = 1, MechanismCode = "s4" },
            new { Id = 3, ProcessScenarioId = 1, MechanismCode = "s5" },
            new { Id = 4, ProcessScenarioId = 1, MechanismCode = "s6" },
            // ps2
            new { Id = 5, ProcessScenarioId = 2, MechanismCode = "s2" },
            new { Id = 6, ProcessScenarioId = 2, MechanismCode = "s4" },
            new { Id = 7, ProcessScenarioId = 2, MechanismCode = "s5" },
            new { Id = 8, ProcessScenarioId = 2, MechanismCode = "s6" },
            // ps3
            new { Id = 9, ProcessScenarioId = 3, MechanismCode = "s1" },
            new { Id = 10, ProcessScenarioId = 3, MechanismCode = "s2" },
            new { Id = 11, ProcessScenarioId = 3, MechanismCode = "s4" },
            new { Id = 12, ProcessScenarioId = 3, MechanismCode = "s5" },
            new { Id = 13, ProcessScenarioId = 3, MechanismCode = "s6" },
            // ps4
            new { Id = 14, ProcessScenarioId = 4, MechanismCode = "s1" },
            new { Id = 15, ProcessScenarioId = 4, MechanismCode = "s2" },
            new { Id = 16, ProcessScenarioId = 4, MechanismCode = "s4" },
            new { Id = 17, ProcessScenarioId = 4, MechanismCode = "s5" },
            new { Id = 18, ProcessScenarioId = 4, MechanismCode = "s6" },
            // ps5
            new { Id = 19, ProcessScenarioId = 5, MechanismCode = "s1" },
            new { Id = 20, ProcessScenarioId = 5, MechanismCode = "s2" },
            new { Id = 21, ProcessScenarioId = 5, MechanismCode = "s4" },
            new { Id = 22, ProcessScenarioId = 5, MechanismCode = "s5" },
            new { Id = 23, ProcessScenarioId = 5, MechanismCode = "s6" },
            // ps6
            new { Id = 24, ProcessScenarioId = 6, MechanismCode = "s1" },
            new { Id = 25, ProcessScenarioId = 6, MechanismCode = "s2" },
            new { Id = 26, ProcessScenarioId = 6, MechanismCode = "s4" },
            new { Id = 27, ProcessScenarioId = 6, MechanismCode = "s5" },
            new { Id = 28, ProcessScenarioId = 6, MechanismCode = "s6" },
            // ps7
            new { Id = 29, ProcessScenarioId = 7, MechanismCode = "s1" },
            new { Id = 30, ProcessScenarioId = 7, MechanismCode = "s2" },
            new { Id = 31, ProcessScenarioId = 7, MechanismCode = "s4" },
            new { Id = 32, ProcessScenarioId = 7, MechanismCode = "s5" },
            new { Id = 33, ProcessScenarioId = 7, MechanismCode = "s6" }
        );
    }
}
