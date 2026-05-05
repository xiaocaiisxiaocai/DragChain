# 拖链选型工具

拖链选型计算工具前后端分离架构版本。

## 项目结构

```
线槽拖链选型/
├── DragChain.API/              # ASP.NET Core Web API (.NET 8)
│   ├── Controllers/           # API 控制器
│   ├── Models/                # 数据模型 + DTO
│   ├── Services/             # 计算服务 + 种子数据
│   ├── Data/                 # EF Core DbContext
│   └── Program.cs
├── DragChain.Client/          # React + Vite + TypeScript
│   └── src/
│       ├── api/              # API 客户端
│       ├── components/       # React 组件
│       ├── hooks/            # 自定义 Hooks
│       └── types/            # TypeScript 类型
└── 拖链选型工具_fixed.html   # 原版 HTML 工具（保留）
```

## 快速启动

### 1. 启动后端 API

```powershell
cd DragChain.API
dotnet run --urls="http://localhost:5001"
```

后端启动后会：
- 创建 SQLite 数据库 `dragchain.db`
- 自动填充种子数据（17种管线类型、WZL/ME 型录）

Swagger 文档地址：http://localhost:5001/swagger

### 2. 启动前端

```powershell
cd DragChain.Client
npm install      # 首次运行需要安装依赖
npm run dev
```

前端地址：http://localhost:5173

## API 端点

| 方法 | 端点 | 说明 |
|------|------|------|
| POST | /api/calculation/calc | 选型计算 |
| GET | /api/pipelibrary | 获取所有管线类型 |
| POST | /api/pipelibrary | 新增管线类型 |
| PUT | /api/pipelibrary/{id} | 更新管线类型 |
| DELETE | /api/pipelibrary/{id} | 删除管线类型 |
| POST | /api/pipelibrary/reset | 恢复默认管线库 |
| GET | /api/wzl | 获取所有 WZL 型录 |
| POST | /api/wzl/reset | 恢复默认 WZL 型录 |
| GET | /api/me | 获取所有 ME 型录 |
| POST | /api/me/reset | 恢复默认 ME 型录 |

## 计算请求示例

```json
POST /api/calculation/calc
{
  "brand": "wzl",
  "sensorCount": 15,
  "magnetCount": 0,
  "motionType": "横移",
  "stroke": 1000,
  "lmOffset": 50,
  "pipes": [
    { "pipeTypeId": 1, "qty": 1 },
    { "pipeTypeId": 7, "qty": 7 }
  ]
}
```

## 技术栈

- **后端**: ASP.NET Core 8 + Entity Framework Core 8 + SQLite
- **前端**: React 18 + TypeScript + Vite
- **样式**: 保持原 HTML 工具的完整 CSS 设计
