# 完成日志：敌人寻路与流场AI

## 项目结构
- 修改 `EnemyManager.cs`，添加流场寻路计算和查询功能，并修复了方向计算错误和类型转换问题。
- 修改 `Enemy.cs`，使其使用 `EnemyManager` 提供的流场方向进行移动，并增加了移动平滑处理以减少墙角卡顿。

## 场景节点结构
- 确保场景中有一个挂载了 `EnemyManager.cs` 脚本的 GameObject。
- 确保场景中有一个或多个 Tilemap GameObject，其中包含表示墙体障碍物的瓦片。
- **重要：需要将表示墙体障碍物的 Tilemap 组件拖拽赋值给 `EnemyManager` 脚本上的 `Wall Tilemap` 字段。**

## 系统实现逻辑
1. **流场计算 (BFS)**：
   - 使用广度优先搜索(BFS)算法，从玩家当前所在的瓦片开始计算到地图上每个可通行瓦片的最小距离。
   - 同时，根据距离信息构建流场，每个瓦片指向相邻的距离最小的瓦片，形成指向玩家的向量场。
   - 流场计算考虑了墙体障碍物以及对角线移动时的角落检查，确保寻路路径的有效性。
   - **移除了流场计算完成时的调试日志，保持控制台输出简洁。**
   - 流场会根据设定的时间间隔自动更新，以适应玩家位置变化。
2. **方向生成 (EnemyManager)**：根据距离场，`EnemyManager` 为每个可通行瓦片计算出一个指向距离递减方向（即流场方向），存储在内部字典中。
3. **敌人移动 (Enemy)**：敌人（在非击退状态下）会获取自身当前位置的瓦片坐标，并向 `EnemyManager` 查询该瓦片对应的流场方向。
4. **沿着流场移动 (Enemy)**：敌人根据获取到的流场方向来设置其移动速度，通过 `Vector2.Lerp` 进行平滑插值，从而实现绕过障碍物追踪玩家的效果。
5. **障碍物检测**：流场计算会跳过在指定 `Wall Tilemap` 上有瓦片的格子，确保敌人不会被导航到障碍物区域。

## 数据结构
- `EnemyManager` 包含：
  - 对 `Tilemap` (wallTilemap) 的引用。
  - 存储距离场的字典 (`distanceField`: `Dictionary<Vector3Int, int>`)。
  - 存储流场方向的字典 (`flowField`: `Dictionary<Vector3Int, Vector2>`)。
  - 流场更新间隔 (`flowFieldUpdateInterval`)。
  - 单例 (`Instance`)。
- `Enemy` 修改了移动逻辑，通过 `EnemyManager.Instance.GetDirectionToPlayer()` 获取移动方向，并使用了 `Vector2.Lerp` 进行速度平滑。

## 目录
- 项目结构与节点说明：本日志
- 敌人生成机制：完成日志_敌人生成机制.md
- 其他系统日志（根据需要列出）：例如完成日志_敌人血量与击退调节.md 等。
- 敌人寻路与流场AI实现细节：本日志已包含

## 用户需要操作的重点
1. 在你的 Unity 场景中，选中挂载了 `EnemyManager.cs` 脚本的 GameObject。
2. 在 Inspector 面板中找到 `Enemy Manager (Script)` 组件。
3. 将场景中代表墙体障碍物的 **Tilemap GameObject** 拖拽到 `Wall Tilemap` 字段。
4. **流场计算完成的调试信息已移除**，如需调试流场计算过程，需要临时在代码中添加调试日志。
5. 运行场景，敌人将尝试使用流场寻路向玩家移动。
6. 调整 `EnemyManager` 的 `Flow Field Update Interval` 参数，可以控制流场更新频率。
7. 你可以在 `Enemy.cs` 中调整 `Vector2.Lerp` 的插值速度因子来优化移动平滑效果。

完成以上步骤后，你可以在场景中放置敌人预制体或者通过敌人生成机制来测试新的寻路AI。 