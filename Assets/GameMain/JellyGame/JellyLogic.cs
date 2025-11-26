using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening; // 需安装 DOTween
using System.Collections.Generic;
using System.Collections;

namespace StarForce
{
    /// <summary>
    /// 挂载在果冻 Prefab 上的逻辑脚本
    /// </summary>
    public class JellyLogic : EntityLogic
    {
        private int m_JellyId;
        private SpriteRenderer m_Renderer;
        
        // 击退参数配置 - 使用GameConstants中的常量
    private readonly float _repulseDuration = GameConstants.REPULSE_DURATION;
    private readonly float _repulseDistance = GameConstants.REPULSE_DISTANCE;
    
    // 伤害相关配置 - 使用GameConstants中的常量
    private readonly int _baseDamage = GameConstants.BASE_DAMAGE;
    private readonly int _critMultiplier = GameConstants.CRIT_MULTIPLIER;
    private readonly int _trapDamage = GameConstants.TRAP_DAMAGE;
        
        // 连锁反应标记
        private bool m_IsInChainReaction = false;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_Renderer = GetComponentInChildren<SpriteRenderer>();
            
            // 注册事件监听
            GameEntry.Event.Subscribe(JellyKilledEventArgs.EventId, OnJellyKilled);
        }

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            // userData 传入初始数据
            if (userData is MapManager.JellyData data)
            {
                m_JellyId = data.Id;
                // 设置初始位置
                transform.position = MapManager.Instance.GridToWorld(data.X, data.Y);
                
                // 根据类型换色 (简单的视觉区分)
                m_Renderer.color = data.Type == 0 ? Color.green : Color.red; 
            }
        }
        
        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
            // 取消事件订阅
            GameEntry.Event.Unsubscribe(JellyKilledEventArgs.EventId, OnJellyKilled);
        }

        // 执行移动动画
        public void ExecuteMove(int targetX, int targetY, System.Action onComplete = null)
        {
            Vector3 worldPos = MapManager.Instance.GridToWorld(targetX, targetY);
            
            // 挤压动画 (Squash)
            transform.DOScale(new Vector3(1.2f, 0.8f, 1f), 0.1f).OnComplete(() =>
            {
                transform.DOScale(Vector3.one, 0.1f);
            });

            // 移动
            transform.DOMove(worldPos, 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                onComplete?.Invoke();
                // 通知流程层移动结束
                GameEntry.Event.Fire(this, JellyMoveCompleteEventArgs.Create(m_JellyId));
            });
        }

        // 受伤震动
        public void PlayHitAnimation()
        {
            transform.DOShakePosition(0.2f, 0.3f);
            
            // 同时播放受伤闪光效果
            StartCoroutine(FlashEffect());
        }
        
        public void Die()
        {
            transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => 
            {
                GameEntry.Entity.HideEntity(m_JellyId);
            });
        }
        
        /// <summary>
        /// 应用击退效果
        /// </summary>
        public void ApplyRepulse(int repulseDirX, int repulseDirY)
        {
            Vector3 repulseDirection = new Vector3(repulseDirX, 0, repulseDirY).normalized;
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + repulseDirection * _repulseDistance;
            
            transform.DOMove(endPos, _repulseDuration).SetEase(Ease.OutQuad).OnComplete(() => {
                transform.DOMove(startPos, _repulseDuration).SetEase(Ease.InQuad);
            });
        }
        
        /// <summary>
        /// 处理受到伤害
        /// </summary>
        public bool TakeDamage(int damage, bool isCrit = false)
        {
            bool isKilled = MapManager.Instance.ApplyDamage(m_JellyId, damage);
            
            // 触发伤害事件
            GameEntry.Event.Fire(this, DamageEventArgs.Create(m_JellyId, damage, isCrit));
            
            // 播放伤害反馈
            PlayHitAnimation();
            
            if (isKilled)
            {
                Die();
            }
            
            return isKilled;
        }
        
        /// <summary>
        /// 处理陷阱伤害
        /// </summary>
        public bool TakeTrapDamage()
        {
            // 陷阱伤害视为暴击，30%暴击率提升
            bool isCrit = UnityEngine.Random.value < 0.3f || true; // 基础为暴击，额外增加30%特殊暴击效果
            int damage = _trapDamage;
            
            // 获取当前位置，用于触发陷阱特效
            if (MapManager.Instance.m_Entities.TryGetValue(m_JellyId, out MapManager.JellyData entityData))
            {
                // 触发陷阱视觉效果
                MapManager.Instance.TriggerTrap(entityData.X, entityData.Y);
            }
            
            // 添加特殊的陷阱伤害震动效果
            StartCoroutine(TrapDamageShake());
            
            return TakeDamage(damage, isCrit);
        }
        
        /// <summary>
        /// 陷阱伤害震动效果
        /// </summary>
        private System.Collections.IEnumerator TrapDamageShake()
        {
            Vector3 originalPosition = transform.localPosition;
            float shakeDuration = 0.5f;
            float shakeAmount = 0.2f;
            float shakeSpeed = 10.0f;
            float elapsedTime = 0;
            
            Color originalColor = m_Renderer.color;
            // 红色闪烁效果
            m_Renderer.color = Color.red;
            
            while (elapsedTime < shakeDuration)
            {
                elapsedTime += Time.deltaTime;
                
                // 计算震动位移
                float x = Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
                float y = Mathf.Cos(Time.time * shakeSpeed * 1.2f) * shakeAmount;
                
                transform.localPosition = originalPosition + new Vector3(x, y, 0);
                
                // 颜色渐变回正常
                m_Renderer.color = Color.Lerp(Color.red, originalColor, elapsedTime / shakeDuration);
                
                yield return null;
            }
            
            // 恢复原始位置和颜色
            transform.localPosition = originalPosition;
            m_Renderer.color = originalColor;
        }
        
        /// <summary>
        /// 执行物理碰撞处理
        /// </summary>
        public void HandleCollision(int hitEntityId, int hitDirX, int hitDirY, SlideResult slideResult)
        {
            // 获取被击中实体的数据
            if (!MapManager.Instance.m_Entities.TryGetValue(hitEntityId, out MapManager.JellyData hitData))
                return;
            
            // 计算伤害
            int damage = BASE_DAMAGE;
            bool isCrit = false;
            
            // 检查是否是地形杀暴击（撞到墙或边界）
            if (slideResult.HitWallType > 0 || IsOutOfBounds(hitData.X + hitDirX, hitData.Y + hitDirY))
            {
                isCrit = true;
                damage = _baseDamage * _critMultiplier;
            }
            
            // 对被击中实体造成伤害
            if (MapManager.Instance.m_Entities.TryGetValue(hitEntityId, out MapManager.JellyData targetData) && !targetData.IsDead)
            {
                Entity targetEntity = GameEntry.Entity.GetEntity(hitEntityId);
                if (targetEntity != null && targetEntity.Logic is JellyLogic targetLogic)
                {
                    // 应用击退效果
                    targetLogic.ApplyRepulse(hitDirX, hitDirY);
                    
                    // 造成伤害
                    bool isKilled = targetLogic.TakeDamage(damage, isCrit);
                    
                    // 如果击杀了实体，检查连锁反应
                    if (isKilled && !m_IsInChainReaction)
                    {
                        StartCoroutine(ProcessChainReaction(hitDirX, hitDirY));
                    }
                }
            }
        }
        
        /// <summary>
        /// 处理连锁反应
        /// </summary>
        private System.Collections.IEnumerator ProcessChainReaction(int dirX, int dirY)
        {
            m_IsInChainReaction = true;
            
            // 给一点延迟，让动画播放
            yield return new WaitForSeconds(0.3f);
            
            // 获取当前实体数据
            if (!MapManager.Instance.m_Entities.TryGetValue(m_JellyId, out MapManager.JellyData selfData) || selfData.IsDead)
            {
                m_IsInChainReaction = false;
                yield break;
            }
            
            // 计算新的滑动结果
            SlideResult chainResult = MapManager.Instance.CalculateSlide(selfData.X, selfData.Y, dirX, dirY, m_JellyId);
            
            // 如果还有碰撞，继续处理连锁反应
            if (chainResult.HitEntityId != -1)
            {
                HandleCollision(chainResult.HitEntityId, dirX, dirY, chainResult);
            }
            
            m_IsInChainReaction = false;
        }
        
        /// <summary>
        /// 检查坐标是否超出边界
        /// </summary>
        private bool IsOutOfBounds(int x, int y)
        {
            return x < 0 || x >= 6 || y < 0 || y >= 6; // 假设地图大小为6x6
        }
        
        /// <summary>
        /// 受伤闪光效果
        /// </summary>
        private System.Collections.IEnumerator FlashEffect()
        {
            if (m_Renderer != null)
            {
                Color originalColor = m_Renderer.color;
                m_Renderer.color = Color.white;
                yield return new WaitForSeconds(0.05f);
                m_Renderer.color = originalColor;
            }
        }
        
        /// <summary>
        /// 处理实体被击杀事件
        /// </summary>
        private void OnJellyKilled(object sender, GameEventArgs e)
        {
            JellyKilledEventArgs args = (JellyKilledEventArgs)e;
            // 可以在这里添加死亡相关的逻辑处理
        }
    }
}