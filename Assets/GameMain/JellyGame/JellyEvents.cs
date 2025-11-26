using GameFramework;
using GameFramework.Event;

namespace StarForce
{
    // 当玩家操作滑动时触发
    public class PerformSlideEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(PerformSlideEventArgs).GetHashCode();
        public override int Id => EventId;

        public int DirX { get; private set; }
        public int DirY { get; private set; }

        public static PerformSlideEventArgs Create(int x, int y)
        {
            var e = ReferencePool.Acquire<PerformSlideEventArgs>();
            e.DirX = x;
            e.DirY = y;
            return e;
        }

        public override void Clear() { DirX = 0; DirY = 0; }
    }

    // 当一个果冻完成移动动画时触发
    public class JellyMoveCompleteEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(JellyMoveCompleteEventArgs).GetHashCode();
        public override int Id => EventId;
        
        public int EntityId { get; private set; }

        public static JellyMoveCompleteEventArgs Create(int entityId)
        {
            var e = ReferencePool.Acquire<JellyMoveCompleteEventArgs>();
            e.EntityId = entityId;
            return e;
        }
        public override void Clear() { EntityId = 0; }
    }
    
    /// <summary>
    /// 果冻被击杀事件
    /// </summary>
    public class JellyKilledEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(JellyKilledEventArgs).GetHashCode();
        
        public override int Id => EventId;
        
        public int EntityId { get; private set; }
        public int EntityType { get; private set; }
        
        public static JellyKilledEventArgs Create(int entityId, int entityType)
        {
            JellyKilledEventArgs jellyKilledEventArgs = ReferencePool.Acquire<JellyKilledEventArgs>();
            jellyKilledEventArgs.EntityId = entityId;
            jellyKilledEventArgs.EntityType = entityType;
            return jellyKilledEventArgs;
        }
        
        public override void Clear()
        {
            EntityId = 0;
            EntityType = 0;
        }
    }
    
    /// <summary>
    /// 墙壁被破坏事件
    /// </summary>
    public class WallBrokenEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(WallBrokenEventArgs).GetHashCode();
        
        public override int Id => EventId;
        
        public int X { get; private set; }
        public int Y { get; private set; }
        
        public static WallBrokenEventArgs Create(int x, int y)
        {
            WallBrokenEventArgs wallBrokenEventArgs = ReferencePool.Acquire<WallBrokenEventArgs>();
            wallBrokenEventArgs.X = x;
            wallBrokenEventArgs.Y = y;
            return wallBrokenEventArgs;
        }
        
        public override void Clear()
        {
            X = 0;
            Y = 0;
        }
    }
    
    /// <summary>
    /// 伤害事件
    /// </summary>
    public class DamageEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DamageEventArgs).GetHashCode();
        
        public override int Id => EventId;
        
        public int EntityId { get; private set; }
        public int Damage { get; private set; }
        public bool IsCrit { get; private set; }
        
        public static DamageEventArgs Create(int entityId, int damage, bool isCrit = false)
        {
            DamageEventArgs damageEventArgs = ReferencePool.Acquire<DamageEventArgs>();
            damageEventArgs.EntityId = entityId;
            damageEventArgs.Damage = damage;
            damageEventArgs.IsCrit = isCrit;
            return damageEventArgs;
        }
        
        public override void Clear()
        {
            EntityId = 0;
            Damage = 0;
            IsCrit = false;
        }
    }
    
    /// <summary>
    /// 伤害反馈事件参数
    /// </summary>
    public class DamageFeedbackEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DamageFeedbackEventArgs).GetHashCode();
        
        public override int Id => EventId;
        
        public int EntityId { get; private set; }
        public int Damage { get; private set; }
        public bool IsCrit { get; private set; }
        public Vector3 Position { get; private set; }
        
        public static DamageFeedbackEventArgs Create(int entityId, int damage, bool isCrit, Vector3 position)
        {
            DamageFeedbackEventArgs damageFeedbackEventArgs = ReferencePool.Acquire<DamageFeedbackEventArgs>();
            damageFeedbackEventArgs.EntityId = entityId;
            damageFeedbackEventArgs.Damage = damage;
            damageFeedbackEventArgs.IsCrit = isCrit;
            damageFeedbackEventArgs.Position = position;
            return damageFeedbackEventArgs;
        }
        
        public override void Clear()
        {
            EntityId = 0;
            Damage = 0;
            IsCrit = false;
            Position = Vector3.zero;
        }
    }
    
    /// <summary>
    /// 关卡胜利事件
    /// </summary>
    public class LevelWinEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(LevelWinEventArgs).GetHashCode();
        
        public override int Id => EventId;
        
        public int LevelId { get; private set; }
        
        public static LevelWinEventArgs Create(int levelId = 0)
        {
            LevelWinEventArgs levelWinEventArgs = ReferencePool.Acquire<LevelWinEventArgs>();
            levelWinEventArgs.LevelId = levelId;
            return levelWinEventArgs;
        }
        
        public override void Clear()
        {
            LevelId = 0;
        }
    }
    
    /// <summary>
    /// 关卡失败事件
    /// </summary>
    public class LevelLoseEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(LevelLoseEventArgs).GetHashCode();
        
        public override int Id => EventId;
        
        public int LevelId { get; private set; }
        
        public static LevelLoseEventArgs Create(int levelId = 0)
        {
            LevelLoseEventArgs levelLoseEventArgs = ReferencePool.Acquire<LevelLoseEventArgs>();
            levelLoseEventArgs.LevelId = levelId;
            return levelLoseEventArgs;
        }
        
        public override void Clear()
        {
            LevelId = 0;
        }
    }
}