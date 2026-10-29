# 虚拟现场巡检问题标注（Unity + Go + React）

巡检人员在 Unity 虚拟现场中点击设备/墙面放置问题标记并上报，管理人员在 React 网页中查看问题并更新处理状态。三端共用同一个 Go 后端和同一份数据。

## 技术栈与版本

| 部分         | 技术                                                | 版本           |
| ------------ | --------------------------------------------------- | -------------- |
| Unity 客户端 | 团结引擎 Tuanjie（基于 Unity 2022.3.62t17 LTS）、C# | 1.11.0         |
| 管理端       | React + TypeScript + Vite                           | 待补（开发中） |
| 后端         | Go（标准库 `net/http`，无第三方依赖）             | 1.24.0         |
| 持久化       | 本地 JSON 文件（`server/issues.json`）            | —             |

## 目录结构

```
├─ server/        Go 后端
├─ unity/         Unity 客户端（Assets/Packages/ProjectSettings）
├─ admin/         React 管理端
└─ docs/          AI_LOG.md 等文档
```

## 启动方式

### 1. Go 后端（端口 8080）

```bash
cd server
go run .
```

看到 `Server is running on port :8080` 即启动成功。数据文件 `issues.json` 在首次写入时自动生成，删除该文件即可清空数据。

### 2. Unity 客户端（Editor 运行）

1. 用团结引擎打开 `unity/` 目录
2. 打开场景 `Assets/Scenes/SampleScene`
3. 确认 Canvas 上 `ApiClient` 组件的 `baseUrl` 为 `http://127.0.0.1:8080`（默认值即此）
4. 点 ▶ 运行：WASD 移动、鼠标转视角，点击场景表面 → 放置标记并弹出表单

### 3. React 管理端

开发中，补齐后在此补充启动命令（计划为 `cd admin && npm install && npm run dev`）。

## 接口设计

所有请求/响应均为 JSON；错误响应统一为 `{"error": "...", "message": "..."}`，`message` 为可直接展示给用户的中文提示。后端已启用 CORS（`Allow-Origin: *`），React 跨端口访问无需代理。

| 方法  | 路径                 | 说明                                    | 成功响应                    |
| ----- | -------------------- | --------------------------------------- | --------------------------- |
| GET   | `/api/health`      | 服务运行状态检查                        | `200` `{"status":"ok"}` |
| GET   | `/api/issues`      | 问题列表（按创建时间倒序）              | `200` `{"items":[...]}` |
| POST  | `/api/issues`      | 上报问题（Unity 使用）                  | `201` 完整 Issue 对象     |
| PATCH | `/api/issues/{id}` | 修改处理状态，body `{"status":"..."}` | `200` 更新后的 Issue      |

### 数据结构（三端字段完全一致）

```json
{
  "id": "ISS-000001",            // 后端生成，ISS- 自增
  "title": "入口墙面破损",
  "description": "左侧墙体存在裂缝",
  "priority": "high",            // low | medium | high
  "status": "open",              // open | in_progress | resolved，新建默认 open
  "position": { "x": 1.25, "y": 0.8, "z": 2.4 },
  "createdAt": "2026-10-29T01:12:32+08:00",   // RFC3339，后端生成
  "updatedAt": "2026-10-29T01:13:06+08:00"
}
```

### 参数校验（HTTP 400）

- `title` 为空或全空格：`标题不能为空`
- `priority` 不在枚举内：`priority 必须是 low / medium / high`
- `status` 不在枚举内：`status 必须是 open / in_progress / resolved`
- ID 不存在：`404` `问题不存在：{id}`

数据保存在 `server/issues.json`：

## AR 基础理解

如果把虚拟场景标注改成真实 AR，平面检测和点击放置通常需要使用哪些 AR Foundation 能力？

平面检测依靠AR平面管理器，把识别到的水平面实例化，点击放置就用ar射线检测管理器，负责发出射线计算检测到的物理特征，然后提供相应的3d坐标

为什么只保存 Unity 中的 x/y/z 坐标，通常不能保证应用下次启动后仍对应同一个真实位置？

坐标系是相对于当前场景的，如果没有合适参照物，不同变量下的情况会导致每次启动的原点都不同

当 AR 初始化失败、设备不支持或 Tracking 状态异常时，应用应该如何提示用户？

先告诉用户当前问题，然后告诉用户下一步要做什么，并提供相关方法链接或者是在线服务。
