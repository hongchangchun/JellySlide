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
        public Button m_ResetButton;
        public Button m_NextLevelButton;
        public Button m_AdButton;
        public Text m_ReviveCountText;

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
            UpdateReviveDisplay();
        }

        protected override void OnClose(bool isShutdown, object userData)
        {
            base.OnClose(isShutdown, userData);
        }

        public void OnResetButtonClick()
        {
            Log.Info("Reset Level Clicked");
            if (ReviveManager.Instance.ConsumeRevive())
            {
                Log.Info("Revive consumed. Resetting level.");
                UpdateReviveDisplay();
                m_LossRoot.SetActive(false);
                GameEntry.Event.Fire(this, ResetLevelEventArgs.Create());
            }
            else
            {
                Log.Info("No revives left!");
                // Optionally show a message saying "Watch Ad to get more revives!"
            }
        }

        public void OnAdButtonClick()
        {
            Log.Info("Watch Ad Button Clicked");
            AdManager.Instance.ShowRewardedAd(() => {
                ReviveManager.Instance.AddRevive(1);
                UpdateReviveDisplay();
                // Hide Ad button or update UI state if needed
            });
        }
        
        public void OnBackButtonClick()
        {
            Log.Info("Back to Menu");
            m_Procedure.ReturnToMenu = true;
        }

        private void UpdateReviveDisplay()
        {
            if (m_ReviveCountText != null)
            {
                m_ReviveCountText.text = $"Revives: {ReviveManager.Instance.ReviveCount}";
            }
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
