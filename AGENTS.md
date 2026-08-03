# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## 项目概述

工业自动化场景下的**线槽与拖链选型计算工具**，同时集成**感应器选型**模块。用户输入管线清单（电缆、气管等）及运动参数，系统自动匹配适合的线槽型号（TK 系列）或拖链型号（WZL/ME 品牌），并给出带链长计算的完整选型结果。

## 技术栈

- **后端**：ASP.NET Core 8 + Entity Framework Core + SQLite（`DragChain.API/`）
- **前端**：Vue 3 + TypeScript + Element Plus + Pinia（`DragChain.Client/`）
- **测试**：`DragChain.API.Tests/Program.cs`（可执行程序，非 xUnit，用断言函数验证业务逻辑）

## 常用命令

### 后端
```bash
# 从解决方案根目录运行 API（默认端口 5256）
dotnet run --project DragChain.API

# 数据库迁移
dotnet ef migrations add <MigrationName> --project DragChain.API
dotnet ef database update --project DragChain.API

# 构建
dotnet build
dotnet build -c Release
```

### 前端
```bash
cd DragChain.Client
npm install
npm run dev          # 开发服务器，端口 5173，自动代理 /api -> localhost:5256
npm run build        # vue-tsc 类型检查 + vite 构建，产物输出至 wwwroot（供后端托管）
```

### 测试（逻辑测试）
```bash
# 后端集成测试（TrunkingCalculationService）
dotnet run --project DragChain.API.Tests

# 前端工具函数逻辑测试（vue-tsc 编译 + node 运行 scripts/run-logic-tests.mjs）
cd DragChain.Client
npm run test:logic
```

新增工具函数时，在 `src/utils/` 下同步创建 `<name>.logic-test.ts` 测试文件。

### 发布（IIS 单站点）
前端构建产物由 `vite build` 输出到 `DragChain.API/wwwroot/`，后端通过 `UseStaticFiles` 托管，无需独立前端服务。发布配置在 `publish/iis-dragchain/`。

## 架构与数据流

### 整体结构
```
用户输入管线清单
    ↓
前端 Vue（ChainCalcView / TrunkingCalcView / SelectorView）
    ↓  POST /api/Calculation/calc 或 /api/Trunking/calc 或 /api/selection-tree
后端 CalculationService / TrunkingCalculationService / Sensor 模块
    ↓  查询 SQLite（EF Core）— 两个独立 DbContext
型录数据（WzlCatalog / MeCatalog / TrunkingCatalog / SensorDbContext）
    ↓
返回匹配结果 + 计算步骤 DTO
```

### 后端分层

| 层 | 路径 | 说明 |
|---|---|---|
| Controller | `Controllers/` | 薄层，仅做参数接收与 HTTP 响应 |
| Service | `Services/CalculationService.cs` | 拖链选型核心计算（WZL/ME 分支） |
| Service | `Services/TrunkingCalculationService.cs` | 线槽选型核心计算（弱电/强电分区，槽位模式） |
| Seeder | `Services/CatalogSeeder.cs` | 应用启动时注入默认型录数据，包含 WZL 架空能力系数 |
| DbContext | `Data/DragChainDbContext.cs` | 9 个 DbSet：PipeTypes、WzlCatalog、MeCatalog、TrunkingCatalog、PipeModules、PipeModuleItems、PipeComponents、PipeComponentItems、AppSettings |
| Sensor 模块 | `Sensor/` | 感应器选型子系统，独立 DbContext，见下文 |
| Migrations | `Data/Migrations/` | EF Core 迁移历史 |

### Sensor 模块（`DragChain.API/Sensor/`）

独立于线槽/拖链的感应器选型子系统，使用**独立的 `SensorDbContext`**（`sensor.db`）。

| 子目录 | 说明 |
|---|---|
| `Controllers/` | AuthController、ProductsController、ScenariosController、SelectionEntriesController、SelectionTreeController 等 |
| `Data/SensorDbContext.cs` | 约 20 个 DbSet：产品、工况场景、选型规则、选型树节点、RBAC 用户/角色权限等 |
| `Data/*Migrator.cs` | 数据迁移辅助类（产品、工艺注释、选型条目、RBAC） |
| `Models/` | Product、Scenario、ScenarioFunction、FunctionCondition、SelectionRule、RbacUser、RbacRolePermission 等 |
| `Security/RbacMiddleware.cs` | 签名 Bearer Token 认证 + RBAC 权限中间件，拦截所有 `/api/*` 请求 |
| `Security/RbacPermissionCatalog.cs` | 权限码硬编码目录（menu / page / api 三类），`MatchApiPermission` 按路径前缀匹配所需权限 |
| `Services/` | ProductWorkbookService（Excel 导入/导出）、RbacUserWorkbookService |

**RBAC 角色体系**：
- `super_admin` / `admin`：全部权限
- `editor`：除用户管理（RBAC 写权限）外的所有权限
- 默认角色：仅读取感应器选型、产品、分类、线槽/拖链/管线库

**认证流程**：前端 `src/utils/auth.ts` 管理 `localStorage` 中的签名 Bearer Token（key: `selection-software-token`），`authVersion` ref 响应式触发 Vue 重新计算。`AuthController` 内存维护 session 映射（`TryGetSession`）；每次请求 `RbacMiddleware` 重新从 DB 加载用户状态和权限，确保实时生效。

**无需 Bearer Token 的端点**：`OPTIONS /api/*`、`GET /api/health`、`POST /login`（兼容 `POST /api/auth/login`）和 `POST /refresh-token`（兼容 `POST /api/auth/refresh-token`）。登录与刷新端点仍分别要求有效凭据和 Refresh Token。

**生产安全配置**：生产环境必须通过安全环境变量注入至少 32 字节高熵的 `DRAGCHAIN_AUTH_SIGNING_KEY`。新建空 `sensor.db` 首次启动必须显式设置至少 12 个字符且包含字母、数字和符号的 `DRAGCHAIN_BOOTSTRAP_ADMIN_PASSWORD`，可选用 `DRAGCHAIN_BOOTSTRAP_ADMIN_EMPLOYEE_NO` 指定工号。升级含历史固定账号的旧库时，若不存在已启用的非旧版超级管理员，也必须设置该强密码以完成一次性轮换；否则应用拒绝启动。被禁用的旧账号同时销毁原凭据。不得把这些秘密写入仓库、发布包或日志。

### 计算逻辑关键点（拖链）

1. **内高校验**：`minHeight = maxDia × 1.25`，需 ≤ 型号 `InnerHeight`
2. **弯曲半径校验**：管线类型决定 `BendMultiplier`（气管/弱电 ×8，编码器 ×13），取最大值
3. **截面积校验**：`minArea = totalArea / ratio`（WZL 占空比 60%，ME 55%）
4. **架空能力**：线性公式 `calcSpan = spanBase - spanSlope × weight`，升降运动直接跳过
5. **链长公式**：`Lk = ceil((Stroke/2 + LmOffset + Lp) / 10) × 10`
6. **选型结论**：`ok`（找到最终型号）/ `warn`（升降模式仅初步型号）/ `err`（无合适型号）

### 计算逻辑关键点（线槽）

- 管线按 `PipeTypeCategory` 自动分入弱电侧（左）或强电侧（右）
- 槽位模式支持多槽位上下层管线，相邻槽位边界段自动合并（`trunkingSegmentLayers.ts`）
- 每个截面段独立选型，按 `FillRatioLimit`（型号自身上限，非请求参数）过滤

### 前端结构

| 模块 | 路径 | 功能 |
|---|---|---|
| 路由 | `src/router/index.ts` | 功能组（`meta.group`）：trunking / chain / pipe / sensor / system；受权限守卫保护 |
| API 层 | `src/api/` | `client.ts` 封装 fetch（自动附带 Bearer Token），各业务文件对应后端 controller |
| Sensor API | `src/api/sensor/index.ts` | 感应器选型相关接口（产品、场景、选型树等） |
| 用户 API | `src/api/user.ts` | 登录/RBAC 相关接口 |
| Auth 工具 | `src/utils/auth.ts` | Token 读写、`isLoggedIn()`、`hasPerms()` 权限判断 |
| Composables | `src/composables/` | `usePipeLibrary`、`usePipeModules`、`usePipeComponents` |
| 工具函数 | `src/utils/` | 纯函数，业务逻辑均有对应 `.logic-test.ts` 测试文件 |
| 状态 | `src/stores/app.ts` | Pinia store，全局应用状态 |
| 运行时状态 | `src/stores/trunkingRuntimeState.ts` | 模块级变量（非 Pinia），线槽槽位布局状态，通过 getter/setter 访问 |
| 视图 | `src/views/sensor/` | SelectorView（感应器选型）、ProductsView（产品管理） |
| 视图 | `src/views/rbac/` | UsersView（用户管理） |
| 视图 | `src/views/LoginView.vue` | 登录页，工号 + 密码，登录后 redirect 回目标路由 |

### 型录数据管理

- 默认数据硬编码在 `CatalogSeeder.cs`，启动时写入（幂等）
- 所有型录支持通过前端 UI（CRUD 表格）手动修改，修改后存 SQLite
- 重置接口（`/reset`）可恢复默认数据

### AppSettings 表

`AppSettings` 表以 key/value 形式存储应用级参数（如默认有效利用率），通过 `/api/AppSettings` 接口读写。

## 数据库

项目使用**两个独立 SQLite 文件**，均在 API 工作目录下生成，不纳入 git 跟踪（`.csproj` 已排除 `*.db`）：

| 文件 | DbContext | 用途 |
|---|---|---|
| `dragchain.db` | `DragChainDbContext` | 线槽/拖链/管线库型录及选型数据 |
| `sensor.db` | `SensorDbContext` | 感应器产品、选型规则、RBAC 用户权限 |

两个 DbContext 均在启动时自动执行迁移（`context.Database.Migrate()`）。

## CORS 配置

开发时前端 Vite（5173）代理到后端（5256），无跨域问题。生产部署为 IIS 单站点，前端产物由后端静态文件中间件托管，无需 CORS。
