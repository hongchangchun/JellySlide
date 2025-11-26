using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;
using static StarForce.MapManager;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;

namespace StarForce
{
    public class ProcedureJellyGame : ProcedureBase
    {
        public override bool UseNativeDialog
        {
            get
            {
                return false;
            }
        }
        
        private bool m_IsPlayerTurn = true;
        private bool m_IsAnimating = false;
        
        // 简单的输入记录
        private Vector2 m_TouchStartPos;
        private bool m_IsTouching = false;
        
        // 地图相关配置
        private const float CELL_SIZE = 1.5f; // 与MapManager保持一致

        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            m_IsPlayerTurn = true;
            m_IsAnimating = false;

            // 1. 初始化地图数据 (Demo硬编码，实际应从 DataTable 读取)
            int[,] map = new int[6, 6] {
                {1,1,1,1,1,1},
                {1,0,0,0,0,1},
                {1,0,2,0,3,1}, // 2:Player, 3:Enemy
                {1,0,4,0,0,1}, // 4:Cracker
                {1,0,0,0,0,1},
                {1,1,1,1,1,1}
            };
            
            MapManager.Instance.InitLevel(map);

            // 2. 生成实体 (需要在 EntityGroup 中配置好 "Jelly" 组)
            // 这里的 EntityId 100 是玩家, 200 是敌人
            MapManager.Instance.AddEntity(100, 0, 2, 2); 
            GameEntry.Entity.ShowEntity(100, typeof(JellyLogic), "Assets/GameMain/Entities/Jelly.prefab", "JellyGroup", MapManager.Instance.m_Entities[100]);

            MapManager.Instance.AddEntity(200, 1, 4, 2);
            GameEntry.Entity.ShowEntity(200, typeof(JellyLogic), "Assets/GameMain/Entities/Jelly.prefab", "JellyGroup", MapManager.Instance.m_Entities[200]);

            // 订阅事件
            GameEntry.Event.Subscribe(JellyMoveCompleteEventArgs.EventId, OnJellyMoveComplete);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_IsAnimating || !m_IsPlayerTurn) return;

            HandleInput();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            GameEntry.Event.Unsubscribe(JellyMoveCompleteEventArgs.EventId, OnJellyMoveComplete);
            // 调用MapManager的清理方法
            MapManager.Instance.Cleanup();
            base.OnLeave(procedureOwner, isShutdown);
        }
        
        /// <summary>
        /// 清理地图可视化资源
        /// </summary>
        private void CleanupMapVisuals()
        {
            // 这里需要在MapManager中添加清理方法
            // 暂时不做具体实现，等待MapManager补充清理方法
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                m_TouchStartPos = Input.mousePosition;
                m_IsTouching = true;
            }
            else if (Input.GetMouseButtonUp(0) && m_IsTouching)
            {
                m_IsTouching = false;
                Vector2 delta = (Vector2)Input.mousePosition - m_TouchStartPos;
                
                if (delta.magnitude > 50) // 滑动阈值
                {
                    int dx = 0, dy = 0;
                    if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) dx = delta.x > 0 ? 1 : -1;
                    else dy = delta.y > 0 ? 1 : -1;

                    ProcessTurn(100, dx, dy); // 玩家 ID 100 移动
                }
            }
        }

        // 核心回合逻辑：执行移动并更新数据
        private void ProcessTurn(int entityId, int dx, int dy)
        {
            m_IsAnimating = true;
            var jellyData = MapManager.Instance.m_Entities[entityId];
            
            // 1. 逻辑计算
            var result = MapManager.Instance.CalculateSlide(jellyData.X, jellyData.Y, dx, dy, entityId);
            
            // 2. 更新数据
            jellyData.X = result.FinalX;
            jellyData.Y = result.FinalY;

            // 3. 视觉表现 (查找 EntityLogic)
            UnityGameFramework.Runtime.Entity logic = GameEntry.Entity.GetEntity(entityId);
            if (logic != null && logic.Logic is JellyLogic jelly)
            {
                jelly.ExecuteMove(result.FinalX, result.FinalY, () => {
                    // 撞击回调逻辑
                    if (result.HitEntityId != -1)
                    {
                        // 简单的撞击处理：扣血
                        MapManager.Instance.ApplyDamage(result.HitEntityId, 1);
                        var victim = GameEntry.Entity.GetEntity(result.HitEntityId)?.Logic as JellyLogic;
                        victim?.PlayHitAnimation();
                    }
                    if (result.HitWallType == 4)
                    {
                        // 破坏墙壁逻辑 (这里简化，只是Log)
                        Log.Info("Cracker Wall Broken!");
                        // 实际应播放特效并移除墙壁实体
                    }
                });
            }
        }

        private void OnJellyMoveComplete(object sender, GameEventArgs e)
        {
            // 动画结束，解除锁定
            m_IsAnimating = false;
            
            // 如果是玩家回合结束，切换到敌人回合
            if (m_IsPlayerTurn)
            {
                m_IsPlayerTurn = false;
                // 延迟 0.5s 执行敌人 AI
                GameEntry.Base.StartCoroutine(EnemyTurnCoroutine());
            }
            // 敌人移动完成后，不立即切换回合，而是在EnemyTurnCoroutine中处理
        }
        
        private System.Collections.IEnumerator EnemyTurnCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            
            // 获取所有敌人和玩家实体
            var enemies = new System.Collections.Generic.List<JellyData>();
            var players = new System.Collections.Generic.List<JellyData>();
            
            foreach (var entityData in MapManager.Instance.m_Entities.Values)
            {
                if (entityData.Type == 0) // 0: Player
                {
                    players.Add(entityData);
                }
                else if (entityData.Type == 1) // 1: Enemy
                {
                    enemies.Add(entityData);
                }
            }
            
            // 无玩家则不执行
            if (players.Count == 0) 
            {
                m_IsPlayerTurn = true;
                yield break;
            }
            
            // 对每个敌人执行AI逻辑
            foreach (var enemy in enemies)
            {
                // 寻找最近的玩家目标
                JellyData target = FindNearestPlayer(enemy, players);
                if (target == null)
                {
                    continue;
                }
                
                // 计算移动方向：优先选择距离差更大的轴
                int dx = 0, dy = 0;
                int distX = Mathf.Abs(target.X - enemy.X);
                int distY = Mathf.Abs(target.Y - enemy.Y);
                
                if (distX > distY)
                {
                    // 优先X轴移动
                    dx = target.X > enemy.X ? 1 : -1;
                }
                else
                {
                    // 优先Y轴移动
                    dy = target.Y > enemy.Y ? 1 : -1;
                }
                
                // 执行敌人移动
                ProcessTurn(enemy.Id, dx, dy);
                
                // 等待移动完成
                while (m_IsAnimating)
                {
                    yield return null;
                }
                
                // 敌人移动之间的延迟
                yield return new WaitForSeconds(0.2f);
                
                // 检查游戏状态（是否所有敌人都被消灭）
                if (!CheckEnemyExists())
                {
                    break;
                }
            }
            
            // 全部敌人行动完成后，检查游戏状态
            CheckGameState();
        }
        
        /// <summary>
        /// 查找最近的玩家实体
        /// </summary>
        private JellyData FindNearestPlayer(JellyData enemy, System.Collections.Generic.List<JellyData> players)
        {
            JellyData nearestPlayer = null;
            int minDistance = int.MaxValue;
            
            foreach (var player in players)
            {
                // 计算曼哈顿距离
                int distance = Mathf.Abs(player.X - enemy.X) + Mathf.Abs(player.Y - enemy.Y);
                
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestPlayer = player;
                }
            }
            
            return nearestPlayer;
        }
        
        /// <summary>
        /// 检查是否还有敌人存在
        /// </summary>
        private bool CheckEnemyExists()
        {
            foreach (var entityData in MapManager.Instance.m_Entities.Values)
            {
                if (entityData.Type == 1) // 1: Enemy
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// 检查游戏状态（胜利/失败条件）
        /// </summary>
        private void CheckGameState()
        {
            bool hasEnemies = false;
            bool hasPlayers = false;
            
            foreach (var entityData in MapManager.Instance.m_Entities.Values)
            {
                if (entityData.Type == 0) // 0: Player
                {
                    hasPlayers = true;
                }
                else if (entityData.Type == 1) // 1: Enemy
                {
                    hasEnemies = true;
                }
            }
            
            // 胜利条件：所有敌人被消灭
            if (!hasEnemies && hasPlayers)
            {
                Log.Info("Game Won! All enemies defeated.");
                // TODO: 触发胜利事件/UI
            }
            // 失败条件：所有玩家被消灭
            else if (!hasPlayers)
            {
                Log.Info("Game Over! All players defeated.");
                // TODO: 触发失败事件/UI
            }
            
            // 无论胜利失败，都将回合交回玩家或重置状态
            m_IsPlayerTurn = true;
        }
    }
}