using System.Collections.Generic;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace StarForce
{
    /// <summary>
    /// 管理网格数据、碰撞计算和回合逻辑的单例管理器
    /// </summary>
    public class MapManager
    {
        private static MapManager s_Instance;
        public static MapManager Instance => s_Instance ?? (s_Instance = new MapManager());

        // 地图常量定义 - 直接使用GameConstants中的常量
        // 常量定义在GameConstants.cs中
        
        // 0:空, 1:墙, 4:饼干墙(可破坏), 9:陷阱
        private int[,] m_Grid;
        private int m_Size = 6;
        private float m_CellSize = 2f; // 每个格子的大小
        
        // 存储动态实体的数据引用: Key=EntityId, Value=JellyData
        public Dictionary<int, JellyData> m_Entities = new Dictionary<int, JellyData>();

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

        /// <summary>
        /// 初始化关卡，深拷贝地图数据
        /// </summary>
        /// <param name="mapData">原始地图数据</param>
        public void InitLevel(int[,] mapData)
        {
            // 深拷贝地图数据
            if (mapData == null || mapData.GetLength(0) == 0 || mapData.GetLength(1) == 0)
            {
                Log.Error("Invalid map data");
                return;
            }
            
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
        }

        public void AddEntity(int id, int type, int x, int y)
        {
            // 使用GameConstants中的默认生命值
            int defaultHp = type == GameConstants.ENTITY_TYPE_PLAYER ? GameConstants.DEFAULT_PLAYER_HP : GameConstants.DEFAULT_ENEMY_HP;
            m_Entities[id] = new JellyData { Id = id, Type = type, X = x, Y = y, Hp = defaultHp };
        }
        
        /// <summary>
        /// 将网格坐标转换为世界坐标
        /// </summary>
        public Vector3 GridToWorld(int x, int y)
        {
            // 计算中心点偏移
            float offset = (m_Size - 1) * 0.5f * m_CellSize;
            return new Vector3(x * m_CellSize - offset, 0, y * m_CellSize - offset);
        }
        
        /// <summary>
        /// 对实体造成伤害
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="damage">伤害值</param>
        /// <returns>是否造成致命伤害</returns>
        public bool ApplyDamage(int entityId, int damage)
        {
            return ApplyDamage(entityId, damage, false);
        }
        
        /// <summary>
        /// 对实体造成伤害
        /// </summary>
        /// <param name="entityId">实体ID</param>
        /// <param name="damage">伤害值</param>
        /// <param name="isCrit">是否暴击</param>
        /// <returns>是否造成致命伤害</returns>
        public bool ApplyDamage(int entityId, int damage, bool isCrit)
        {
            if (!m_Entities.TryGetValue(entityId, out JellyData data) || data.IsDead)
            {
                Log.Warning("Entity {0} not found or already dead when applying damage", entityId);
                return false;
            }
            
            int actualDamage = damage;
            
            // 暴击伤害计算 - 使用GameConstants中的常量
            if (isCrit)
            {
                actualDamage = Mathf.RoundToInt(damage * GameConstants.CRIT_DAMAGE_MULTIPLIER);
            }
            
            data.Hp -= actualDamage;
            
            // 触发伤害事件，包含伤害数字和暴击效果
            GameEntry.Event.Fire(this, DamageEventArgs.Create(entityId, actualDamage, isCrit));
            
            // 触发伤害反馈事件，用于显示伤害数字
            if (GameEntry.Entity.TryGetEntity(entityId, out Entity entity))
            {
                DamageFeedbackEventArgs args = DamageFeedbackEventArgs.Create(entityId, actualDamage, isCrit, entity.CachedTransform.position);
                GameEntry.Event.Fire(this, args);
            }
            
            // 获取实体位置，用于播放特效
            if (GameEntry.Entity.TryGetEntity(entityId, out Entity entity))
            {
                // 播放命中特效
                EffectsManager.Instance.PlayHitEffect(entity.CachedTransform.position);
            }
            
            bool isKilled = data.IsDead;
            
            if (isKilled)
            {
                // 触发死亡事件
                GameEntry.Event.Fire(this, JellyKilledEventArgs.Create(entityId, data.Type));
                
                // 播放死亡特效
                if (GameEntry.Entity.TryGetEntity(entityId, out Entity deadEntity))
                {
                    EffectsManager.Instance.PlayDeathEffect(deadEntity.CachedTransform.position);
                }
            }
            
            return isKilled;
        }
        
        /// <summary>
        /// 检查并处理陷阱伤害
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        /// <returns>是否是陷阱</returns>
        public bool CheckTrap(int x, int y)
        {
            if (x < 0 || x >= m_Size || y < 0 || y >= m_Size)
                return false;
                
            return m_Grid[y, x] == GameConstants.MAP_CELL_TRAP;
        }
        
        /// <summary>
        /// 触发陷阱
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        public void TriggerTrap(int x, int y)
        {
            if (x < 0 || x >= m_Size || y < 0 || y >= m_Size)
                return;
                
            if (m_Grid[y, x] == GameConstants.MAP_CELL_TRAP)
            {
                // 获取陷阱的世界坐标
                Vector3 worldPos = GridToWorld(x, y);
                
                // 播放陷阱特效和音效
                EffectsManager.Instance.PlayTrapEffect(worldPos);
                
                // 这里可以设置陷阱在一定时间后重置
                // StartCoroutine(ResetTrapAfterDelay(x, y, 2.0f));
            }
        }
        
        /// <summary>
        /// 延迟重置陷阱协程
        /// </summary>
        private System.Collections.IEnumerator ResetTrapAfterDelay(int x, int y, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (x >= 0 && x < m_Size && y >= 0 && y < m_Size && m_Grid[y, x] == GameConstants.MAP_CELL_EMPTY)
            {
                m_Grid[y, x] = GameConstants.MAP_CELL_TRAP;
            }
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
            if (cellType == GameConstants.MAP_CELL_WALL) break; // 硬墙
            if (cellType == GameConstants.MAP_CELL_CRACKER_WALL) 
            {
                // 撞到饼干墙
                result.HitWallType = GameConstants.MAP_CELL_CRACKER_WALL;
                result.HitWallX = nextX;
                result.HitWallY = nextY;
                break; 
            }
            if (cellType == GameConstants.MAP_CELL_TRAP)
                {
                    // 碰到陷阱，记录但允许移动到陷阱上
                    result.HitTrap = true;
                    result.HitTrapX = nextX;
                    result.HitTrapY = nextY;
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
            
            // 检查最终位置是否在陷阱上
            if (!result.HitTrap && currX != startX && currY != startY)
            {
                result.HitTrap = CheckTrap(currX, currY);
                result.HitTrapX = currX;
                result.HitTrapY = currY;
            }
            
            return result;
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
        
        /// <summary>
        /// 更新实体位置
        /// </summary>
        public void UpdateEntityPosition(int entityId, int x, int y)
        {            
            if (m_Entities.TryGetValue(entityId, out JellyData data))
            {
                data.X = x;
                data.Y = y;
            }
        }
        
        /// <summary>
        /// 破坏墙壁
        /// </summary>
        /// <param name="x">x坐标</param>
        /// <param name="y">y坐标</param>
        /// <returns>是否成功破坏</returns>
        public bool BreakWall(int x, int y)
        {
            if (x < 0 || x >= m_Size || y < 0 || y >= m_Size)
                return false;
                
            if (m_Grid[y, x] == GameConstants.MAP_CELL_CRACKER_WALL)
            {
                // 获取墙壁的世界坐标
                Vector3 worldPos = GridToWorld(x, y);
                
                // 播放墙壁破坏特效和音效
                EffectsManager.Instance.PlayWallBreakEffect(worldPos);
                
                // 更新地图数据
                m_Grid[y, x] = GameConstants.MAP_CELL_EMPTY;
                Log.Info($"Wall broken at ({x}, {y})");
                
                // 触发墙壁破坏事件
                GameEntry.Event.Fire(this, WallBrokenEventArgs.Create(x, y));
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取当前关卡的敌人数量
        /// </summary>
        public int GetEnemyCount()
        {
            int count = 0;
            foreach (var data in m_Entities.Values)
            {
                if (!data.IsDead && data.Type == 1)
                    count++;
            }
            return count;
        }
        
        /// <summary>
        /// 检查关卡是否胜利
        /// </summary>
        public bool CheckWinCondition()
        {
            return GetEnemyCount() == 0;
        }
        
        /// <summary>
        /// 检查关卡是否失败
        /// </summary>
        public bool CheckLoseCondition()
        {
            foreach (var data in m_Entities.Values)
            {
                if (data.Type == 0 && !data.IsDead) // 有玩家存活
                    return false;
            }
            return true;
        }
        
        /// <summary>
        /// 获取所有玩家实体
        /// </summary>
        public List<int> GetAllPlayers()
        {
            List<int> players = new List<int>();
            foreach (var kv in m_Entities)
            {
                if (!kv.Value.IsDead && kv.Value.Type == 0)
                    players.Add(kv.Key);
            }
            return players;
        }
        
        /// <summary>
        /// 获取所有敌人实体
        /// </summary>
        public List<int> GetAllEnemies()
        {
            List<int> enemies = new List<int>();
            foreach (var kv in m_Entities)
            {
                if (!kv.Value.IsDead && kv.Value.Type == 1)
                    enemies.Add(kv.Key);
            }
            return enemies;
        }
        
        /// <summary>
        /// 检查实体是否激活
        /// </summary>
        public bool IsEntityActive(int entityId)
        {
            return m_Entities.ContainsKey(entityId) && !m_Entities[entityId].IsDead;
        }
        
        /// <summary>
        /// 获取所有玩家的位置
        /// </summary>
        public List<Vector2Int> GetPlayerPositions()
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            foreach (var kv in m_Entities)
            {
                if (!kv.Value.IsDead && kv.Value.Type == 0)
                {
                    positions.Add(new Vector2Int(kv.Value.X, kv.Value.Y));
                }
            }
            return positions;
        }
        
        /// <summary>
        /// 检查实体是否可以移动到指定位置
        /// </summary>
        public bool CanEntityMove(int entityId, Vector2Int newPos)
        {
            // 检查边界
            if (newPos.x < 0 || newPos.x >= m_Size || newPos.y < 0 || newPos.y >= m_Size)
            {
                return false;
            }
            
            // 检查是否有障碍物
            int cellType = m_Grid[newPos.y, newPos.x];
            if (cellType == GameConstants.MAP_CELL_WALL)
            {
                return false;
            }
            
            // 检查是否有其他实体
            int existingEntity = GetEntityAt(newPos.x, newPos.y);
            if (existingEntity != -1 && existingEntity != entityId)
            {
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取从一个位置到另一个位置的方向
        /// </summary>
        public Vector2Int GetDirection(int fromX, int fromY, int toX, int toY)
        {
            int dx = toX - fromX;
            int dy = toY - fromY;
            
            // 优先选择绝对值较大的方向
            if (Mathf.Abs(dx) > Mathf.Abs(dy))
            {
                return new Vector2Int(dx > 0 ? 1 : -1, 0);
            }
            else
            {
                return new Vector2Int(0, dy > 0 ? 1 : -1);
            }
        }
        
        /// <summary>
        /// 获取两个位置之间的曼哈顿距离
        /// </summary>
        public int GetManhattanDistance(int x1, int y1, int x2, int y2)
        {
            return Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2);
        }

        public Vector3 GridToWorld(int x, int y)
        {
            // 假设格子大小为 1.5 单位，原点在左下角
            return new Vector3(x * 1.5f, y * 1.5f, 0);
        }

        // 简单的伤害处理
        public void ApplyDamage(int entityId, int dmg)
        {
            if (m_Entities.TryGetValue(entityId, out JellyData data))
            {
                data.Hp -= dmg;
                if (data.Hp <= 0) Log.Info($"Entity {entityId} Died.");
            }
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