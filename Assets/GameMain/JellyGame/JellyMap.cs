using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;
using GameFramework.Resource;

namespace StarForce
{
    /// <summary>
    /// 管理网格数据、碰撞计算和回合逻辑的单例管理器
    /// </summary>
    public class MapManager
    {
        private static MapManager s_Instance;
        public static MapManager Instance => s_Instance ?? (s_Instance = new MapManager());

        // Sprite 资源管理
        private Dictionary<string, Sprite> m_Sprites = new Dictionary<string, Sprite>();
        private bool m_IsSpritesLoaded = false;

        // 0:空, 1:墙, 4:饼干墙(可破坏)
        private int[,] m_Grid;
        private int m_Size = 6;
        private float m_CellSize = 1.5f;
        
        // 存储动态实体的数据引用: Key=EntityId, Value=JellyData
        public Dictionary<int, JellyData> m_Entities = new Dictionary<int, JellyData>();
        
        // 网格可视化相关
        private GameObject m_GridVisualRoot;
        private GameObject[,] m_CellVisuals; // 存储格子的可视化对象
        private Color m_EmptyCellColor = new Color(0.9f, 0.9f, 0.9f, 0.5f); // 空格子颜色
        private Color m_WallCellColor = new Color(0.3f, 0.3f, 0.3f, 1f);    // 墙颜色
        private Color m_CrackerColor = new Color(0.8f, 0.6f, 0.2f, 1f);     // 饼干墙颜色
        private Color m_TrapColor = new Color(0.6f, 0.1f, 0.1f, 1f);        // 陷阱颜色 (深红)

        // 简单的实体数据类
        public class JellyData
        {
            public int Id;
            public int Type; // 0:Player, 1:Enemy
            public int X;
            public int Y;
            public int Hp;
            public bool IsDead => Hp <= 0;
        }

        public void InitLevel(int[,] mapData)
        {
            // 加载 Sprite 资源
            LoadSprites();

            // 深拷贝地图数据
            m_Size = mapData.GetLength(0);
            m_Grid = new int[m_Size, m_Size];
            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    m_Grid[y, x] = mapData[y, x];
                }
            }
            
            m_Entities.Clear();
            CreateGridVisuals();
        }
        
        private void LoadSprites()
        {
            if (m_IsSpritesLoaded) return;

            GameEntry.Resource.LoadAsset("Assets/GameMain/Atlas/jelly_spritesheet_512.png", typeof(Texture2D), new LoadAssetCallbacks(
                (assetName, asset, duration, userData) =>
                {
                    Texture2D texture = (Texture2D)asset;
                    if (texture == null) return;

                    // 创建 Sprite (基于 .meta 文件数据)
                    // Texture Size: 512x512
                    // Pivot: 0.5, 0.5, PixelsPerUnit: 64
                    m_Sprites["player"] = Sprite.Create(texture, new Rect(0, 448, 64, 64), new Vector2(0.5f, 0.5f), 64);
                    m_Sprites["monster"] = Sprite.Create(texture, new Rect(64, 448, 64, 64), new Vector2(0.5f, 0.5f), 64);
                    m_Sprites["wall"] = Sprite.Create(texture, new Rect(128, 448, 64, 64), new Vector2(0.5f, 0.5f), 64);
                    m_Sprites["box"] = Sprite.Create(texture, new Rect(192, 448, 64, 64), new Vector2(0.5f, 0.5f), 64);
                    m_Sprites["warn"] = Sprite.Create(texture, new Rect(256, 448, 64, 64), new Vector2(0.5f, 0.5f), 64);
                    m_Sprites["black"] = Sprite.Create(texture, new Rect(320, 448, 64, 64), new Vector2(0.5f, 0.5f), 64);

                    m_IsSpritesLoaded = true;
                    Log.Info("Sprites loaded successfully from atlas.");

                    // 资源加载完成后刷新显示
                    RefreshAllVisuals();
                    RefreshEntityVisuals();
                },
                (assetName, status, errorMessage, userData) =>
                {
                    Log.Error($"Load Sprites Failed: {errorMessage}");
                }
            ));
        }

        public Sprite GetSprite(string name)
        {
            if (m_Sprites.TryGetValue(name, out Sprite sprite))
            {
                return sprite;
            }
            return null;
        }

        private void RefreshAllVisuals()
        {
            if (m_CellVisuals == null) return;
            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    UpdateGridVisual(x, y);
                }
            }
        }

        private void RefreshEntityVisuals()
        {
            foreach (var kv in m_Entities)
            {
                var entity = GameEntry.Entity.GetEntity(kv.Key);
                if (entity != null && entity.Logic is JellyLogic jellyLogic)
                {
                    jellyLogic.UpdateSprite();
                }
            }
        }

        /// <summary>
        /// 创建网格可视化
        /// </summary>
        private void CreateGridVisuals()
        {
            Log.Info($"CreateGridVisuals: Size={m_Size}, CellSize={m_CellSize}");

            // 销毁旧的网格可视化
            if (m_GridVisualRoot != null)
            {
                Object.Destroy(m_GridVisualRoot);
            }
            
            // 创建新的根对象
            m_GridVisualRoot = new GameObject("GridVisuals");
            m_CellVisuals = new GameObject[m_Size, m_Size];
            
            // 计算偏移量，使网格居中
            float offsetX = -(m_Size - 1) * m_CellSize * 0.5f;
            float offsetY = -(m_Size - 1) * m_CellSize * 0.5f;
            
            // 创建每个格子的可视化
            for (int y = 0; y < m_Size; y++)
            {
                for (int x = 0; x < m_Size; x++)
                {
                    GameObject cell = new GameObject($"Cell_{x}_{y}");
                    cell.transform.SetParent(m_GridVisualRoot.transform);
                    
                    // 添加 SpriteRenderer
                    SpriteRenderer renderer = cell.AddComponent<SpriteRenderer>();
                    renderer.sortingOrder = 0; // 地图层级

                    // 设置位置和大小
                    Vector3 worldPos = new Vector3(
                        x * m_CellSize + offsetX,
                        y * m_CellSize + offsetY,
                        1f); // 在实体下方
                    cell.transform.position = worldPos;
                    cell.transform.localScale = new Vector3(m_CellSize * 0.95f, m_CellSize * 0.95f, 1f); // 调整缩放适配格子

                    // 设置初始 Sprite (如果已加载)
                    UpdateGridVisualInternal(x, y, renderer);
                    
                    // 添加边框
                    //AddBorderToCell(cell);
                    
                    m_CellVisuals[y, x] = cell;
                }
            }
        }
        
        /// <summary>
        /// 为格子添加边框
        /// </summary>
        private void AddBorderToCell(GameObject cell)
        {
            // 创建边框对象
            GameObject border = new GameObject("Border");
            border.transform.SetParent(cell.transform);
            border.transform.localPosition = Vector3.zero;
            border.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
            
            // 添加LineRenderer组件
            LineRenderer lineRenderer = border.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 5;
            lineRenderer.useWorldSpace = false;
            lineRenderer.widthMultiplier = 0.05f;
            lineRenderer.startColor = Color.black;
            lineRenderer.endColor = Color.black;
            
            // 设置边框顶点（正方形边框）
            Vector3[] points = new Vector3[5];
            points[0] = new Vector3(-0.5f, -0.5f, 0.1f);
            points[1] = new Vector3(0.5f, -0.5f, 0.1f);
            points[2] = new Vector3(0.5f, 0.5f, 0.1f);
            points[3] = new Vector3(-0.5f, 0.5f, 0.1f);
            points[4] = new Vector3(-0.5f, -0.5f, 0.1f); // 闭合
            
            lineRenderer.SetPositions(points);
            
            // 移除碰撞器，避免影响游戏逻辑
            Collider collider = cell.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }
        }
        
        /// <summary>
        /// 更新网格可视化（当格子类型改变时调用）
        /// </summary>
        public void UpdateGridVisual(int x, int y)
        {
            if (x >= 0 && x < m_Size && y >= 0 && y < m_Size && m_CellVisuals != null)
            {
                GameObject cell = m_CellVisuals[y, x];
                if (cell != null)
                {
                    SpriteRenderer renderer = cell.GetComponent<SpriteRenderer>();
                    UpdateGridVisualInternal(x, y, renderer);
                }
            }
        }

        private void UpdateGridVisualInternal(int x, int y, SpriteRenderer renderer)
        {
            if (renderer == null) return;

            string spriteName = "black"; // 默认/空格子
            Color color = Color.white;

            switch (m_Grid[y, x])
            {
                case 1: // 墙
                    spriteName = "wall";
                    break;
                case 4: // 饼干墙
                    spriteName = "box";
                    break;
                case 9: // 陷阱
                    spriteName = "warn";
                    break;
                default: // 空格子
                    spriteName = "black";
                    break;
            }

            Sprite sprite = GetSprite(spriteName);
            if (sprite != null)
            {
                renderer.sprite = sprite;
                renderer.color = Color.white;
            }
            else
            {
                // Fallback to colors if sprite not loaded yet
                renderer.sprite = null; 
                switch (m_Grid[y, x])
                {
                    case 1: renderer.color = m_WallCellColor; break;
                    case 4: renderer.color = m_CrackerColor; break;
                    case 9: renderer.color = m_TrapColor; break;
                    default: renderer.color = m_EmptyCellColor; break;
                }
            }
        }

        public void AddEntity(int id, int type, int x, int y)
        {
            m_Entities[id] = new JellyData { Id = id, Type = type, X = x, Y = y, Hp = 3 };
        }

        // 核心算法：计算从 (startX, startY) 向 (dirX, dirY) 滑行的结果
        // 返回：最终坐标，是否发生碰撞，碰撞对象ID
        public SlideResult CalculateSlide(int startX, int startY, int dirX, int dirY, int selfId)
        {
            int currX = startX;
            int currY = startY;
            SlideResult result = new SlideResult();

            while (true)
            {
                int nextX = currX + dirX;
                int nextY = currY + dirY;

                // 1. 边界检查
                if (nextX < 0 || nextX >= m_Size || nextY < 0 || nextY >= m_Size)
                    break;

                // 2. 静态墙壁检查
                int cellType = m_Grid[nextY, nextX];
                if (cellType == 1) break; // 硬墙
                if (cellType == 4) 
                {
                    // 撞到饼干墙
                    result.HitWallType = 4;
                    result.HitWallX = nextX;
                    result.HitWallY = nextY;
                    
                    // 移除饼干墙（破坏）
                    m_Grid[nextY, nextX] = 0;
                    // 更新可视化
                    UpdateGridVisual(nextX, nextY);
                    break; 
                }
                
                // 2.5 陷阱检查
                if (cellType == 9)
                {
                    // 踩到陷阱，移动到该位置并死亡
                    currX = nextX;
                    currY = nextY;
                    ApplyDamage(selfId, 999); // 即死
                    break;
                }

                // 3. 动态实体检查
                int blockerId = GetEntityAt(nextX, nextY);
                if (blockerId != -1 && blockerId != selfId)
                {
                    result.HitEntityId = blockerId;
                    break;
                }

                // 移动一步
                currX = nextX;
                currY = nextY;
            }

            result.FinalX = currX;
            result.FinalY = currY;
            return result;
        }

        // 击退逻辑
        // 返回：是否造成了 Wall Slam (Crit)
        public bool ApplyKnockback(int entityId, int dirX, int dirY)
        {
            if (!m_Entities.TryGetValue(entityId, out JellyData data)) return false;

            int nextX = data.X + dirX;
            int nextY = data.Y + dirY;

            // 1. 检查是否出界
            if (nextX < 0 || nextX >= m_Size || nextY < 0 || nextY >= m_Size)
            {
                return true; // 撞边界 = Wall Slam
            }

            // 2. 检查是否撞墙
            int cellType = m_Grid[nextY, nextX];
            if (cellType == 1 || cellType == 4) // 硬墙或饼干墙
            {
                return true; // 撞墙 = Wall Slam
            }

            // 3. 检查是否撞人 (简单的处理：如果后面有人，也算撞墙，或者连锁反应？这里先简化为撞墙)
            int blockerId = GetEntityAt(nextX, nextY);
            if (blockerId != -1)
            {
                return true; // 撞人 = Wall Slam (简化处理)
            }

            // 4. 执行击退
            data.X = nextX;
            data.Y = nextY;
            
            // 检查击退后是否踩到陷阱
            if (m_Grid[nextY, nextX] == 9) // 陷阱
            {
                ApplyDamage(entityId, 999); // 即死
            }

            return false; // 成功击退，没有撞墙
        }

        public int GetEntityAt(int x, int y)
        {
            foreach (var kv in m_Entities)
            {
                if (!kv.Value.IsDead && kv.Value.X == x && kv.Value.Y == y)
                    return kv.Key;
            }
            return -1;
        }

        public Vector3 GridToWorld(int x, int y)
        {
            // 根据网格大小和偏移量计算世界坐标
            float offsetX = -(m_Size - 1) * m_CellSize * 0.5f;
            float offsetY = -(m_Size - 1) * m_CellSize * 0.5f;
            return new Vector3(
                x * m_CellSize + offsetX,
                y * m_CellSize + offsetY,
                0f);
        }

        // 简单的伤害处理
        public bool ApplyDamage(int entityId, int dmg)
        {
            Log.Info($"ApplyDamage: Entity {entityId} took {dmg} damage. Trace: {System.Environment.StackTrace}");
            if (m_Entities.TryGetValue(entityId, out JellyData data))
            {
                data.Hp -= dmg;
                if (data.Hp <= 0) 
                {
                    Log.Info($"Entity {entityId} Died.");
                    return true;
                }
            }
            return false;
        }

        public void TriggerTrap(int x, int y)
        {
            // 可以在这里添加陷阱触发的视觉效果逻辑
            Log.Info($"Trap triggered at ({x}, {y})");
        }

        public void ApplyRepulseToEntity(int entityId, int dirX, int dirY)
        {
            // 调用现有的 ApplyKnockback 逻辑
            ApplyKnockback(entityId, dirX, dirY);
            
            // 更新视觉位置
            if (m_Entities.TryGetValue(entityId, out JellyData data))
            {
                var entity = GameEntry.Entity.GetEntity(entityId);
                if (entity != null && entity.Logic is JellyLogic jellyLogic)
                {
                    // 简单的位移动画或直接设置位置
                    // 这里我们假设 JellyLogic 会处理平滑移动，或者我们直接设置位置
                    // 为了简单起见，我们让 JellyLogic 自己处理动画，这里只更新数据
                    // 但实际上 JellyLogic 需要知道它被击退了
                    jellyLogic.ApplyRepulse(dirX, dirY);
                }
            }
        }

        public bool ApplyDamageToEntity(int entityId, int damage, bool isCrit, int attackerId)
        {
            bool isKilled = ApplyDamage(entityId, damage);
            
            // 触发事件或回调
            var entity = GameEntry.Entity.GetEntity(entityId);
            if (entity != null && entity.Logic is JellyLogic jellyLogic)
            {
                // 通知 JellyLogic 它受伤了（虽然 JellyLogic 可能已经通过事件知道了，但这里是直接调用）
                // 注意：JellyLogic.TakeDamage 也会调用 ApplyDamage，所以要小心死循环
                // 这里的 ApplyDamageToEntity 应该是更高层的逻辑
                // 我们修改 JellyLogic.TakeDamage 不再调用 MapManager.ApplyDamage，或者这里只做数据更新
                
                // 修正：JellyLogic.TakeDamage 负责调用 MapManager.ApplyDamage
                // 所以这里我们应该调用 JellyLogic.TakeDamage
                // 但 JellyLogic.TakeDamage 又会调用 MapManager.ApplyDamage...
                // 让我们理清一下：
                // 1. 碰撞发生 -> JellyLogic.HandleCollision -> MapManager.ApplyDamageToEntity
                // 2. MapManager.ApplyDamageToEntity -> ApplyDamage (数据) -> JellyLogic.PlayHitAnimation (表现)
                
                // 实际上，JellyLogic.TakeDamage 设计为处理“受到伤害”的全部逻辑（数据+表现）
                // 所以我们应该调用 JellyLogic.TakeDamage，但要避免重复扣血
                
                // 方案：让 JellyLogic.TakeDamage 负责扣血。MapManager.ApplyDamageToEntity 只负责调用它。
                // 但 MapManager.ApplyDamage 是数据层核心。
                
                // 让我们简化：
                // MapManager.ApplyDamage 只负责数据。
                // JellyLogic.TakeDamage 负责表现 + 触发事件。
                
                // 这里我们只更新数据，然后通知表现
                // isKilled 已经在上面计算了
                
                // 播放受击动画
                jellyLogic.PlayHitAnimation();
                
                // 飘字
                GameEntry.Event.Fire(this, DamageEventArgs.Create(entityId, damage, isCrit));
                
                if (isKilled)
                {
                    jellyLogic.Die();
                    GameEntry.Event.Fire(this, JellyKilledEventArgs.Create(entityId));
                }
            }
            return isKilled;
        }
        
        /// <summary>
        /// 清理地图资源
        /// </summary>
        public void Cleanup()
        {
            Log.Info($"MapManager.Cleanup called.");
            if (m_GridVisualRoot != null)
            {
                Object.Destroy(m_GridVisualRoot);
                m_GridVisualRoot = null;
            }
            
            // Note: We do not clear sprites here because they are global resources for the session.
            // If we want to clear them, we should unload the texture.
            // For now, keep them cached.
            
            // 隐藏所有实体
            foreach (var entityId in new List<int>(m_Entities.Keys))
            {
                if (GameEntry.Entity.HasEntity(entityId))
                {
                    GameEntry.Entity.HideEntity(entityId);
                }
            }
            
            m_CellVisuals = null;
            m_Entities.Clear();
            m_Grid = null;
        }
    }

    public class SlideResult
    {
        public int FinalX, FinalY;
        public int HitEntityId; // -1 表示没撞人
        public int HitWallType; // 0 表示没撞墙
        public int HitWallX, HitWallY;

        public SlideResult() { HitEntityId = -1; HitWallType = 0; FinalX=0; FinalY=0; HitWallX=-1; HitWallY=-1; }
    }
}