using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;
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

            // 2. 生成实体 (需要在 EntityGroup 中配置好 "JellyGroup" 组)
            // 这里的 EntityId 100 是玩家, 200 是敌人
            MapManager.Instance.AddEntity(100, 0, 2, 2); 
            // 使用正确的资源路径格式
            GameEntry.Entity.ShowEntity(100, typeof(JellyLogic), AssetUtility.GetEntityAsset("Jelly"), "JellyGroup", MapManager.Instance.m_Entities[100]);

            MapManager.Instance.AddEntity(200, 1, 4, 2);
            GameEntry.Entity.ShowEntity(200, typeof(JellyLogic), AssetUtility.GetEntityAsset("Jelly"), "JellyGroup", MapManager.Instance.m_Entities[200]);

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
            base.OnLeave(procedureOwner, isShutdown);
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
            // 动画结束，解除锁定，或者切换到敌人回合
            m_IsAnimating = false;
            
            // 简单的回合切换 Demo
            if (m_IsPlayerTurn)
            {
                m_IsPlayerTurn = false;
                // 延迟 0.5s 执行敌人 AI
                GameEntry.Base.StartCoroutine(EnemyTurnCoroutine());
            }
            else
            {
                m_IsPlayerTurn = true;
            }
        }
        
        private System.Collections.IEnumerator EnemyTurnCoroutine()
        {
            yield return new WaitForSeconds(0.5f);
            // 极其简单的 AI：向左撞
            ProcessTurn(200, -1, 0); 
        }
    }
}