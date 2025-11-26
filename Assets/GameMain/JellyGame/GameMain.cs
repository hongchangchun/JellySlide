using UnityEngine;
using UnityGameFramework.Runtime;
using System.Collections.Generic;

namespace StarForce
{
    /// <summary>
    /// 游戏主入口类
    /// 负责初始化所有系统并管理游戏流程
    /// </summary>
    public class GameMain : MonoBehaviour
    {
        // 单例实例
        private static GameMain _instance;
        public static GameMain Instance => _instance;
        
        // 关卡数据配置
        [SerializeField]
        private TextAsset _levelConfigJson;
        
        // 当前关卡ID
        private int _currentLevelId = 1;
        
        // 游戏状态 - 使用GameConstants中的GameState枚举
        private GameConstants.GameState _gameState = GameConstants.GameState.Initializing;
        
        // 预定义的第一关地图数据（作为备用）
        private int[,] _defaultMapData = new int[,]
        {
            {1, 1, 1, 1, 1, 1},
            {1, 0, 0, 0, 0, 1},
            {1, 0, 0, 0, 1, 1},
            {1, 1, 0, 0, 0, 1},
            {1, 0, 0, 0, 0, 1},
            {1, 1, 1, 1, 1, 1}
        };
        
        private void Awake()
        {   
            // 单例检查
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            // 初始化游戏框架
            InitializeGameFramework();
            
            // 初始化核心系统
            InitializeCoreSystems();
            
            // 订阅事件
            SubscribeEvents();
        }
        
        private void Start()
        {   
            // 启动游戏
            StartGame();
        }
        
        private void OnDestroy()
        {   
            // 取消订阅
            UnsubscribeEvents();
        }
        
        /// <summary>
        /// 初始化游戏框架
        /// </summary>
        private void InitializeGameFramework()
        {   
            Log.Info("初始化游戏框架...");
            // 确保GameEntry可用
            if (GameEntry.Instance == null)
            {
                Log.Error("GameEntry未找到！请确保游戏框架已正确初始化。");
                return;
            }
        }
        
        /// <summary>
        /// 初始化核心系统
        /// </summary>
        private void InitializeCoreSystems()
        {   
            Log.Info("初始化核心系统...");
            
            // 初始化地图管理器
            MapManager.Instance.InitLevel(_defaultMapData);
            
            // 初始化特效管理器
            if (EffectsManager.Instance == null)
            {
                GameObject obj = new GameObject("EffectsManager");
                obj.AddComponent<EffectsManager>();
            }
            
            // 初始化伤害反馈管理器
            if (DamageFeedbackManager.Instance == null)
            {
                GameObject obj = new GameObject("DamageFeedbackManager");
                obj.AddComponent<DamageFeedbackManager>();
            }
            
            // 初始化敌人AI管理器
            if (EnemyAI.Instance == null)
            {
                GameObject obj = new GameObject("EnemyAI");
                obj.AddComponent<EnemyAI>();
            }
            
            // 初始化UI管理器
            if (UIManager.Instance == null)
            {
                GameObject obj = new GameObject("UIManager");
                obj.AddComponent<UIManager>();
            }
        }
        
        /// <summary>
        /// 订阅事件
        /// </summary>
        private void SubscribeEvents()
        {   
            Log.Info("订阅事件...");
            
            // 订阅游戏状态事件 - 使用GameConstants中的事件ID
            GameEntry.Event.Subscribe(GameConstants.GAME_STATE_CHANGED_EVENT, OnGameStateChanged);
            
            // 订阅关卡事件 - 使用GameConstants中的事件ID
            GameEntry.Event.Subscribe(GameConstants.LEVEL_WIN_EVENT, OnLevelWin);
            GameEntry.Event.Subscribe(GameConstants.LEVEL_LOSE_EVENT, OnLevelLose);
        }
        
        /// <summary>
        /// 取消订阅事件
        /// </summary>
        private void UnsubscribeEvents()
        {   
            if (GameEntry.Event != null)
            {
                GameEntry.Event.Unsubscribe(GameConstants.GAME_STATE_CHANGED_EVENT, OnGameStateChanged);
            GameEntry.Event.Unsubscribe(GameConstants.LEVEL_WIN_EVENT, OnLevelWin);
            GameEntry.Event.Unsubscribe(GameConstants.LEVEL_LOSE_EVENT, OnLevelLose);
            }
        }
        
        /// <summary>
        /// 启动游戏
        /// </summary>
        public void StartGame()
        {   
            Log.Info("启动游戏...");
            
            // 加载第一关
            LoadLevel(_currentLevelId);
            
            // 更新游戏状态
            ChangeGameState(GameConstants.GameState.Ready);
        }
        
        /// <summary>
        /// 加载关卡
        /// </summary>
        public void LoadLevel(int levelId)
        {   
            Log.Info($"加载关卡 {levelId}");
            
            // 设置当前关卡ID
            _currentLevelId = levelId;
            
            // 重置游戏状态
            _gameState = GameConstants.GameState.Loading;
            
            // 清理当前场景
            ClearCurrentScene();
            
            // 获取关卡数据
            int[,] mapData = GetLevelMapData(levelId);
            
            // 初始化地图
            MapManager.Instance.InitLevel(mapData);
            
            // 生成玩家和敌人
            SpawnEntities();
            
            // 初始化玩家回合
            StartPlayerTurn();
        }
        
        /// <summary>
        /// 获取关卡地图数据
        /// </summary>
        private int[,] GetLevelMapData(int levelId)
        {   
            // TODO: 从JSON配置中读取关卡数据
            // 暂时返回默认地图数据
            return _defaultMapData;
        }
        
        /// <summary>
        /// 清理当前场景
        /// </summary>
        private void ClearCurrentScene()
        {   
            // 重置敌人AI
            EnemyAI.Instance.Reset();
            
            // 清理地图
            MapManager.Instance.InitLevel(_defaultMapData);
            
            // 清理UI
            UIManager.Instance.ResetUI();
        }
        
        /// <summary>
        /// 生成实体
        /// </summary>
        private void SpawnEntities()
        {   
            Log.Info("生成游戏实体...");
            
            // 生成玩家（暂时放在固定位置）
            MapManager.Instance.AddEntity(1, 0, 1, 1); // 玩家ID=1，类型=0，位置(1,1)
            
            // 生成敌人（暂时放在固定位置）
            MapManager.Instance.AddEntity(101, 1, 4, 4); // 敌人ID=101，类型=1，位置(4,4)
            MapManager.Instance.AddEntity(102, 1, 4, 2); // 敌人ID=102，类型=1，位置(4,2)
        }
        
        /// <summary>
        /// 开始玩家回合
        /// </summary>
        private void StartPlayerTurn()
        {   
            Log.Info("开始玩家回合");
            
            // 更新游戏状态
            ChangeGameState(GameConstants.GameState.PlayerTurn);
        }
        
        /// <summary>
        /// 开始敌人回合
        /// </summary>
        public void StartEnemyTurn()
        {   
            Log.Info("开始敌人回合");
            
            // 更新游戏状态
            ChangeGameState(GameConstants.GameState.EnemyTurn);
            
            // 延迟执行敌人AI行动
            StartCoroutine(ExecuteEnemyActions());
        }
        
        /// <summary>
        /// 执行敌人行动
        /// </summary>
        private System.Collections.IEnumerator ExecuteEnemyActions()
        {   
            // 等待敌人AI执行完毕
            yield return new WaitForSeconds(1.5f);
            
            // 检查游戏状态
            CheckGameStatus();
            
            // 检查游戏状态
            if (_gameState == GameConstants.GameState.EnemyTurn)
            {
                StartPlayerTurn();
            }
        }
        
        /// <summary>
        /// 检查游戏状态
        /// </summary>
        private void CheckGameStatus()
        {   
            // 检查胜利条件
            if (MapManager.Instance.CheckWinCondition())
            {
                Log.Info("胜利条件满足！");
                OnLevelWinInternal();
            }
            // 检查失败条件
            else if (MapManager.Instance.CheckLoseCondition())
            {
                Log.Info("失败条件满足！");
                OnLevelLoseInternal();
            }
        }
        
        /// <summary>
        /// 内部处理关卡胜利
        /// </summary>
        private void OnLevelWinInternal()
        {   
            // 更新游戏状态
            ChangeGameState(GameConstants.GameState.Win);
            
            // 触发胜利事件
            GameEntry.Event.Fire(this, LevelWinEventArgs.Create(_currentLevelId));
        }
        
        /// <summary>
        /// 内部处理关卡失败
        /// </summary>
        private void OnLevelLoseInternal()
        {   
            // 更新游戏状态
            ChangeGameState(GameConstants.GameState.Lose);
            
            // 触发失败事件
            GameEntry.Event.Fire(this, LevelLoseEventArgs.Create(_currentLevelId));
        }
        
        /// <summary>
        /// 重置关卡
        /// </summary>
        public void ResetLevel()
        {   
            Log.Info("重置当前关卡");
            LoadLevel(_currentLevelId);
        }
        
        /// <summary>
        /// 进入下一关
        /// </summary>
        public void NextLevel()
        {   
            Log.Info("进入下一关");
            LoadLevel(_currentLevelId + 1);
        }
        
        /// <summary>
        /// 重新开始游戏
        /// </summary>
        public void RestartGame()
        {   
            Log.Info("重新开始游戏");
            _currentLevelId = 1;
            LoadLevel(_currentLevelId);
        }
        
        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {   
            Log.Info("退出游戏");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        /// <summary>
        /// 改变游戏状态
        /// </summary>
        private void ChangeGameState(GameConstants.GameState newState)
        {   
            if (_gameState == newState) return;
            
            GameConstants.GameState oldState = _gameState;
            _gameState = newState;
            
            Log.Info($"游戏状态变化: {oldState} -> {newState}");
            
            // 触发游戏状态变化事件
            GameEntry.Event.Fire(this, GameStateChangedEventArgs.Create((int)newState, (int)oldState));
            Log.Info($"游戏状态从 {oldState} 变为 {newState}");
        }
        
        /// <summary>
        /// 游戏状态变化事件处理
        /// </summary>
        private void OnGameStateChanged(object sender, GameEventArgs e)
        {
            GameStateChangedEventArgs args = (GameStateChangedEventArgs)e;
            GameConstants.GameState newState = (GameConstants.GameState)args.NewState;
            Log.Info($"游戏状态已变为: {newState}");
            
            // 根据不同的游戏状态执行不同的逻辑
            switch (newState)
            {
                case GameConstants.GameState.Win:
                    // 胜利逻辑
                    break;
                case GameConstants.GameState.Lose:
                    // 失败逻辑
                    break;
            }
        }
        
        /// <summary>
        /// 关卡胜利事件处理
        /// </summary>
        private void OnLevelWin(object sender, GameEventArgs e)
        {   
            LevelWinEventArgs args = (LevelWinEventArgs)e;
            Log.Info($"关卡 {args.LevelId} 胜利！");
        }
        
        /// <summary>
        /// 关卡失败事件处理
        /// </summary>
        private void OnLevelLose(object sender, GameEventArgs e)
        {   
            LevelLoseEventArgs args = (LevelLoseEventArgs)e;
            Log.Info($"关卡 {args.LevelId} 失败！");
        }
        
        /// <summary>
        /// 获取当前游戏状态
        /// </summary>
        public GameConstants.GameState GetCurrentState()
        {   
            return _gameState;
        }
        
        /// <summary>
        /// 获取当前关卡ID
        /// </summary>
        public int GetCurrentLevelId()
        {   
            return _currentLevelId;
        }
    }
    
    // 游戏状态枚举已移至GameConstants.cs
}
