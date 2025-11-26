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
        private float m_CellSize = 1.5f;
        
        // 存储动态实体的数据引用: Key=EntityId, Value=JellyData
        public Dictionary<int, JellyData> m_Entities = new Dictionary<int, JellyData>();
        
        // 网格可视化相关
        private GameObject m_GridVisualRoot;
        private GameObject[,] m_CellVisuals; // 存储格子的可视化对象
        private Color m_EmptyCellColor = new Color(0.9f, 0.9f, 0.9f, 0.5f); // 空格子颜色
        private Color m_WallCellColor = new Color(0.3f, 0.3f, 0.3f, 1f);    // 墙颜色
        private Color m_CrackerColor = new Color(0.8f, 0.6f, 0.2f, 1f);     // 饼干墙颜色

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
        
        /// <summary>
        /// 创建网格可视化
        /// </summary>
        private void CreateGridVisuals()
        {
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
                    GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    cell.name = $"Cell_{x}_{y}";
                    cell.transform.SetParent(m_GridVisualRoot.transform);
                    
                    // 设置位置和大小
                    Vector3 worldPos = new Vector3(
                        x * m_CellSize + offsetX,
                        y * m_CellSize + offsetY,
                        1f); // 在实体下方
                    cell.transform.position = worldPos;
                    cell.transform.localScale = new Vector3(m_CellSize * 0.95f, m_CellSize * 0.95f, 1f);
                    
                    // 设置颜色
                    Renderer renderer = cell.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        switch (m_Grid[y, x])
                        {
                            case 1: // 墙
                                renderer.material.color = m_WallCellColor;
                                break;
                            case 4: // 饼干墙
                                renderer.material.color = m_CrackerColor;
                                break;
                            default: // 空格子
                                renderer.material.color = m_EmptyCellColor;
                                break;
                        }
                    }
                    
                    // 添加边框
                    AddBorderToCell(cell);
                    
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
                    Renderer renderer = cell.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        switch (m_Grid[y, x])
                        {
                            case 1: // 墙
                                renderer.material.color = m_WallCellColor;
                                break;
                            case 4: // 饼干墙
                                renderer.material.color = m_CrackerColor;
                                break;
                            default: // 空格子
                                renderer.material.color = m_EmptyCellColor;
                                break;
                        }
                    }
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
            // 根据网格大小和偏移量计算世界坐标
            float offsetX = -(m_Size - 1) * m_CellSize * 0.5f;
            float offsetY = -(m_Size - 1) * m_CellSize * 0.5f;
            return new Vector3(
                x * m_CellSize + offsetX,
                y * m_CellSize + offsetY,
                0f);
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
        
        /// <summary>
        /// 清理地图资源
        /// </summary>
        public void Cleanup()
        {
            if (m_GridVisualRoot != null)
            {
                Object.Destroy(m_GridVisualRoot);
                m_GridVisualRoot = null;
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