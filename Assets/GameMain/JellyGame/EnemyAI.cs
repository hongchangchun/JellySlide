using UnityEngine;
using GameFramework;
using System.Collections.Generic;
using System;

namespace JellyGame
{
    /// <summary>
    /// 敌人AI管理器
    /// 负责控制所有敌人的行为模式和决策逻辑
    /// </summary>
    public class EnemyAI : MonoBehaviour
    {
        private static EnemyAI _instance;
        public static EnemyAI Instance
        {
            get
            {   
                if (_instance == null)
                {   
                    GameObject obj = new GameObject("EnemyAI");
                    _instance = obj.AddComponent<EnemyAI>();
                    DontDestroyOnLoad(obj);
                }
                return _instance;
            }
        }
        
        // 敌人类实体ID映射
        private Dictionary<int, EnemyController> _enemyControllers = new Dictionary<int, EnemyController>();
        
        // 决策间隔时间（秒）
        [SerializeField]
        private float _decisionInterval = 1.5f;
        
        // 检测范围
        [SerializeField]
        private int _detectionRange = 5;
        
        // 移动方向优先级
        private readonly Vector2Int[] _movePriorities = new Vector2Int[]
        {
            new Vector2Int(0, 1),  // 上
            new Vector2Int(1, 0),  // 右
            new Vector2Int(0, -1), // 下
            new Vector2Int(-1, 0)  // 左
        };
        
        private void Awake()
        {   
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // 订阅事件
            SubscribeEvents();
        }
        
        private void OnDestroy()
        {   
            // 取消订阅
            UnsubscribeEvents();
        }
        
        private void SubscribeEvents()
        {   
            GameEntry.Event.Subscribe(JellyLogic.GENERATE_ENTITY_EVENT, OnGenerateEntity);
            GameEntry.Event.Subscribe(JellyLogic.DESTROY_ENTITY_EVENT, OnDestroyEntity);
            GameEntry.Event.Subscribe(JellyLogic.ENTITY_MOVED_EVENT, OnEntityMoved);
        }
        
        private void UnsubscribeEvents()
        {   
            GameEntry.Event.Unsubscribe(JellyLogic.GENERATE_ENTITY_EVENT, OnGenerateEntity);
            GameEntry.Event.Unsubscribe(JellyLogic.DESTROY_ENTITY_EVENT, OnDestroyEntity);
            GameEntry.Event.Unsubscribe(JellyLogic.ENTITY_MOVED_EVENT, OnEntityMoved);
        }
        
        private void OnGenerateEntity(object sender, GameEventArgs e)
        {   
            GenerateEntityEventArgs args = (GenerateEntityEventArgs)e;
            if (args.EntityType == JellyLogic.ENTITY_TYPE_ENEMY)
            {
                // 创建敌人控制器并添加到字典
                EnemyController controller = new EnemyController(args.EntityId, args.Position);
                _enemyControllers.Add(args.EntityId, controller);
                
                // 启动敌人AI协程
                StartCoroutine(EnemyAIBehavior(args.EntityId));
            }
        }
        
        private void OnDestroyEntity(object sender, GameEventArgs e)
        {   
            DestroyEntityEventArgs args = (DestroyEntityEventArgs)e;
            if (_enemyControllers.ContainsKey(args.EntityId))
            {
                _enemyControllers.Remove(args.EntityId);
            }
        }
        
        private void OnEntityMoved(object sender, GameEventArgs e)
        {   
            EntityMovedEventArgs args = (EntityMovedEventArgs)e;
            // 如果移动的是敌人，更新其位置
            if (_enemyControllers.ContainsKey(args.EntityId))
            {
                _enemyControllers[args.EntityId].UpdatePosition(args.NewPosition);
            }
        }
        
        /// <summary>
        /// 敌人AI行为协程
        /// 每个敌人独立的思考和行动流程
        /// </summary>
        private System.Collections.IEnumerator EnemyAIBehavior(int entityId)
        {   
            while (_enemyControllers.ContainsKey(entityId))
            {   
                EnemyController controller = _enemyControllers[entityId];
                
                // 检查敌人是否还存活
                if (!JellyMap.Instance.IsEntityActive(entityId))
                {   
                    break;
                }
                
                // 寻找最近的玩家
                Vector2Int? targetPos = FindNearestPlayer(controller.CurrentPosition, _detectionRange);
                
                // 做出移动决策
                if (targetPos.HasValue)
                {   
                    // 有目标：尝试向目标移动
                    MoveTowardsTarget(entityId, controller.CurrentPosition, targetPos.Value);
                }
                else
                {   
                    // 无目标：随机移动或跟随优先级方向
                    RandomMove(entityId, controller.CurrentPosition);
                }
                
                // 等待下一次决策
                yield return new WaitForSeconds(_decisionInterval);
            }
        }
        
        /// <summary>
        /// 寻找最近的玩家
        /// </summary>
        private Vector2Int? FindNearestPlayer(Vector2Int enemyPos, int maxRange)
        {   
            List<Vector2Int> playerPositions = JellyMap.Instance.GetPlayerPositions();
            Vector2Int? nearestPlayer = null;
            float minDistance = float.MaxValue;
            
            foreach (Vector2Int playerPos in playerPositions)
            {   
                float distance = Vector2.Distance(enemyPos, playerPos);
                if (distance <= maxRange && distance < minDistance)
                {   
                    minDistance = distance;
                    nearestPlayer = playerPos;
                }
            }
            
            return nearestPlayer;
        }
        
        /// <summary>
        /// 向目标移动
        /// </summary>
        private void MoveTowardsTarget(int entityId, Vector2Int currentPos, Vector2Int targetPos)
        {   
            // 计算方向向量
            Vector2Int direction = Vector2Int.zero;
            
            // 优先水平方向移动
            if (Math.Abs(targetPos.x - currentPos.x) > Math.Abs(targetPos.y - currentPos.y))
            {   
                direction.x = targetPos.x > currentPos.x ? 1 : -1;
            }
            else
            {   
                direction.y = targetPos.y > currentPos.y ? 1 : -1;
            }
            
            // 尝试移动
            if (CanMove(entityId, currentPos, direction))
            {   
                ExecuteMove(entityId, direction);
            }
            else
            {   
                // 如果主方向不可移动，尝试备选方向
                TryAlternativeMoves(entityId, currentPos);
            }
        }
        
        /// <summary>
        /// 随机移动
        /// </summary>
        private void RandomMove(int entityId, Vector2Int currentPos)
        {   
            // 打乱移动优先级
            List<Vector2Int> shuffledDirections = new List<Vector2Int>(_movePriorities);
            ShuffleList(shuffledDirections);
            
            // 尝试移动
            foreach (Vector2Int direction in shuffledDirections)
            {   
                if (CanMove(entityId, currentPos, direction))
                {   
                    ExecuteMove(entityId, direction);
                    return;
                }
            }
        }
        
        /// <summary>
        /// 尝试备选移动方向
        /// </summary>
        private void TryAlternativeMoves(int entityId, Vector2Int currentPos)
        {   
            foreach (Vector2Int direction in _movePriorities)
            {   
                if (CanMove(entityId, currentPos, direction))
                {   
                    ExecuteMove(entityId, direction);
                    return;
                }
            }
        }
        
        /// <summary>
        /// 检查是否可以移动
        /// </summary>
        private bool CanMove(int entityId, Vector2Int currentPos, Vector2Int direction)
        {   
            Vector2Int newPos = currentPos + direction;
            
            // 检查移动是否合法
            return JellyMap.Instance.CanEntityMove(entityId, newPos);
        }
        
        /// <summary>
        /// 执行移动
        /// </summary>
        private void ExecuteMove(int entityId, Vector2Int direction)
        {   
            // 触发移动事件
            MoveEntityEventArgs args = MoveEntityEventArgs.Create(entityId, direction);
            GameEntry.Event.Fire(this, args);
            ReferencePool.Release(args);
        }
        
        /// <summary>
        /// 打乱列表
        /// </summary>
        private void ShuffleList<T>(List<T> list)
        {   
            int n = list.Count;
            while (n > 1)
            {   
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        
        /// <summary>
        /// 重置敌人AI系统
        /// </summary>
        public void Reset()
        {   
            _enemyControllers.Clear();
            StopAllCoroutines();
        }
    }
    
    /// <summary>
    /// 敌人控制器
    /// 管理单个敌人的状态和位置
    /// </summary>
    internal class EnemyController
    {
        public int EntityId { get; private set; }
        public Vector2Int CurrentPosition { get; private set; }
        
        public EnemyController(int entityId, Vector2Int initialPosition)
        {   
            EntityId = entityId;
            CurrentPosition = initialPosition;
        }
        
        public void UpdatePosition(Vector2Int newPosition)
        {   
            CurrentPosition = newPosition;
        }
    }
}
