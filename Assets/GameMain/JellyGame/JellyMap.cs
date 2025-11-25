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

        // 0:空, 1:墙, 4:饼干墙(可破坏)
        private int[,] m_Grid;
        private int m_Size = 6;
        
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

        public void InitLevel(int[,] mapData)
        {
            m_Grid = mapData; // 这里应该深拷贝
            m_Entities.Clear();
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