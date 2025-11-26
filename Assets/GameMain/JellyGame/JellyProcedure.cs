using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;
using ProcedureOwner = GameFramework.Fsm.IFsm<GameFramework.Procedure.IProcedureManager>;
using System.Collections.Generic;
using System.Collections;

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
        
        // 地图数据
        private int[,] m_MapData = new int[6, 6] {
            {0, 0, 0, 0, 0, 0},
            {0, 1, 4, 1, 0, 0},
            {0, 0, 0, 0, 0, 0},
            {0, 4, 1, 4, 0, 0},
            {0, 0, 0, 9, 0, 0}, // 添加一个陷阱在(3,4)位置
            {0, 0, 0, 0, 0, 0}
        };
        
        // 当前玩家和敌人实体ID列表
        private List<int> m_PlayerEntityIds = new List<int>();
        private List<int> m_EnemyEntityIds = new List<int>();
        
        // 回合状态
        private bool m_IsPlayerTurn = true;
        private bool m_IsMoving = false;
        private int m_MovingEntityCount = 0;
        private int m_TotalMovingEntities = 0;

        // 当前关卡ID
        private int m_CurrentLevel = 1;
        // 游戏是否暂停
        private bool m_IsPaused = false;
        // 使用GameConstants中的GameState枚举
        private GameConstants.GameState m_GameState = GameConstants.GameState.Ready;
        
        protected override void OnEnter(ProcedureOwner procedureOwner)
        {
            base.OnEnter(procedureOwner);
            
            // 初始化游戏状态
            m_GameState = GameConstants.GameState.Ready;
            m_CurrentLevel = 1;
            
            // 加载并初始化当前关卡
            LoadLevel(m_CurrentLevel);
            
            // 注册事件监听
            GameEntry.Event.Subscribe(JellyMoveCompleteEventArgs.EventId, OnJellyMoveComplete);
            GameEntry.Event.Subscribe(JellyKilledEventArgs.EventId, OnJellyKilled);
            GameEntry.Event.Subscribe(DamageEventArgs.EventId, OnDamage);
            GameEntry.Event.Subscribe(LevelWinEventArgs.EventId, OnLevelWin);
            GameEntry.Event.Subscribe(LevelLoseEventArgs.EventId, OnLevelLose);
        }

        protected override void OnUpdate(ProcedureOwner procedureOwner, float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(procedureOwner, elapseSeconds, realElapseSeconds);

            if (m_IsMoving || !m_IsPlayerTurn || m_GameState == GameConstants.GameState.GameOver || m_IsPaused) return;

            HandleInput();
        }

        protected override void OnLeave(ProcedureOwner procedureOwner, bool isShutdown)
        {
            // 取消事件订阅
            GameEntry.Event.Unsubscribe(JellyMoveCompleteEventArgs.EventId, OnJellyMoveComplete);
            GameEntry.Event.Unsubscribe(JellyKilledEventArgs.EventId, OnJellyKilled);
            GameEntry.Event.Unsubscribe(DamageEventArgs.EventId, OnDamage);
            GameEntry.Event.Unsubscribe(LevelWinEventArgs.EventId, OnLevelWin);
            GameEntry.Event.Unsubscribe(LevelLoseEventArgs.EventId, OnLevelLose);
            
            // 清理实体
            ClearCurrentLevel();
            
            base.OnLeave(procedureOwner, isShutdown);
        }
        
        /// <summary>
        /// 加载关卡
        /// </summary>
        /// <param name="levelId">关卡ID</param>
        private void LoadLevel(int levelId)
        {            
            Log.Info($"加载关卡: {levelId}");
            
            // 清空当前场景
            ClearCurrentLevel();
            
            // 初始化地图
            MapManager.Instance.InitLevel(m_MapData);
            
            // 生成实体
            SpawnEntities();
            
            // 设置游戏状态为准备就绪
            m_GameState = GameConstants.GameState.Ready;
            
            // 重置敌人计数
            m_EnemiesKilled = 0;
            
            // 记录关卡开始时间
            m_LevelStartTime = Time.time;
            
            // 延迟一帧后开始玩家回合
            StartCoroutine(StartPlayerTurnAfterDelay());
            
            // 更新UI关卡信息
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateLevelInfo(levelId);
            }
        }
        
        /// <summary>
        /// 延迟开始玩家回合
        /// </summary>
        private System.Collections.IEnumerator StartPlayerTurnAfterDelay()
        {   
            yield return new WaitForSeconds(0.5f);
            m_IsPlayerTurn = true;
        }
        
        /// <summary>
        /// 清空当前关卡内容
        /// </summary>
        private void ClearCurrentLevel()
        {   
            // 清理实体
            foreach (int entityId in m_PlayerEntityIds)
            {
                GameEntry.Entity.HideEntity(entityId);
            }
            foreach (int entityId in m_EnemyEntityIds)
            {
                GameEntry.Entity.HideEntity(entityId);
            }
            
            m_PlayerEntityIds.Clear();
            m_EnemyEntityIds.Clear();
            
            // 重置地图管理器
            MapManager.Instance.Reset();
        }
        
        /// <summary>
        /// 重置当前关卡
        /// </summary>
        public void ResetLevel()
        {   
            Log.Info("重置关卡");
            
            // 重置UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ResetUI();
            }
            
            LoadLevel(m_CurrentLevel);
        }
        
        /// <summary>
        /// 进入下一关
        /// </summary>
        public void NextLevel()
        {   
            m_CurrentLevel++;
            Log.Info($"进入下一关: {m_CurrentLevel}");
            LoadLevel(m_CurrentLevel);
        }
        
        // 游戏状态数据
        private int m_EnemiesKilled = 0;
        private float m_LevelStartTime = 0;
        
        private void OnLevelWin(object sender, GameEventArgs e)
        {    
            LevelWinEventArgs winArgs = (LevelWinEventArgs)e;
            Log.Info($"关卡 {winArgs.LevelId} 胜利！所有敌人都被消灭了！");
            
            // 设置游戏状态为结束
            m_GameState = GameConstants.GameState.GameOver;
            
            // 计算游戏时间
            float timeUsed = Time.time - m_LevelStartTime;
            
            // 创建胜利数据
            var victoryData = VictoryPanelUI.VictoryData.Calculate(m_EnemiesKilled, timeUsed);
            
            // 显示胜利UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowVictoryPanel(winArgs.LevelId, victoryData);
            }
        }
        
        private void OnLevelLose(object sender, GameEventArgs e)
        {    
            LevelLoseEventArgs loseArgs = (LevelLoseEventArgs)e;
            Log.Info($"关卡 {loseArgs.LevelId} 失败！所有玩家都被消灭了！");
            
            // 设置游戏状态为结束
            m_GameState = GameConstants.GameState.GameOver;
            
            // 计算游戏时间
            float timeUsed = Time.time - m_LevelStartTime;
            
            // 创建失败数据
            var failureData = DefeatPanelUI.FailureData.Calculate(
                DefeatPanelUI.FailureReason.PlayerDied,
                m_EnemiesKilled,
                timeUsed
            );
            
            // 显示失败UI
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowDefeatPanel(failureData);
            }
        }

        private void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                TouchManager.Instance.StartTouch(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                TouchManager.TouchResult result = TouchManager.Instance.EndTouch(Input.mousePosition);
                if (result.IsValid)
                {
                    // 执行所有玩家实体的移动
                    ExecutePlayerSlide(result.Direction);
                }
            }
        }

        /// <summary>
        /// 执行玩家滑动操作
        /// </summary>
        private void ExecutePlayerSlide(Vector2Int direction)
        {
            if (m_IsMoving || !m_IsPlayerTurn) return;
            
            m_MovingEntityCount = 0;
            m_TotalMovingEntities = 0;
            
            foreach (int playerId in m_PlayerEntityIds)
            {
                if (!MapManager.Instance.m_Entities.TryGetValue(playerId, out MapManager.JellyData playerData) || playerData.IsDead)
                    continue;
                
                m_TotalMovingEntities++;
                
                // 计算滑动结果
                SlideResult result = MapManager.Instance.CalculateSlide(playerData.X, playerData.Y, direction.x, direction.y, playerId);
                
                // 执行移动
                ExecuteMove(playerId, direction.x, direction.y, result);
            }
            
            if (m_TotalMovingEntities > 0)
                m_IsMoving = true;
        }

        /// <summary>
        /// 生成初始实体
        /// </summary>
        private void SpawnEntities()
        {
            // 生成玩家
            m_PlayerEntityIds.Add(SpawnEntity("Jelly", 0, 1001, 0, 0));
            
            // 生成敌人
            m_EnemyEntityIds.Add(SpawnEntity("Jelly", 1, 2001, 5, 5));
            m_EnemyEntityIds.Add(SpawnEntity("Jelly", 1, 2002, 4, 4));
        }
        
        /// <summary>
        /// 生成实体
        /// </summary>
        private int SpawnEntity(string assetName, int entityType, int entityId, int x, int y)
        {
            // 创建实体数据
            JellyData data = new JellyData();
            data.Id = entityId;
            data.Type = entityType;
            data.X = x;
            data.Y = y;
            data.Hp = entityType == 0 ? 3 : 2; // 玩家3血，敌人2血
            
            // 添加到地图管理器
            MapManager.Instance.AddEntity(entityId, entityType, x, y);
            
            // 创建实体
            GameEntry.Entity.ShowEntity(entityId, typeof(JellyLogic), AssetUtility.GetEntityAsset(assetName), "JellyGroup", data);
            return entityId;
        }

        /// <summary>
        /// 执行移动
        /// </summary>
        private void ExecuteMove(int entityId, int dirX, int dirY, SlideResult result)
        {
            // 更新地图数据中的位置
            if (MapManager.Instance.m_Entities.TryGetValue(entityId, out MapManager.JellyData entityData))
            {
                entityData.X = result.FinalX;
                entityData.Y = result.FinalY;
            }
            
            // 获取实体并执行移动动画
            UnityGameFramework.Runtime.Entity entity = GameEntry.Entity.GetEntity(entityId);
            if (entity != null && entity.Logic is JellyLogic jellyLogic)
            {
                jellyLogic.ExecuteMove(result.FinalX, result.FinalY, () => {
                    // 移动完成后处理后续逻辑
                    ProcessPostMoveLogic(entityId, result, dirX, dirY);
                    
                    // 增加移动完成计数
                    m_MovingEntityCount++;
                    
                    // 检查是否所有实体都移动完成
                    if (CheckAllMovesComplete())
                    {
                        m_IsMoving = false;
                        
                        // 切换回合
                        SwitchTurn();
                    }
                });
            }
        }
        
        /// <summary>
        /// 处理移动后的逻辑
        /// </summary>
        private void ProcessPostMoveLogic(int entityId, SlideResult result, int dirX, int dirY)
        {
            // 处理陷阱伤害
            if (result.HitTrap)
            {
                HandleTrapDamage(entityId, result.HitTrapX, result.HitTrapY);
            }
            
            // 处理实体碰撞
            if (result.HitEntityId != -1)
            {
                HandleEntityCollision(entityId, result.HitEntityId, dirX, dirY, result);
            }
            
            // 处理墙壁破坏
            if (result.HitWallType == GameConstants.MAP_CELL_CRACKER_WALL)
            {
                MapManager.Instance.BreakWall(result.HitWallX, result.HitWallY);
                Log.Info("Cracker Wall Broken!");
            }
        }
        
        /// <summary>
        /// 处理实体碰撞
        /// </summary>
        private void HandleEntityCollision(int attackerId, int defenderId, int dirX, int dirY, SlideResult slideResult)
        {
            // 使用新的物理碰撞系统处理碰撞
            UnityGameFramework.Runtime.Entity attacker = GameEntry.Entity.GetEntity(attackerId);
            if (attacker != null && attacker.Logic is JellyLogic attackerLogic)
            {
                attackerLogic.HandleCollision(defenderId, dirX, dirY, slideResult);
            }
            
            // 基础伤害应用
            MapManager.Instance.ApplyDamage(defenderId, 1);
            var victim = GameEntry.Entity.GetEntity(defenderId)?.Logic as JellyLogic;
            victim?.PlayHitAnimation();
        }
        
        /// <summary>
        /// 处理陷阱伤害
        /// </summary>
        private void HandleTrapDamage(int entityId, int trapX, int trapY)
        {
            UnityGameFramework.Runtime.Entity entity = GameEntry.Entity.GetEntity(entityId);
            if (entity != null && entity.Logic is JellyLogic jellyLogic)
            {
                jellyLogic.TakeTrapDamage();
            }
            
            // 对踩中陷阱的实体造成伤害
            MapManager.Instance.ApplyDamage(entityId, 2);
        }

        private void OnJellyMoveComplete(object sender, GameEventArgs e)
        {
            // 现在通过移动回调处理移动完成逻辑
        }
        
        private void OnJellyKilled(object sender, GameEventArgs e)
        {            
            JellyKilledEventArgs killArgs = (JellyKilledEventArgs)e;
            int entityId = killArgs.EntityId;
            int entityType = killArgs.EntityType;
            
            // 从相应列表中移除
            if (m_PlayerEntityIds.Contains(entityId))
            {
                m_PlayerEntityIds.Remove(entityId);
            }
            else if (m_EnemyEntityIds.Contains(entityId))
            {
                m_EnemyEntityIds.Remove(entityId);
                m_EnemiesKilled++;
            }
            
            // 检查游戏状态
            CheckGameStatus();
        }
        
        private void OnDamage(object sender, GameEventArgs e)
        {
            DamageEventArgs damageArgs = (DamageEventArgs)e;
            
            // 伤害反馈
            Log.Info($"Entity {damageArgs.EntityId} took {damageArgs.Damage} damage{(damageArgs.IsCrit ? " (CRITICAL!)" : "")}");
        }
        
        private bool CheckAllMovesComplete()
        {
            return m_MovingEntityCount >= m_TotalMovingEntities;
        }
        
        private void SwitchTurn()
        {
            m_IsPlayerTurn = !m_IsPlayerTurn;
            
            // 如果是敌人回合，启动敌人AI
            if (!m_IsPlayerTurn)
            {
                StartCoroutine(EnemyTurnCoroutine());
            }
        }
        
        private System.Collections.IEnumerator EnemyTurnCoroutine()
        {
            // 等待一段时间再执行敌人移动
            yield return new WaitForSeconds(0.5f);
            
            m_MovingEntityCount = 0;
            m_TotalMovingEntities = 0;
            
            // 为每个敌人选择一个方向移动
            foreach (int enemyId in m_EnemyEntityIds)
            {
                if (!MapManager.Instance.m_Entities.TryGetValue(enemyId, out MapManager.JellyData enemyData) || enemyData.IsDead)
                    continue;
                
                m_TotalMovingEntities++;
                
                // 寻找最近的玩家作为目标
                Vector2Int direction = FindNearestPlayerDirection(enemyData.X, enemyData.Y);
                
                // 计算滑动结果
                SlideResult result = MapManager.Instance.CalculateSlide(enemyData.X, enemyData.Y, direction.x, direction.y, enemyId);
                
                // 执行移动
                ExecuteMove(enemyId, direction.x, direction.y, result);
                
                // 等待一小段时间再移动下一个敌人
                yield return new WaitForSeconds(0.3f);
            }
            
            if (m_TotalMovingEntities > 0)
                m_IsMoving = true;
        }
        
        /// <summary>
        /// 寻找最近玩家的方向
        /// </summary>
        private Vector2Int FindNearestPlayerDirection(int x, int y)
        {
            int minDistance = int.MaxValue;
            Vector2Int bestDirection = Vector2Int.right; // 默认向右
            
            foreach (int playerId in m_PlayerEntityIds)
            {
                if (!MapManager.Instance.m_Entities.TryGetValue(playerId, out MapManager.JellyData playerData) || playerData.IsDead)
                    continue;
                
                int distance = Mathf.Abs(x - playerData.X) + Mathf.Abs(y - playerData.Y);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    int dx = playerData.X > x ? 1 : (playerData.X < x ? -1 : 0);
                    int dy = playerData.Y > y ? 1 : (playerData.Y < y ? -1 : 0);
                    
                    // 优先水平方向
                    if (dx != 0) dy = 0;
                    bestDirection = new Vector2Int(dx, dy);
                }
            }
            
            return bestDirection;
        }
        
        /// <summary>
        /// 检查游戏状态
        /// </summary>
        private void CheckGameStatus()
        {
            // 检查胜利条件
            if (m_EnemyEntityIds.Count == 0)
            {
                OnGameWin();
            }
            // 检查失败条件
            else if (m_PlayerEntityIds.Count == 0)
            {
                OnGameLose();
            }
        }
        
        /// <summary>
        /// 游戏胜利处理
        /// </summary>
        private void OnGameWin()
        {
            Log.Info("Game Win!");
            GameEntry.Event.Fire(this, LevelWinEventArgs.Create(1));
        }
        
        /// <summary>
        /// 游戏失败处理
        /// </summary>
        private void OnGameLose()
        {
            Log.Info("Game Over!");
            GameEntry.Event.Fire(this, LevelLoseEventArgs.Create(1));
        }
        
        // 简化版的SlideResult类，实际应该定义在MapManager中
        public class SlideResult
        {
            public int FinalX;
            public int FinalY;
            public int HitEntityId = -1;
            public int HitWallType = 0;
            public int HitWallX = -1;
            public int HitWallY = -1;
            public bool HitTrap = false;
            public int HitTrapX = -1;
            public int HitTrapY = -1;
            public bool HasChainReaction = false;
        }
    }
}