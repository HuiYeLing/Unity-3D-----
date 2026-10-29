# AI_LOG.md — AI 使用记录

> 项目：虚拟现场巡检问题标注（Unity 无 AR 备选版 + Go + React）
> 说明：按实际时间顺序记录本题中主动发送给 AI 的指令与结果，包含追问与纠错。

---

## P-001

- 目标：评估这道面试题的整体工作量与风险，决定接不接、怎么排期

### Prompt 原文

> 这是一份三端闭环的面试题：Unity 端在场景中射线放置标记并上报问题 → Go 后端保存 → React 管理端查看并修改处理状态。帮我预估难度和开发时间

### 结果

- 采用
- AI 给出分模块工时：Go 后端 3h、React 5h、Unity AR 14-20h，难点在 AR 环境而非业务代码，总计 30-36h
- 验证：后续实际开发中 Unity 环境与排错确实占了主要时间，与预估一致；Go/React 部分实际用时也与预估接近

---

## P-002

- 目标：评估三端打通（联调）的耗时与风险点

### Prompt 原文

> 打通链路时间长吗

### 结果

- 采用
- AI 给出分层验证法：curl 探活 → 浏览器访问 React → 手机浏览器打开后端 /health → 最后才在 Unity 里点提交，并给出 `adb reverse tcp:8080 tcp:8080` 绕开局域网问题的技巧
- 验证：因最终选择 Editor 同机运行，链路天然简化为 localhost，实测 /health 探针一次通过，联调耗时远低于预估上限

---

## P-003

- 目标：在"AR 真机版"和"无 AR 备选版"之间做选择

### Prompt 原文

> 如果没有支持 ARCore 的真机，改做备选版（用 Unity 基础对象搭虚拟巡检现场，射线点击放置标记，不要求真实 AR），这道题和上一题哪个难，开发时间差距多大

### 结果

- 未采用 AR 真机版，采用无 AR 备选版
- 理由：手上没有确认支持 ARCore 的设备，AR 版有 10-16h 的环境/构建风险且可能直接卡死；备选版题目明确声明不因无真机演示扣分，工作量约为 AR 版的 60%
- 验证：备选版实际进度符合预期，未出现环境卡死

---

## P-004

- 目标：让 AI 直接产出 Unity 客户端的可运行实现

### Prompt 原文

> 那么unity部分该怎么做

### 结果

- 修改后采用
- AI 生成了 7 个 C# 脚本：IssueModels（数据契约）、ApiClient（HTTP）、UiGuard（UI 命中判断）、FirstPersonController（移动视角）、MarkerPlacer（射线放置）、IssueFormUI（表单）、Toast（提示），以及场景搭建指南
- 我的检查与修改：
  1. 逐个读脚本确认职责后再使用，重点核对了"四层短路防 UI 误放置"的判断顺序（表单开关 → 光标锁定 → UI 命中 → 鼠标按下）
  2. 核对提交坐标用 `hit.point`（射线命中点）而不是标记 transform（两者差一个表面偏移 6cm）
  3. 注意到列表接口必须包一层 `{"items":[...]}`，因为 JsonUtility 不支持顶层数组——这一点需要和 Go 端对齐
- 验证：脚本拷入项目编译通过；FirstPersonController 挂载后 WASD 移动、鼠标转视角正常

---

## P-005

- 目标：完全没有 Unity 经验，确定上手路径

### Prompt 原文

> 我是说，我是完全不懂unity，我该从哪里做起

### 结果

- 采用
- AI 给出"概念映射（GameObject≈DOM、Update≈requestAnimationFrame、Prefab≈组件模板）→ Day0 安装 → 三天里程碑（能走 → 放射线 → 接 UI 上报）"路线，并明确列出不需要学的内容（光照、Shader、动画等）
- 验证：按 Day1 里程碑当天跑通了 WASD 移动

---

## P-006

- 目标：确定安装哪个编辑器版本

### Prompt 原文

> （团结引擎 Hub 截图）选哪个

### 结果

- 采用
- 选 TUANJIE 1.11（1.11.0，基于 Unity 2022.3.62t17 LTS），排除 1.6.14（HMI 车机专用版）
- 验证：项目创建成功，7 个脚本无版本兼容问题；README 中版本一栏写"团结引擎 Tuanjie 1.11.0（基于 Unity 2022.3.62t17 LTS）"

---

## P-007

- 目标：确认安装时需要勾选的模块

### Prompt 原文

> 团结编辑器只需要勾选window build support吗

### 结果

- 采用
- 仅勾 Windows Build Support，跳过 Android/iOS（备选版全程 Editor 运行，无需移动端构建链）
- 验证：安装成功；后续若切 AR 版可随时在 Hub 补装 Android 模块

---

## P-008

- 目标：找不到创建 Cube 的入口

### Prompt 原文

> 我好像没有cube（附 Project 窗口截图）

### 结果

- 采用
- AI 指出混淆了 Project 窗口（文件）与 Hierarchy 窗口（场景物体）；Cube 应在 Hierarchy 右键 → 3D Object → Cube 创建
- 验证：创建成功，物体同时出现在 Hierarchy 与 Scene 视图

---

## P-009

- 目标：创建 Player 层（Layer）用于射线过滤

### Prompt 原文

> layer player在哪里

### 结果

- 采用
- Layer 不是现成的：Inspector 右上角 Layer 下拉 → Add Layer → User Layer 6 填 "Player" → 赋给 Player 物体并选择同时更改子物体
- 验证：Layer 列表出现 Player；配合 MarkerPlacer 的 raycastMask 取消勾选 Player，射线不再打到自身胶囊

---

## P-010

- 目标：搭建 Player 物体（空物体 + 碰撞胶囊 + 子相机）

### Prompt 原文

> 哪有player物体

### 结果

- 修改后采用
- AI 给出方案：Create Empty 命名 Player，挂 CharacterController（Center(0,-0.85,0)、Height 1.7、Radius 0.3）+ FirstPersonController，把 Main Camera 拖为子物体并归零 Transform
- 实际执行时出错：组件一度挂到了 Main Camera 上、相机没成为子物体，后在 P-014 中经截图诊断纠正
- 验证：纠正后结构为 Player(空物体) 驮 CharacterController + 脚本 + Camera 子物体

---

## P-011

- 目标：理解"加了 CharacterController 还不能动"的原因

### Prompt 原文

> 你都没有加脚本写wsad能移动它当然不能动了

### 结果

- 采用
- AI 确认：CharacterController 只是碰撞胶囊，移动由 FirstPersonController.cs 每帧调用 cc.Move() 驱动；需要先把 7 个脚本文件拷入 Assets/Scripts 才能在 Add Component 里搜到
- 验证：拷入脚本并挂载后移动生效

---

## P-012

- 目标：解决"所有编译错误"，无法进入运行模式

### Prompt 原文

> all cpmpilers errors 。。。。。 （附 Scene 视图截图）

### 结果

- 采用
- AI 要求提供 Console 第一条报错；随后根据报错路径 `Assets\FirstPersonController.cs`（根目录）判断：只拷了 1 个文件且位置不对，其依赖的 UiGuard 缺失导致 CS0103 连锁报错
- 修正：7 个脚本整体拷入 Assets/Scripts
- 验证：CS0103 消失

---

## P-013

- 目标：解决重复定义编译错误

### Prompt 原文

> （Console 截图：error CS0101: The namespace 'Inspection' already contains a definition for 'FirstPersonController'）

### 结果

- 采用
- 原因：Assets 根目录残留一份旧 FirstPersonController.cs，与 Assets/Scripts 里的新文件同名冲突
- 修正：删除根目录的旧文件，每个类全项目只保留一份
- 验证：CS0101 消失，编译通过

---

## P-014

- 目标：修复 WASD 无法移动

### Prompt 原文

> wasd无法移动 （附 Inspector 与 Console 截图）

### 结果

- 修改后采用
- AI 给出二分定位法：运行时选中 Player 看 Transform 数字变不变，区分"没在动"与"在动但看不见"
- 截图诊断出三个问题并逐一修正：
  1. Player 的 Scale 被设成了 0，导致 Step Offset 参数校验失败、CharacterController 被静默禁用 → Scale 改回 (1,1,1)
  2. CharacterController 和脚本误挂在 Main Camera 上 → 移除后重挂到 Player，Main Camera 拖为 Player 子物体
  3. Assets 根目录仍有旧脚本（见 P-015）→ 删除
- 验证：进入 Play 后 WASD 移动、鼠标转视角均正常，Console 仅剩两条无害警告

---

## P-015

- 目标：处理之前另一 AI 生成的移动脚本

### Prompt 原文

> （无独立 Prompt。该脚本为此前用其他 AI 工具生成的 FirstPersonController，放在 Assets 根目录）

### 结果

- 未采用，删除
- 原因：代码第 88 行使用了不存在的枚举值 `RigidbodyConstraints.freezeAll`（正确拼写为 `FreezeAll`），直接导致 CS0117 编译错误；且该方案基于 Rigidbody，与本项目采用的 CharacterController 方案冲突
- 教训：AI 生成的代码必须编译验证后再接入，不能多来源混用同一个类
- 验证：删除后 CS0117 消失，编译通过，项目只保留 Scripts 目录内经检查的一套脚本

---

## P-016

- 目标：学会手动搭建虚拟巡检场景

### Prompt 原文

> 你这个几何体，是怎么搞出来的

### 结果

- 采用
- AI 演示了"Hierarchy 右键创建 → F2 改名 → Inspector 手填 Transform"的完整流程，并给出 6 个物体的坐标表（Plane 地面、两面墙、两个机柜、一根躺倒的 Cylinder 管线）
- 关键理解：Cube 的 Position Y=高度一半使底部贴地；Cylinder Rotation Z=90 让柱子躺倒成管线
- 验证：场景运行走查正常、无法穿墙；此题不考察美术，未做额外美化

---

## P-017

- 目标：确认 Day 1 完成，开始正式课题实施

### Prompt 原文

> 能够正常移动了，只有两个黄色感叹号，我们是不是可以开始进行课题了

### 结果

- 采用
- 进入 Day 2：搭建场景几何体 → 用 5 行临时脚本验证 Physics.Raycast（屏幕坐标 → 世界射线 → 命中点 → 生成物体）→ 验收后删除临时脚本，待 UI 面板搭好后接入正式 MarkerPlacer
- 验证：射线放置、表单弹出、取消移除标记均正常

---

## P-018

- 目标：修复"关闭表单后 WASD 失灵，点一下空白处又恢复"

### Prompt 原文

> 我测试发现。点击后弹出表单，点击取消，此时wasd都用不了，点击右键就恢复正常了

### 结果

- 修改后采用
- 根因分析：按钮点击后 EventSystem 会持续保持该按钮为选中状态，而 AI 原先的 `UiGuard.IsUiFocused()` 判断条件是"只要有 Selectable 被选中就拦截移动"，于是表单关闭后移动被永久拦死；随手点一下场景空白处会清空选中，于是"莫名其妙恢复"，与观察到的现象完全吻合
- 修正两处：
  1. `UiGuard.IsUiFocused()` 收窄为"输入框 `isFocused` 才为真"，不再因为按钮被选中而拦截
  2. `IssueFormUI.Close()` 主动 `DeactivateInputField()` 并 `EventSystem.current.SetSelectedGameObject(null)`
- 验证：点取消后**不点击任何地方**直接按 WASD 可正常移动；同时回归确认输入框聚焦时相机仍不移动

---

## P-019

- 目标：让 TMP 文本正常显示中文

### Prompt 原文

> 我一创建text就报错（附 Console 截图：The Font Atlas Texture of the Font Asset msyh SDF is missing）

### 结果

- 修改后采用
- 根因：用 `msyh.ttc` 生成 TMP Font Asset，而 TMP 只支持 `.ttf` / `.otf`，`.ttc` 是字体合辑格式，无法生成图集纹理；Font Asset 图集缺失后，所有引用它的文本在编辑器里绘制面板时连锁抛 NullReferenceException
- 修正：删除坏字体资源，改用 `C:\Windows\Fonts\simhei.ttf`（标准 ttf）重新生成 Font Asset，并把 Default Font Asset 与既有文本的引用全部切换过去
- 验证：新建文本中文正常显示，Console 不再刷红；创建 Font Asset 后检查 Inspector 中 Atlas Texture 有贴图作为健康判据

---

## P-020

- 目标：排查"点击 Import TMP Essentials 只有提示音没有反应"

### Prompt 原文

> 点击之后除了系统提示音没有其他反应

### 结果

- 采用
- AI 要求先看 Console，红字为 `Cannot import package in play mode`
- 根因：编辑器仍处于运行模式（此前测试移动后未退出），运行状态下禁止导入资源包
- 修正：退出 Play 模式后重新导入
- 验证：Assets 下出现 `TextMesh Pro` 目录，导入成功
- 附带教训：同一时段在 Play 模式下搭建的 UI 面板在退出运行后全部回滚丢失，需重建。此后固定流程为"非运行状态编辑 → Ctrl+S → 再运行测试"

---

## P-021

- 目标：确认 Go 端已有骨架还缺什么，决定补充范围

### Prompt 原文

> （server/handlers.go、main.go、models.go、store.go 四个文件）结构应该已经打好了，看一下接下去要补充什么

### 结果

- 修改后采用
- AI 逐文件核对后给出结论：`models.go` 的 JSON tag 与 Unity 契约完全一致，不用改；`store.go` 只有函数签名且缺 `sync` 导入；`handlers.go` 是空文件；`main.go` 仍是 Hello World；另外**缺 `go.mod`**（`go build` 直接报 no main module）
- AI 产出：`store.go`（互斥锁 + JSON 文件原子写 + ID 自增）、`handlers.go`（health / 创建 / 列表 / 改状态 + 参数校验）、`main.go`（路由 + CORS 中间件 + 启动加载）
- 我的检查：
  1. 确认 `saveLocked()` 采用"写 .tmp 再 rename"而不是直接覆盖，避免写一半崩溃损坏数据
  2. 确认错误响应格式为 `{"error","message"}`，与 Unity `ApiClient.ExtractMessage` 的解析字段一致
  3. 确认列表返回 `{"items":[...]}` 而非裸数组，满足 JsonUtility 限制
- 验证：`go mod init` + `go build` 通过；curl 逐接口实测通过；杀掉进程重启后数据完整

---

## P-022

- 目标：把课题 9 条后端要求落到可执行的待办上

### Prompt 原文

> Go 后端需要为 Unity 和 React 提供以下能力：……（课题 3.3 全文）这里是课题的要求，服务器已经搭建完毕，把我需要做的事情规划好

### 结果

- 采用
- AI 把 9 条要求映射成一张验收表（每条对应一个 curl 命令与预期结果），并标出两个高风险项：**重启持久化**与 **CORS 预检 OPTIONS**（后者只影响 React，curl 测不出来）
- 验证：按表逐条执行，9 条全部通过；CORS 头在 `curl -i` 中确认存在

---

## P-023

- 目标：确认数据到底存在哪里，是否满足"重启后仍存在"

### Prompt 原文

> 那数据保存到哪里了，因为我的要求是数据在 Go 服务重启后仍然存在

### 结果

- 采用
- AI 说明：数据保存在 `server/issues.json`，创建/更新时由 `saveLocked()` 立即落盘，启动时 `load()` 恢复到内存 map 与自增序列；课题原文允许"SQLite 或本地 JSON 文件"，JSON 方案零第三方依赖（无需 CGO）
- 验证：POST 一条 → 文件生成 → Ctrl+C 停服 → 重启 → `GET /api/issues` 数据原样返回，`id` 序列未重复

---

## P-024

- 目标：寻找 Java 接口测试习惯在 Go 项目中的对应做法

### Prompt 原文

> 我是写java的，平常这个时候我会做接口的测试，看看能不能正确读取到数据，你告诉我在golang项目里这一步怎么做

### 结果

- 修改后采用
- AI 给出 Java → Go 的映射：JUnit ≈ `testing` 包，MockMvc ≈ `net/http/httptest`，并生成 `server/handlers_test.go`（表驱动测试，覆盖创建/校验/列表/改状态/持久化恢复）
- 首次运行全部用例的状态码断言都拿到 200（"got 200 want 201/400"），而 curl 实测返回的是正确的 201/400/404 —— 当时判断为测试辅助函数问题
- 根因后经 P-028 定位：**真正的原因是 `writeJSON` 缺少 `w.WriteHeader(code)`**，测试反而先暴露了这个真实缺陷
- 教训：测试全红时不要先怀疑测试，状态码"永远是 200、响应体却正确"这种组合，指向的正是"忘了写 header"

---

## P-025

- 目标：补测工具说明（Postman 方式）

### Prompt 原文

> 你先不用做，告诉我golang的接口如何用postman测试

### 结果

- 采用
- AI 给出可直接照填的 Postman 清单：4 个正常请求（health / 创建 / 列表 / 改状态）+ 4 个异常用例（空标题、非法优先级、非法状态、ID 不存在），并强调两个校验点：**状态码**与**响应体中的 message**
- 额外提示：Postman 不走浏览器同源策略，测不出 CORS；持久化必须靠手动重启验证
- 验证：按清单在 Postman 中逐条执行，结果与 curl 一致

---

## P-026

- 目标：按需求实现 React 管理端（列表 + 详情 + 改状态 + 断连不白屏）

### Prompt 原文

> 现在我们开始react页面的开发，只需要根据json数据展示到对应面板即可，要求能够获取问题列表，显示标题优先级以及状态，能够修改状态，然后在和后端失去联系时提示页面错误，不可直接白屏，注意风格简约不需要花哨设计，优先展示问题列表，点开对应问题后展示相关细节

### 结果

- 修改后采用
- AI 在 `admin/` 生成最小 Vite + React + TS 项目（不使用交互式脚手架）：`api.ts` 统一 fetch 与错误翻译、`types.ts` 对齐三端字段、`App.tsx` 列表/详情/状态下拉、`App.css` 简约样式
- 我的检查：
  1. 确认 `api.ts` 的 BASE_URL 指向 `http://127.0.0.1:8080`（Go 接口），而不是任何本地文件
  2. 确认 fetch 失败被完整 catch，渲染错误横幅 + 重试按钮，不会白屏
  3. 确认状态修改失败时受控 select 自动回滚到原值
- 验证：`tsc && vite build` 零错误；页面能展示 Unity 提交的真实数据（含长小数坐标）；停掉后端后刷新显示错误横幅而非白屏

---

## P-027

- 目标：澄清"React 是否绕过 Go 直接读写 JSON"的疑问

### Prompt 原文

> 你做错了，react页面的数据应该来自于go后端的结构，而不是直接去读取json然后保存了，这样没法实时查看待处理的问题

### 结果

- 未修改代码（核对后确认实现本就符合要求，属误解）
- 核对结论：`api.ts` 中唯一数据源是 `BASE_URL = 'http://127.0.0.1:8080'`，所有读写都走 Go 的 HTTP 接口；React 的 `setIssues` 只是组件内存中的视图状态，写盘只发生在 Go 侧（浏览器沙箱本身也读不到服务器磁盘文件）
- 实验验证：停掉 Go 后点刷新 → 页面显示错误横幅、列表为空（若真在读 JSON 文件反而不会报错）
- 遗留改进项：当前是"进页面拉一次 + 手动刷新"，如需"Unity 提交后自动出现"可加 5 秒轮询或 WebSocket；本题未实现

---
