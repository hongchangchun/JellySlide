using UnityEngine;
using UnityGameFramework.Runtime;
using DG.Tweening; // 需安装 DOTween

namespace StarForce
{
    /// <summary>
    /// 挂载在果冻 Prefab 上的逻辑脚本
    /// </summary>
    public class JellyLogic : EntityLogic
    {
        private int m_JellyId;
        private SpriteRenderer m_Renderer;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_Renderer = GetComponentInChildren<SpriteRenderer>();
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
        }
        
        public void Die()
        {
            transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => 
            {
                GameEntry.Entity.HideEntity(m_JellyId);
            });
        }
    }
}