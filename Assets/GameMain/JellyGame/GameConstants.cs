using UnityEngine;

namespace StarForce
{
    /// <summary>
    /// 游戏常量类
    /// 定义所有事件ID、实体类型和游戏常量
    /// </summary>
    public static class GameConstants
    {
        // =======================================
        // 事件ID定义
        // =======================================
        
        // 实体相关事件
        public const int GENERATE_ENTITY_EVENT = 1001;
        public const int DESTROY_ENTITY_EVENT = 1002;
        public const int ENTITY_MOVED_EVENT = 1003;
        public const int ENTITY_HIT_EVENT = 1004;
        
        // 移动相关事件
        public const int MOVE_ENTITY_EVENT = 2001;
        public const int SLIDE_START_EVENT = 2002;
        public const int SLIDE_COMPLETE_EVENT = 2003;
        
        // 伤害相关事件
        public const int DAMAGE_EVENT = 3001;
        public const int DAMAGE_FEEDBACK_EVENT = 3002;
        public const int JELLY_KILLED_EVENT = 3003;
        
        // 地图相关事件
        public const int WALL_BROKEN_EVENT = 4001;
        public const int TRAP_TRIGGERED_EVENT = 4002;
        public const int MAP_LOADED_EVENT = 4003;
        
        // 游戏状态相关事件
        public const int GAME_STATE_CHANGED_EVENT = 5001;
        public const int LEVEL_START_EVENT = 5002;
        public const int LEVEL_COMPLETE_EVENT = 5003;
        public const int LEVEL_WIN_EVENT = 5004;
        public const int LEVEL_LOSE_EVENT = 5005;
        
        // UI相关事件
        public const int UI_SHOW_PANEL_EVENT = 6001;
        public const int UI_HIDE_PANEL_EVENT = 6002;
        public const int UI_UPDATE_EVENT = 6003;
        
        // =======================================
        // 实体类型定义
        // =======================================
        
        public const int ENTITY_TYPE_PLAYER = 0;
        public const int ENTITY_TYPE_ENEMY = 1;
        public const int ENTITY_TYPE_NPC = 2;
        
        // =======================================
        // 地图单元格类型
        // =======================================
        
        public const int MAP_CELL_EMPTY = 0;
        public const int MAP_CELL_WALL = 1;
        public const int MAP_CELL_CRACKER_WALL = 4;
        public const int MAP_CELL_TRAP = 9;
        
        // =======================================
        // 游戏配置常量
        // =======================================
        
        // 移动方向
        public const int DIR_UP = 0;
        public const int DIR_DOWN = 1;
        public const int DIR_LEFT = 2;
        public const int DIR_RIGHT = 3;
        
        // 默认参数
        public const int DEFAULT_PLAYER_HP = 3;
        public const int DEFAULT_ENEMY_HP = 2;
        public const int DEFAULT_DAMAGE = 1;
        public const float DEFAULT_SLIDE_SPEED = 1.0f;
        
        // 特效配置
        public const float HIT_EFFECT_DURATION = 0.5f;
        public const float DEATH_EFFECT_DURATION = 1.0f;
        public const float WALL_BREAK_EFFECT_DURATION = 0.8f;
        public const float TRAP_EFFECT_DURATION = 0.6f;
        
        // UI面板名称
        public const string UI_PANEL_GAME = "GameUI";
        public const string UI_PANEL_VICTORY = "VictoryPanel";
        public const string UI_PANEL_DEFEAT = "DefeatPanel";
        public const string UI_PANEL_MAIN_MENU = "MainMenuPanel";
        public const string UI_PANEL_SETTINGS = "SettingsPanel";
        
        // =======================================
        // 游戏规则常量
        // =======================================
        
        // 暴击率
        public const float CRITICAL_HIT_CHANCE = 0.3f; // 30%
        
        // 陷阱伤害
        public const int TRAP_DAMAGE = 1;
        
        // 移动动画时长
        public const float MOVE_ANIMATION_DURATION = 0.3f;
        
        // AI决策间隔
        public const float ENEMY_DECISION_INTERVAL = 1.5f;
        
        // 检测范围
        public const int ENEMY_DETECTION_RANGE = 5;
        
        // =======================================
        // 游戏状态枚举
        // =======================================
        public enum GameState
        {
            Initializing, // 初始化中
            Loading,      // 加载中
            Ready,        // 准备就绪
            PlayerTurn,   // 玩家回合
            EnemyTurn,    // 敌人回合
            Win,          // 胜利
            Lose,         // 失败
            GameOver      // 游戏结束
        }
    }
}
