using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using DG.Tweening;

namespace StarForce
{
    public class JellyUIForm : UGuiForm
    {
        private Text m_LevelText;
        private Button m_ResetButton;
        private Button m_NextLevelButton;
        private Transform m_DamageNumberRoot;
        private GameObject m_DamageNumberTemplate;

#if UNITY_2017_3_OR_NEWER
        protected override void OnInit(object userData)
#else
        protected internal override void OnInit(object userData)
#endif
        {
            base.OnInit(userData);
            
            m_LevelText = transform.Find("Panel/LevelText").GetComponent<Text>();
            m_ResetButton = transform.Find("Panel/ResetButton").GetComponent<Button>();
            m_NextLevelButton = transform.Find("Panel/NextLevelButton").GetComponent<Button>();
            m_DamageNumberRoot = transform.Find("Panel/DamageNumbers");
            m_DamageNumberTemplate = transform.Find("Panel/DamageNumberTemplate").gameObject;
            
            m_DamageNumberTemplate.SetActive(false);
            
            m_ResetButton.onClick.AddListener(OnResetButtonClick);
            m_NextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
            m_NextLevelButton.gameObject.SetActive(false);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnOpen(object userData)
#else
        protected internal override void OnOpen(object userData)
#endif
        {
            base.OnOpen(userData);
            Log.Info("JellyUIForm OnOpen called.");
            m_NextLevelButton.gameObject.SetActive(false);
        }

#if UNITY_2017_3_OR_NEWER
        protected override void OnClose(bool isShutdown, object userData)
#else
        protected internal override void OnClose(bool isShutdown, object userData)
#endif
        {
            base.OnClose(isShutdown, userData);
        }

        private void OnResetButtonClick()
        {
            Log.Info("Reset Level");
            GameEntry.Event.Fire(this, ResetLevelEventArgs.Create());
        }

        private void OnNextLevelButtonClick()
        {
            if (!m_NextLevelButton.gameObject.activeSelf) return;
            
            Log.Info("Next Level Button Clicked");
            m_NextLevelButton.interactable = false; // Prevent double click
            GameEntry.Event.Fire(this, NextLevelEventArgs.Create());
        }

        public void ShowWinUI()
        {
            Log.Info("JellyUIForm.ShowWinUI called.");
            if (m_NextLevelButton != null)
            {
                m_NextLevelButton.gameObject.SetActive(true);
                m_NextLevelButton.interactable = true;
            }
        }

        public void ShowLoseUI()
        {
            Log.Info("JellyUIForm.ShowLoseUI called.");
            if (m_ResetButton != null)
            {
                m_ResetButton.gameObject.SetActive(true);
            }
        }

        public void UpdateLevelDisplay(int level)
        {
            if (m_LevelText != null)
            {
                m_LevelText.text = $"Level {level}";
            }
            
            if (m_NextLevelButton != null)
            {
                m_NextLevelButton.gameObject.SetActive(false);
            }
        }

        public void ShowDamageNumber(Vector3 worldPos, int damage, bool isCrit)
        {
            if (m_DamageNumberTemplate == null) return;

            GameObject go = Instantiate(m_DamageNumberTemplate, m_DamageNumberRoot);
            go.SetActive(true);
            
            Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
            go.transform.position = screenPos;

            Text text = go.GetComponent<Text>();
            text.text = damage.ToString();
            text.color = isCrit ? Color.red : Color.white;
            text.fontSize = isCrit ? 40 : 24;

            go.transform.DOMoveY(go.transform.position.y + 100, 0.5f).OnComplete(() => Destroy(go));
            text.DOFade(0, 0.5f);
        }
    }
}
