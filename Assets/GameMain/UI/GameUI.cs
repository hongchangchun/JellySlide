using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// 游戏主界面 - 显示关卡信息、控制按钮等
/// </summary>
public class GameUI : UIPanelBase
{
    [SerializeField] private Text _levelText;
    [SerializeField] private Button _resetButton;
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private GameObject _defeatPanel;
    [SerializeField] private GameObject _tutorialPanel;
    [SerializeField] private Text _victoryLevelText;
    [SerializeField] private Text _defeatLevelText;
    [SerializeField] private Button _nextLevelButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _closeTutorialButton;

    /// <summary>
    /// 面板名称
    /// </summary>
    public override string PanelName => "GameUI";

    /// <summary>
    /// 初始化面板
    /// </summary>
    protected override void OnInitialize()
    {
        // 绑定按钮事件
        if (_resetButton != null)
        {
            _resetButton.onClick.AddListener(OnResetButtonClick);
        }

        if (_nextLevelButton != null)
        {
            _nextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(OnRestartButtonClick);
        }

        if (_closeTutorialButton != null)
        {
            _closeTutorialButton.onClick.AddListener(OnCloseTutorialClick);
        }

        // 默认隐藏结算面板
        if (_victoryPanel != null)
        {
            _victoryPanel.SetActive(false);
        }

        if (_defeatPanel != null)
        {
            _defeatPanel.SetActive(false);
        }

        // 默认显示教程（首次游戏）
        ShowTutorial(true);
    }

    /// <summary>
    /// 更新关卡文本
    /// </summary>
    public void UpdateLevelText(int levelId)
    {
        if (_levelText != null)
        {
            _levelText.text = $"关卡 {levelId}";
        }
    }

    /// <summary>
    /// 显示胜利面板
    /// </summary>
    public void ShowVictoryPanel(int levelId)
    {
        if (_victoryPanel != null)
        {
            if (_victoryLevelText != null)
            {
                _victoryLevelText.text = $"关卡 {levelId} 完成！";
            }
            _victoryPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 显示失败面板
    /// </summary>
    public void ShowDefeatPanel(int levelId)
    {
        if (_defeatPanel != null)
        {
            if (_defeatLevelText != null)
            {
                _defeatLevelText.text = $"关卡 {levelId} 失败";
            }
            _defeatPanel.SetActive(true);
        }
    }

    /// <summary>
    /// 隐藏游戏元素
    /// </summary>
    public void HideGameElements()
    {
        // 隐藏游戏进行中的UI元素
        if (_levelText != null)
        {
            _levelText.gameObject.SetActive(false);
        }

        if (_resetButton != null)
        {
            _resetButton.gameObject.SetActive(false);
        }

        // 隐藏其他游戏相关元素
        Debug.Log("隐藏游戏UI元素");
    }

    /// <summary>
    /// 显示游戏元素
    /// </summary>
    public void ShowGameElements()
    {
        // 显示游戏进行中的UI元素
        if (_levelText != null)
        {
            _levelText.gameObject.SetActive(true);
        }

        if (_resetButton != null)
        {
            _resetButton.gameObject.SetActive(true);
        }

        // 显示其他游戏相关元素
        Debug.Log("显示游戏UI元素");
    }

    /// <summary>
    /// 显示或隐藏教程
    /// </summary>
    public void ShowTutorial(bool show)
    {
        if (_tutorialPanel != null)
        {
            _tutorialPanel.SetActive(show);
        }
    }

    /// <summary>
    /// 重置按钮点击事件
    /// </summary>
    private void OnResetButtonClick()
    {
        Debug.Log("重置关卡按钮点击");
        // 调用游戏流程管理器重置关卡
        var jellyProcedure = FindObjectOfType<JellyProcedure>();
        if (jellyProcedure != null)
        {
            jellyProcedure.ResetLevel();
        }
    }

    /// <summary>
    /// 下一关按钮点击事件
    /// </summary>
    private void OnNextLevelButtonClick()
    {
        Debug.Log("下一关按钮点击");
        // 隐藏胜利面板
        if (_victoryPanel != null)
        {
            _victoryPanel.SetActive(false);
        }
        
        // 调用游戏流程管理器进入下一关
        var jellyProcedure = FindObjectOfType<JellyProcedure>();
        if (jellyProcedure != null)
        {
            jellyProcedure.NextLevel();
        }
    }

    /// <summary>
    /// 重新开始按钮点击事件
    /// </summary>
    private void OnRestartButtonClick()
    {
        Debug.Log("重新开始按钮点击");
        // 隐藏失败面板
        if (_defeatPanel != null)
        {
            _defeatPanel.SetActive(false);
        }
        
        // 调用游戏流程管理器重置关卡
        var jellyProcedure = FindObjectOfType<JellyProcedure>();
        if (jellyProcedure != null)
        {
            jellyProcedure.ResetLevel();
        }
    }

    /// <summary>
    /// 关闭教程按钮点击事件
    /// </summary>
    private void OnCloseTutorialClick()
    {
        ShowTutorial(false);
    }
}
