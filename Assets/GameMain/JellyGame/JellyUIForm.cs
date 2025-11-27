using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using DG.Tweening;

namespace StarForce
{
    public class JellyUIForm : UIFormLogic
    {
        private Text m_LevelText;
        private Button m_ResetButton;
        private Button m_NextLevelButton; // New Button
        private Transform m_DamageNumberRoot;
        
        // 简单的对象池或直接实例化
        private GameObject m_DamageNumberTemplate;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            
            m_LevelText = transform.Find("LevelText").GetComponent<Text>();
            m_ResetButton = transform.Find("ResetButton").GetComponent<Button>();
            m_NextLevelButton = transform.Find("NextLevelButton").GetComponent<Button>(); // Find button
            m_DamageNumberRoot = transform.Find("DamageNumbers");
            
            // 假设有一个模板在 UI 下
            m_DamageNumberTemplate = transform.Find("DamageNumberTemplate").gameObject;
            m_DamageNumberTemplate.SetActive(false);
            
            m_ResetButton.onClick.AddListener(OnResetButtonClick);
            m_NextLevelButton.onClick.AddListener(OnNextLevelButtonClick); // Add listener
            m_NextLevelButton.gameObject.SetActive(false); // Hide initially
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            // UpdateLevelDisplay(1); // Removed hardcoded level
            m_NextLevelButton.gameObject.SetActive(false);
        }

        private void OnResetButtonClick()
        {
            // 重置当前关卡
            Log.Info("Reset Level");
            GameEntry.Event.Fire(this, ResetLevelEventArgs.Create());
        }

        private void OnNextLevelButtonClick()
        {
            Log.Info("Next Level");
            GameEntry.Event.Fire(this, NextLevelEventArgs.Create());
        }

        public void ShowWinUI()
        {
            m_NextLevelButton.gameObject.SetActive(true);
        }

        public void UpdateLevelDisplay(int level)
        {
            if (m_LevelText != null)
            {
                m_LevelText.text = $"Level {level}";
            }
        }

        public void ShowDamageNumber(Vector3 worldPos, int damage, bool isCrit)
        {
            if (m_DamageNumberTemplate == null) return;

            GameObject go = Instantiate(m_DamageNumberTemplate, m_DamageNumberRoot);
            go.SetActive(true);
            
            // 世界坐标转 UI 坐标
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            // 简单的转换，如果 Canvas 是 ScreenSpaceOverlay
            go.transform.position = screenPos;

            Text text = go.GetComponent<Text>();
            text.text = damage.ToString();
            text.color = isCrit ? Color.red : Color.white;
            text.fontSize = isCrit ? 40 : 24;

            // 动画
            go.transform.DOMoveY(go.transform.position.y + 100, 0.5f).OnComplete(() => Destroy(go));
            text.DOFade(0, 0.5f);
        }
    }
}
