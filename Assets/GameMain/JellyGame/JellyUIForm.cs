using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
using DG.Tweening;

namespace StarForce
{
    public class JellyUIForm : UGuiForm
    {
        private ProcedureJellyGame m_Procedure; 
        public Text m_LevelText;
        public Text m_LevelDetailText;
        public GameObject m_WinRoot;
        public GameObject m_LossRoot;
        public Transform m_DamageNumberRoot;
        public GameObject m_DamageNumberTemplate;

        protected override void OnInit(object userData)
        {
            base.OnInit(userData);
            m_DamageNumberTemplate.SetActive(false);
            m_WinRoot.SetActive(false);
            m_LossRoot.SetActive(false);
        }

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            Log.Info("JellyUIForm OnOpen called.");
            m_Procedure = userData as ProcedureJellyGame;
            if (m_Procedure == null)
            {
                Log.Warning("JellyUIForm opened without ProcedureJellyGame reference!");
            }
            m_WinRoot.SetActive(false);
            m_LossRoot.SetActive(false);
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
        }

        public void OnResetButtonClick()
        {
            Log.Info("Reset Level");
            m_LossRoot.SetActive(false);
            GameEntry.Event.Fire(this, ResetLevelEventArgs.Create());
        }

        public void OnBackButtonClick()
        {
            Log.Info("Back to Menu");
            m_Procedure.ReturnToMenu = true;
        }
        public void OnNextLevelButtonClick()
        {
            Log.Info("Next Level Button Clicked");
            m_WinRoot.SetActive(false);
            GameEntry.Event.Fire(this, NextLevelEventArgs.Create());
        }

        public void ShowWinUI()
        {
            Log.Info("JellyUIForm.ShowWinUI called.");
            m_WinRoot.SetActive(true);
            m_LossRoot.SetActive(false);
        }

        public void ShowLoseUI()
        {
            Log.Info("JellyUIForm.ShowLoseUI called.");
            m_WinRoot.SetActive(false);
            m_LossRoot.SetActive(true);
        }

        public void UpdateLevelDisplay(int level)
        {
            if (m_LevelText != null)
            {
                m_LevelText.text = $"Level {level}";
            }
            if (m_LevelDetailText != null)
            {
                m_LevelDetailText.text = MapManager.Instance.CurrentLevelDescription;
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
