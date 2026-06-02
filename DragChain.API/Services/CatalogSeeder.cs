using Microsoft.EntityFrameworkCore;
using DragChain.API.Data;
using DragChain.API.Models;

namespace DragChain.API.Services;

public static class CatalogSeeder
{
    private static readonly List<PipeType> DefaultPipeTypes = new()
    {
        new PipeType { Id = 1,  Name = "感應器信號電纜 Φ6",    Type = "weak_cable",   Diameter = 6.0m,   Weight = 0.0600m,  BendMultiplier = 8 },
        new PipeType { Id = 2,  Name = "感應器信號電纜 Φ10",   Type = "weak_cable",   Diameter = 10.0m,  Weight = 0.1500m,  BendMultiplier = 8 },
        new PipeType { Id = 3,  Name = "感應器信號電纜 Φ13",   Type = "weak_cable",   Diameter = 13.0m,  Weight = 0.2400m,  BendMultiplier = 8 },
        new PipeType { Id = 4,  Name = "台達伺服電源線 Φ9",     Type = "strong_cable", Diameter = 9.0m,   Weight = 0.1000m,  BendMultiplier = 8 },
        new PipeType { Id = 5,  Name = "匯川伺服電源線 Φ6.5",  Type = "strong_cable", Diameter = 6.5m,   Weight = 0.0800m,  BendMultiplier = 8 },
        new PipeType { Id = 6,  Name = "SMC電動缸電源線 Φ6.5", Type = "strong_cable", Diameter = 6.5m,   Weight = 0.0800m,  BendMultiplier = 8 },
        new PipeType { Id = 7,  Name = "伺服編碼器線 Φ6",       Type = "encoder", Diameter = 6.0m,   Weight = 0.0600m,  BendMultiplier = 13 },
        new PipeType { Id = 8,  Name = "SMC電動缸信號線 Φ6",  Type = "encoder", Diameter = 6.0m,   Weight = 0.0600m,  BendMultiplier = 13 },
        new PipeType { Id = 9,  Name = "EtherCAT通信線 Φ6",   Type = "weak_cable",   Diameter = 6.0m,   Weight = 0.0550m,  BendMultiplier = 8 },
        new PipeType { Id = 10, Name = "DeviceNet總線 Φ8",     Type = "weak_cable",   Diameter = 8.0m,   Weight = 0.0900m,  BendMultiplier = 8 },
        new PipeType { Id = 11, Name = "氣管 Φ4",              Type = "tube",    Diameter = 4.0m,   Weight = 0.0080m,  BendMultiplier = 8 },
        new PipeType { Id = 12, Name = "氣管 Φ6",              Type = "tube",    Diameter = 6.0m,   Weight = 0.0193m,  BendMultiplier = 8 },
        new PipeType { Id = 13, Name = "氣管 Φ8",              Type = "tube",    Diameter = 8.0m,   Weight = 0.0366m,  BendMultiplier = 8 },
        new PipeType { Id = 14, Name = "氣管 Φ10",             Type = "tube",    Diameter = 10.0m,  Weight = 0.0544m,  BendMultiplier = 8 },
        new PipeType { Id = 15, Name = "氣管 Φ12",             Type = "tube",    Diameter = 12.0m,  Weight = 0.0756m,  BendMultiplier = 8 },
    };

    private static readonly List<WzlCatalog> DefaultWzlCatalog = new()
    {
        // WZL15
        new WzlCatalog { Model = "WZL15.025.02", Function = "S:標準款",   Stroke = "≤1000", InnerHeight = 15, InnerWidth = 25, OuterHeight = 24,   OuterWidth = 40,  MinRadius = 50,  RecRadius = 70,  ReservedK = 42, BendLength = 304, MountingH1 = "130~150", InterferenceH2 = "150~200", InnerArea = 312,  AppPipes = "4×Φ4~Φ6" },
        new WzlCatalog { Model = "WZL15.025.02", Function = "ES:防靜電",  Stroke = "≤1000", InnerHeight = 15, InnerWidth = 25, OuterHeight = 24,   OuterWidth = 40,  MinRadius = 50,  RecRadius = 70,  ReservedK = 42, BendLength = 304, MountingH1 = "130~150", InterferenceH2 = "150~200", InnerArea = null, AppPipes = "4×Φ4~Φ6" },
        new WzlCatalog { Model = "WZL15.025.02", Function = "M:輕型龍骨", Stroke = "≤1600", InnerHeight = 15, InnerWidth = 25, OuterHeight = 24,   OuterWidth = 40,  MinRadius = 50,  RecRadius = 70,  ReservedK = 42, BendLength = 304, MountingH1 = "130/170", InterferenceH2 = "150/190", InnerArea = null, AppPipes = "4×Φ4" },
        // WZL18
        new WzlCatalog { Model = "WZL18.032.02", Function = "S:標準款",   Stroke = "≤1200", InnerHeight = 18, InnerWidth = 32, OuterHeight = 26,   OuterWidth = 50,  MinRadius = 50,  RecRadius = 75,  ReservedK = 40, BendLength = 300, MountingH1 = "140~160", InterferenceH2 = "160~210", InnerArea = 490,  AppPipes = "6×Φ4~Φ6" },
        new WzlCatalog { Model = "WZL18.032.02", Function = "ES:防靜電",  Stroke = "≤1200", InnerHeight = 18, InnerWidth = 32, OuterHeight = 26,   OuterWidth = 50,  MinRadius = 50,  RecRadius = 70,  ReservedK = 40, BendLength = 300, MountingH1 = "140~160", InterferenceH2 = "160~210", InnerArea = null, AppPipes = "6×Φ4~Φ6" },
        // WZL22
        new WzlCatalog { Model = "WZL22.040.02", Function = "S:標準款",   Stroke = "≤1600", InnerHeight = 22, InnerWidth = 40, OuterHeight = 35,   OuterWidth = 65,  MinRadius = 60,  RecRadius = 80,  ReservedK = 49, BendLength = 318, MountingH1 = "140~170", InterferenceH2 = "160~220", InnerArea = 710,  AppPipes = "6×Φ7~Φ8 +4×Φ4~Φ6" },
        new WzlCatalog { Model = "WZL22.040.02", Function = "ES:防靜電",  Stroke = "≤1600", InnerHeight = 22, InnerWidth = 40, OuterHeight = 35,   OuterWidth = 65,  MinRadius = 60,  RecRadius = 70,  ReservedK = 49, BendLength = 318, MountingH1 = "140~170", InterferenceH2 = "160~220", InnerArea = null, AppPipes = "6×Φ7~Φ8 +4×Φ4~Φ6" },
        new WzlCatalog { Model = "WZL22.040.02", Function = "M:輕型龍骨", Stroke = "≤2000", InnerHeight = 22, InnerWidth = 40, OuterHeight = 35,   OuterWidth = 65,  MinRadius = 50,  RecRadius = 70,  ReservedK = 49, BendLength = 318, MountingH1 = "140/180", InterferenceH2 = "160/200", InnerArea = null, AppPipes = "2×Φ6~Φ7 +2×Φ4~Φ5" },
        // WZL28
        new WzlCatalog { Model = "WZL28.065.02", Function = "S:標準款",   Stroke = "≤1700", InnerHeight = 28, InnerWidth = 65, OuterHeight = 41,   OuterWidth = 90,  MinRadius = 60,  RecRadius = 85,  ReservedK = 49, BendLength = 318, MountingH1 = "150~180", InterferenceH2 = "180~230", InnerArea = 1515, AppPipes = "6×Φ8~Φ10 +6×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL28.065.02", Function = "ES:防靜電",  Stroke = "≤1700", InnerHeight = 28, InnerWidth = 65, OuterHeight = 41,   OuterWidth = 90,  MinRadius = 60,  RecRadius = 70,  ReservedK = 49, BendLength = 318, MountingH1 = "150~180", InterferenceH2 = "180~230", InnerArea = null, AppPipes = "6×Φ8~Φ10 +6×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL28.065.02", Function = "M:輕型龍骨", Stroke = "≤2000", InnerHeight = 28, InnerWidth = 65, OuterHeight = 41,   OuterWidth = 90,  MinRadius = 50,  RecRadius = 70,  ReservedK = 49, BendLength = 318, MountingH1 = "150/190", InterferenceH2 = "170/210", InnerArea = null, AppPipes = "4×Φ4~Φ5" },
        new WzlCatalog { Model = "WZL28.065.02", Function = "L:中型龍骨", Stroke = "≤2600", InnerHeight = 28, InnerWidth = 65, OuterHeight = 41,   OuterWidth = 90,  MinRadius = 70,  RecRadius = 100, ReservedK = 49, BendLength = 318, MountingH1 = "200/260", InterferenceH2 = "220/280", InnerArea = null, AppPipes = "2×Φ4~Φ5" },
        // WZL35
        new WzlCatalog { Model = "WZL35.065.02", Function = "S:標準款",   Stroke = "≤1700", InnerHeight = 35, InnerWidth = 65, OuterHeight = 48,   OuterWidth = 90,  MinRadius = 70,  RecRadius = 90,  ReservedK = 56, BendLength = 394, MountingH1 = "160~190", InterferenceH2 = "200~250", InnerArea = 2050, AppPipes = "8×Φ8~Φ10 +6×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL35.065.02", Function = "ES:防靜電",  Stroke = "≤1700", InnerHeight = 35, InnerWidth = 65, OuterHeight = 48,   OuterWidth = 90,  MinRadius = 70,  RecRadius = 90,  ReservedK = 56, BendLength = 394, MountingH1 = "160~190", InterferenceH2 = "200~250", InnerArea = null, AppPipes = "8×Φ8~Φ10 +6×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL35.065.02", Function = "M:輕型龍骨", Stroke = "≤2000", InnerHeight = 35, InnerWidth = 65, OuterHeight = 48,   OuterWidth = 90,  MinRadius = 50,  RecRadius = 70,  ReservedK = 56, BendLength = 394, MountingH1 = "160/190", InterferenceH2 = "180/210", InnerArea = null, AppPipes = "6×Φ8~Φ9 +4×Φ4~Φ6" },
        new WzlCatalog { Model = "WZL35.065.02", Function = "L:中型龍骨", Stroke = "≤2600", InnerHeight = 35, InnerWidth = 65, OuterHeight = 48,   OuterWidth = 90,  MinRadius = 70,  RecRadius = 100, ReservedK = 56, BendLength = 394, MountingH1 = "210/270", InterferenceH2 = "230/290", InnerArea = null, AppPipes = "8×Φ8~Φ10 +2×Φ6~Φ7" },
        // WZL40.085
        new WzlCatalog { Model = "WZL40.085.02", Function = "S:標準款",   Stroke = "≤1800", InnerHeight = 40, InnerWidth = 85, OuterHeight = 53,   OuterWidth = 115, MinRadius = 100, RecRadius = 130, ReservedK = 70, BendLength = 548, MountingH1 = "250~300", InterferenceH2 = "300~350", InnerArea = 3056, AppPipes = "4×Φ10~Φ12 +10×Φ8~Φ9 +8×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL40.085.02", Function = "ES:防靜電",  Stroke = "≤1800", InnerHeight = 40, InnerWidth = 85, OuterHeight = 53,   OuterWidth = 115, MinRadius = 100, RecRadius = 130, ReservedK = 70, BendLength = 548, MountingH1 = "250~300", InterferenceH2 = "300~350", InnerArea = null, AppPipes = "4×Φ10~Φ12 +10×Φ8~Φ9 +8×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL40.085.02", Function = "G:重型龍骨", Stroke = "≤3500", InnerHeight = 40, InnerWidth = 85, OuterHeight = 53,   OuterWidth = 115, MinRadius = 130, RecRadius = 130, ReservedK = 70, BendLength = 548, MountingH1 = "350",     InterferenceH2 = "380",     InnerArea = null, AppPipes = "2×Φ10~Φ12 +6×Φ8~Φ9 +4×Φ6~Φ7" },
        // WZL40.110
        new WzlCatalog { Model = "WZL40.110.02", Function = "S:標準款",   Stroke = "≤1800", InnerHeight = 40, InnerWidth = 110, OuterHeight = 53.2m, OuterWidth = 140, MinRadius = 100, RecRadius = 130, ReservedK = 70, BendLength = 548, MountingH1 = "280~300", InterferenceH2 = "330~380", InnerArea = 4000, AppPipes = "4×Φ10~Φ12 +12×Φ8~Φ9 +12×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL40.110.02", Function = "ES:防靜電",  Stroke = "≤1800", InnerHeight = 40, InnerWidth = 110, OuterHeight = 53.2m, OuterWidth = 140, MinRadius = 100, RecRadius = 130, ReservedK = 70, BendLength = 548, MountingH1 = "280~300", InterferenceH2 = "330~380", InnerArea = null, AppPipes = "4×Φ10~Φ12 +12×Φ8~Φ9 +12×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL40.110.02", Function = "G:重型龍骨", Stroke = "≤3500", InnerHeight = 40, InnerWidth = 110, OuterHeight = 53.2m, OuterWidth = 140, MinRadius = 130, RecRadius = 130, ReservedK = 70, BendLength = 548, MountingH1 = "350",     InterferenceH2 = "380",     InnerArea = null, AppPipes = "2×Φ10~Φ12 +8×Φ8~Φ9 +6×Φ6~Φ7" },
    };

    private static readonly List<WzlCatalog> WzlSelectionModels = new()
    {
        new WzlCatalog { Model = "WZL15.025.02", Function = "S:標準款", InnerHeight = 15, InnerWidth = 25, RecRadius = 70,  BendLength = 304, InnerArea = 312,  AppPipes = "4×Φ4~Φ6" },
        new WzlCatalog { Model = "WZL18.032.02", Function = "S:標準款", InnerHeight = 18, InnerWidth = 32, RecRadius = 75,  BendLength = 300, InnerArea = 490,  AppPipes = "6×Φ4~Φ6" },
        new WzlCatalog { Model = "WZL22.040.02", Function = "S:標準款", InnerHeight = 22, InnerWidth = 40, RecRadius = 80,  BendLength = 318, InnerArea = 710,  AppPipes = "6×Φ7~Φ8 +4×Φ4~Φ6" },
        new WzlCatalog { Model = "WZL28.065.02", Function = "S:標準款", InnerHeight = 28, InnerWidth = 65, RecRadius = 85,  BendLength = 318, InnerArea = 1515, AppPipes = "6×Φ8~Φ10 +6×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL35.065.02", Function = "S:標準款", InnerHeight = 35, InnerWidth = 65, RecRadius = 90,  BendLength = 394, InnerArea = 2050, AppPipes = "8×Φ8~Φ10 +6×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL40.085.02", Function = "S:標準款", InnerHeight = 40, InnerWidth = 85, RecRadius = 130, BendLength = 548, InnerArea = 3056, AppPipes = "4×Φ10~Φ12 +10×Φ8~Φ9 +8×Φ6~Φ7" },
        new WzlCatalog { Model = "WZL40.110.02", Function = "S:標準款", InnerHeight = 40, InnerWidth = 110, RecRadius = 130, BendLength = 548, InnerArea = 4000, AppPipes = "4×Φ10~Φ12 +12×Φ8~Φ9 +12×Φ6~Φ7" },
    };

    private static readonly Dictionary<string, (decimal maxW, decimal spanBase, decimal spanSlope)> WzlSpanData = new()
    {
        ["WZL15.025.02"] = (0.4m,   550m,   750m),
        ["WZL18.032.02"] = (0.6m,   650m,   333m),
        ["WZL22.040.02"] = (0.7m,   850m,   571m),
        ["WZL28.065.02"] = (0.8m,   950m,   500m),
        ["WZL35.065.02"] = (1.0m,   950m,   400m),
        ["WZL40.085.02"] = (1.3m,   1050m,  384m),
        ["WZL40.110.02"] = (1.5m,   1050m,  333m),
    };

    private static readonly List<MeCatalog> DefaultMeCatalog = new()
    {
        new MeCatalog { BaseModel = "ME15.20.R",  InnerHeight = 15, InnerWidth = 20,  R1 = 43,  R2 = 53,  R3 = 63,  R1Suffix = "28", R2Suffix = "38", R3Suffix = "48", Lp1 = 130, Lp2 = 160, Lp3 = 280, InnerArea = 300,  MaxWeight = 1.25m, SpanBase = 1062m, SpanSlope = 350m },
        new MeCatalog { BaseModel = "ME15.30.R",  InnerHeight = 15, InnerWidth = 30,  R1 = 43,  R2 = 53,  R3 = 63,  R1Suffix = "28", R2Suffix = "38", R3Suffix = "48", Lp1 = 130, Lp2 = 160, Lp3 = 280, InnerArea = 450,  MaxWeight = 1.25m, SpanBase = 1062m, SpanSlope = 350m },
        new MeCatalog { BaseModel = "ME15.40.R",  InnerHeight = 15, InnerWidth = 40,  R1 = 43,  R2 = 53,  R3 = 63,  R1Suffix = "28", R2Suffix = "38", R3Suffix = "48", Lp1 = 130, Lp2 = 160, Lp3 = 280, InnerArea = 600,  MaxWeight = 1.25m, SpanBase = 1062m, SpanSlope = 350m },
        new MeCatalog { BaseModel = "ME20.25.R",  InnerHeight = 20, InnerWidth = 25,  R1 = 58,  R2 = 68,  R3 = 95,  R1Suffix = "38", R2Suffix = "48", R3Suffix = "75", Lp1 = 180, Lp2 = 215, Lp3 = 300, InnerArea = 500,  MaxWeight = 1.5m,  SpanBase = 1125m, SpanSlope = 389m },
        new MeCatalog { BaseModel = "ME20.40.R",  InnerHeight = 20, InnerWidth = 40,  R1 = 58,  R2 = 68,  R3 = 95,  R1Suffix = "38", R2Suffix = "48", R3Suffix = "75", Lp1 = 180, Lp2 = 215, Lp3 = 300, InnerArea = 800,  MaxWeight = 1.5m,  SpanBase = 1125m, SpanSlope = 389m },
        new MeCatalog { BaseModel = "ME20.50.R",  InnerHeight = 20, InnerWidth = 50,  R1 = 58,  R2 = 68,  R3 = 95,  R1Suffix = "38", R2Suffix = "48", R3Suffix = "75", Lp1 = 180, Lp2 = 215, Lp3 = 300, InnerArea = 1000, MaxWeight = 1.5m,  SpanBase = 1125m, SpanSlope = 389m },
        new MeCatalog { BaseModel = "ME20.60.R",  InnerHeight = 20, InnerWidth = 60,  R1 = 58,  R2 = 68,  R3 = 95,  R1Suffix = "38", R2Suffix = "48", R3Suffix = "75", Lp1 = 180, Lp2 = 215, Lp3 = 300, InnerArea = 1200, MaxWeight = 1.5m,  SpanBase = 1125m, SpanSlope = 389m },
        new MeCatalog { BaseModel = "ME20.70.R",  InnerHeight = 20, InnerWidth = 70,  R1 = 58,  R2 = 68,  R3 = 95,  R1Suffix = "38", R2Suffix = "48", R3Suffix = "75", Lp1 = 180, Lp2 = 215, Lp3 = 300, InnerArea = 1400, MaxWeight = 1.5m,  SpanBase = 1125m, SpanSlope = 389m },
        new MeCatalog { BaseModel = "ME25.40.R",  InnerHeight = 25, InnerWidth = 40,  R1 = 80,  R2 = 100, R3 = 125, R1Suffix = "55", R2Suffix = "75", R3Suffix = "100", Lp1 = 265, Lp2 = 330, Lp3 = 630, InnerArea = 1000, MaxWeight = 5.2m,  SpanBase = 2200m, SpanSlope = 215m },
        new MeCatalog { BaseModel = "ME25.55.R",  InnerHeight = 25, InnerWidth = 55,  R1 = 80,  R2 = 100, R3 = 125, R1Suffix = "55", R2Suffix = "75", R3Suffix = "100", Lp1 = 265, Lp2 = 330, Lp3 = 630, InnerArea = 1375, MaxWeight = 5.2m,  SpanBase = 2200m, SpanSlope = 215m },
        new MeCatalog { BaseModel = "ME25.75.R",  InnerHeight = 25, InnerWidth = 75,  R1 = 80,  R2 = 100, R3 = 125, R1Suffix = "55", R2Suffix = "75", R3Suffix = "100", Lp1 = 265, Lp2 = 330, Lp3 = 630, InnerArea = 1875, MaxWeight = 5.2m,  SpanBase = 2200m, SpanSlope = 215m },
        new MeCatalog { BaseModel = "ME25.90.R",  InnerHeight = 25, InnerWidth = 90,  R1 = 80,  R2 = 100, R3 = 125, R1Suffix = "55", R2Suffix = "75", R3Suffix = "100", Lp1 = 265, Lp2 = 330, Lp3 = 630, InnerArea = 2250, MaxWeight = 5.2m,  SpanBase = 2200m, SpanSlope = 215m },
        new MeCatalog { BaseModel = "ME35.50.R",  InnerHeight = 35, InnerWidth = 50,  R1 = 100, R2 = 110, R3 = 135, R1Suffix = "65", R2Suffix = "75", R3Suffix = "100", Lp1 = 315, Lp2 = 350, Lp3 = 605, InnerArea = 1750, MaxWeight = 8.0m,  SpanBase = 2500m, SpanSlope = 140m },
        new MeCatalog { BaseModel = "ME35.75.R",  InnerHeight = 35, InnerWidth = 75,  R1 = 100, R2 = 110, R3 = 135, R1Suffix = "65", R2Suffix = "75", R3Suffix = "100", Lp1 = 315, Lp2 = 350, Lp3 = 605, InnerArea = 2625, MaxWeight = 8.0m,  SpanBase = 2500m, SpanSlope = 140m },
        new MeCatalog { BaseModel = "ME35.100.R", InnerHeight = 35, InnerWidth = 100, R1 = 100, R2 = 110, R3 = 135, R1Suffix = "65", R2Suffix = "75", R3Suffix = "100", Lp1 = 315, Lp2 = 350, Lp3 = 605, InnerArea = 3500, MaxWeight = 8.0m,  SpanBase = 2500m, SpanSlope = 140m },
        new MeCatalog { BaseModel = "ME35.125.R", InnerHeight = 35, InnerWidth = 125, R1 = 100, R2 = 110, R3 = 135, R1Suffix = "65", R2Suffix = "75", R3Suffix = "100", Lp1 = 315, Lp2 = 350, Lp3 = 605, InnerArea = 4375, MaxWeight = 8.0m,  SpanBase = 2500m, SpanSlope = 140m },
    };

    private static readonly List<TrunkingCatalog> DefaultTrunkingCatalog = new()
    {
        new TrunkingCatalog { Model = "TK-25×25",  Width = 25,  Height = 25,  CrossSection = 625, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Model = "TK-40×25",  Width = 40,  Height = 25,  CrossSection = 1000, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Model = "TK-40×40",  Width = 40,  Height = 40,  CrossSection = 1600, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Model = "TK-60×40",  Width = 60,  Height = 40,  CrossSection = 2400, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Model = "TK-60×60",  Width = 60,  Height = 60,  CrossSection = 3600, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Model = "TK-80×40",  Width = 80,  Height = 40,  CrossSection = 3200, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Model = "TK-80×60",  Width = 80,  Height = 60,  CrossSection = 4800, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Model = "TK-100×60", Width = 100, Height = 60,  CrossSection = 6000, FillRatioLimit = 0.60m },
        new TrunkingCatalog { Model = "TK-100×100", Width = 100, Height = 100, CrossSection = 10000, FillRatioLimit = 0.60m },
    };

    public static (List<PipeType> pipeTypes, List<WzlCatalog> wzlCatalog, List<MeCatalog> meCatalog,
                   Dictionary<string, (decimal maxW, decimal spanBase, decimal spanSlope)> wzlSpanData)
        GetDefaults() => (DefaultPipeTypes, WzlSelectionModels, DefaultMeCatalog, WzlSpanData);

    public static List<TrunkingCatalog> GetTrunkingDefaults() => DefaultTrunkingCatalog;

    public static async Task SeedAsync(DragChainDbContext context)
    {
        await RemoveWaterPipeTypesAsync(context);

        if (!await context.PipeTypes.AnyAsync())
        {
            foreach (var p in DefaultPipeTypes)
                context.PipeTypes.Add(p);
        }
        else
        {
            await NormalizePipeTypeCategoriesAsync(context);
        }

        if (!await context.WzlCatalog.AnyAsync())
        {
            int wzlId = 1;
            foreach (var w in DefaultWzlCatalog)
            {
                w.Id = wzlId++;
                context.WzlCatalog.Add(w);
            }
        }

        if (!await context.MeCatalog.AnyAsync())
        {
            int meId = 1;
            foreach (var m in DefaultMeCatalog)
            {
                m.Id = meId++;
                context.MeCatalog.Add(m);
            }
        }

        if (!await context.TrunkingCatalog.AnyAsync())
        {
            int tkId = 1;
            foreach (var t in DefaultTrunkingCatalog)
            {
                t.Id = tkId++;
                context.TrunkingCatalog.Add(t);
            }
        }
        else
        {
            await AddMissingDefaultTrunkingAsync(context);
        }

        await context.SaveChangesAsync();
    }

    private static async Task RemoveWaterPipeTypesAsync(DragChainDbContext context)
    {
        var waterPipes = await context.PipeTypes
            .Where(p => p.Type == "water" || p.Name.Contains("水管"))
            .ToListAsync();

        context.PipeTypes.RemoveRange(waterPipes);

        if (waterPipes.Count > 0)
            await context.SaveChangesAsync();
    }

    private static async Task NormalizePipeTypeCategoriesAsync(DragChainDbContext context)
    {
        var pipes = await context.PipeTypes.ToListAsync();
        var changed = false;

        foreach (var pipe in pipes)
        {
            var normalized = pipe.Type == "cable" ? InferCableCategory(pipe.Name) : PipeTypeCategory.Normalize(pipe.Type);
            if (pipe.Type == normalized) continue;

            pipe.Type = normalized;
            changed = true;
        }

        if (changed)
            await context.SaveChangesAsync();
    }

    private static string InferCableCategory(string name)
    {
        return name.Contains("電源") || name.Contains("电源")
            ? PipeTypeCategory.StrongCable
            : PipeTypeCategory.WeakCable;
    }

    private static async Task AddMissingDefaultTrunkingAsync(DragChainDbContext context)
    {
        var existingModels = await context.TrunkingCatalog
            .Select(t => t.Model)
            .ToListAsync();
        var existingModelSet = existingModels.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nextId = (await context.TrunkingCatalog.MaxAsync(t => (int?)t.Id) ?? 0) + 1;

        foreach (var t in DefaultTrunkingCatalog)
        {
            if (existingModelSet.Contains(t.Model)) continue;

            context.TrunkingCatalog.Add(new TrunkingCatalog
            {
                Id = nextId++,
                Model = t.Model,
                Width = t.Width,
                Height = t.Height,
                CrossSection = t.CrossSection,
                FillRatioLimit = t.FillRatioLimit
            });
        }
    }

    public static async Task ResetAsync(DragChainDbContext context)
    {
        context.PipeTypes.RemoveRange(context.PipeTypes);
        context.WzlCatalog.RemoveRange(context.WzlCatalog);
        context.MeCatalog.RemoveRange(context.MeCatalog);
        context.TrunkingCatalog.RemoveRange(context.TrunkingCatalog);
        await context.SaveChangesAsync();
        await SeedAsync(context);
    }
}
