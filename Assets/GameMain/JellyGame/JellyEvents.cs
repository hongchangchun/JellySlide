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

    public class JellyKilledEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(JellyKilledEventArgs).GetHashCode();
        public override int Id => EventId;

        public int EntityId { get; private set; }

        public static JellyKilledEventArgs Create(int entityId)
        {
            var e = ReferencePool.Acquire<JellyKilledEventArgs>();
            e.EntityId = entityId;
            return e;
        }
        public override void Clear() { EntityId = 0; }
    }

    public class DamageEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(DamageEventArgs).GetHashCode();
        public override int Id => EventId;

        public int EntityId { get; private set; }
        public int Damage { get; private set; }
        public bool IsCrit { get; private set; }

        public static DamageEventArgs Create(int entityId, int damage, bool isCrit)
        {
            var e = ReferencePool.Acquire<DamageEventArgs>();
            e.EntityId = entityId;
            e.Damage = damage;
            e.IsCrit = isCrit;
            return e;
        }
        public override void Clear() { EntityId = 0; Damage = 0; IsCrit = false; }
    }

    public class ResetLevelEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(ResetLevelEventArgs).GetHashCode();
        public override int Id => EventId;
        public static ResetLevelEventArgs Create() => ReferencePool.Acquire<ResetLevelEventArgs>();
        public override void Clear() { }
    }

    public class NextLevelEventArgs : GameEventArgs
    {
        public static readonly int EventId = typeof(NextLevelEventArgs).GetHashCode();
        public override int Id => EventId;
        public static NextLevelEventArgs Create() => ReferencePool.Acquire<NextLevelEventArgs>();
        public override void Clear() { }
    }
}