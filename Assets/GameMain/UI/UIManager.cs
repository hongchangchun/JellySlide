using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UI管理器 - 负责管理所有UI面板的显示与隐藏
/// </summary>
public class UIManager : MonoBehaviour
{
    /// <summary>
    /// 单例实例
    /// </summary>
    public static UIManager Instance { get; private set; }

    /// <summary>
    /// 存储所有UI面板
    /// </summary>
    private Dictionary<string, UIPanelBase> _panels = new Dictionary<string, UIPanelBase>();

    /// <summary>
    /// 存储当前显示的面板名称
    /// </summary>
    private Stack<string> _activePanels = new Stack<string>();
    
    // 胜利和失败面板
    private VictoryPanelUI _victoryPanel;
    private DefeatPanelUI _defeatPanel;

    // 面板名称常量
    public const string GAME_UI = "GameUI";
    public const string VICTORY_PANEL = "VictoryPanel";
    public const string DEFEAT_PANEL = "DefeatPanel";

    private void Awake()
    {
        // 单例模式实现
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // 自动查找所有子面板
        FindAllPanels();
    }

    private void Start()
    {
        // 初始化时显示主游戏界面
        ShowPanel("GameUI");
    }

    /// <summary>
    /// 查找所有UI面板
    /// </summary>
    private void FindAllPanels()
    {
        UIPanelBase[] panels = GetComponentsInChildren<UIPanelBase>(true);
        foreach (var panel in panels)
        {
            RegisterPanel(panel);
        }
        
        // 初始化胜利和失败面板引用
        _victoryPanel = GetComponentInChildren<VictoryPanelUI>(true);
        _defeatPanel = GetComponentInChildren<DefeatPanelUI>(true);
    }

    /// <summary>
    /// 注册UI面板
    /// </summary>
    public void RegisterPanel(UIPanelBase panel)
    {
        if (!_panels.ContainsKey(panel.PanelName))
        {
            _panels.Add(panel.PanelName, panel);
            panel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 显示UI面板
    /// </summary>
    public void ShowPanel(string panelName)
    {
        if (!_panels.ContainsKey(panelName))
        {
            Debug.LogError($"UI面板 {panelName} 不存在");
            return;
        }

        // 如果面板已经在栈顶，不重复显示
        if (_activePanels.Count > 0 && _activePanels.Peek() == panelName)
        {
            return;
        }

        // 隐藏栈顶面板
        if (_activePanels.Count > 0)
        {
            HidePanel(_activePanels.Peek());
        }

        // 显示目标面板
        _panels[panelName].Show();
        _activePanels.Push(panelName);
    }

    /// <summary>
    /// 隐藏UI面板
    /// </summary>
    public void HidePanel(string panelName)
    {
        if (_panels.ContainsKey(panelName))
        {
            _panels[panelName].Hide();
            if (_activePanels.Count > 0 && _activePanels.Peek() == panelName)
            {
                _activePanels.Pop();
            }
        }
    }

    /// <summary>
    /// 返回上一个面板
    /// </summary>
    public void Back()
    {
        if (_activePanels.Count > 0)
        {
            string currentPanel = _activePanels.Pop();
            HidePanel(currentPanel);

            if (_activePanels.Count > 0)
            {
                string previousPanel = _activePanels.Peek();
                _panels[previousPanel].Show();
            }
        }
    }

    /// <summary>
    /// 显示胜利面板
    /// </summary>
    public void ShowVictoryPanel(int levelId, VictoryPanelUI.VictoryData victoryData)
    {
        Debug.Log($"显示胜利面板，关卡：{levelId}");
        
        // 隐藏游戏UI的其他元素
        GameUI gameUI = GetPanel<GameUI>(GAME_UI);
        if (gameUI != null)
        {
            gameUI.HideGameElements();
        }
        
        // 显示胜利面板
        if (_victoryPanel != null)
        {
            _victoryPanel.Show(levelId, victoryData);
        }
        else
        {
            // 如果没有找到胜利面板组件，尝试实例化或从场景中查找
            FindAndShowVictoryPanel(levelId, victoryData);
        }
    }

    /// <summary>
    /// 显示失败面板
    /// </summary>
    public void ShowDefeatPanel(DefeatPanelUI.FailureData failureData)
    {
        Debug.Log("显示失败面板");
        
        // 隐藏游戏UI的其他元素
        GameUI gameUI = GetPanel<GameUI>(GAME_UI);
        if (gameUI != null)
        {
            gameUI.HideGameElements();
        }
        
        // 显示失败面板
        if (_defeatPanel != null)
        {
            _defeatPanel.Show(GetCurrentLevelId(), failureData);
        }
        else
        {
            // 如果没有找到失败面板组件，尝试实例化或从场景中查找
            FindAndShowDefeatPanel(failureData);
        }
    }

    /// <summary>
    /// 获取指定类型的面板
    /// </summary>
    public T GetPanel<T>(string panelName) where T : UIPanelBase
    {
        if (_panels.ContainsKey(panelName))
        {
            return _panels[panelName] as T;
        }
        return null;
    }

    /// <summary>
    /// 设置关卡显示
    /// </summary>
    public void UpdateLevelInfo(int levelId)
    {
        GameUI gameUI = GetPanel<GameUI>("GameUI");
        if (gameUI != null)
        {
            gameUI.UpdateLevelText(levelId);
        }
    }

    /// <summary>
    /// 查找并显示胜利面板
    /// </summary>
    private void FindAndShowVictoryPanel(int levelId, VictoryPanelUI.VictoryData victoryData)
    {
        _victoryPanel = FindObjectOfType<VictoryPanelUI>();
        if (_victoryPanel != null)
        {
            _victoryPanel.Show(levelId, victoryData);
        }
        else
        {
            Debug.LogWarning("胜利面板未找到，请确保场景中已添加VictoryPanelUI组件");
        }
    }

    /// <summary>
    /// 查找并显示失败面板
    /// </summary>
    private void FindAndShowDefeatPanel(DefeatPanelUI.FailureData failureData)
    {
        _defeatPanel = FindObjectOfType<DefeatPanelUI>();
        if (_defeatPanel != null)
        {
            _defeatPanel.Show(GetCurrentLevelId(), failureData);
        }
        else
        {
            Debug.LogWarning("失败面板未找到，请确保场景中已添加DefeatPanelUI组件");
        }
    }

    /// <summary>
    /// 获取当前关卡ID
    /// </summary>
    private int GetCurrentLevelId()
    {
        var jellyProcedure = FindObjectOfType<JellyProcedure>();
        if (jellyProcedure != null)
        {
            // 这里假设JellyProcedure有GetCurrentLevelId方法
            // 如果没有，返回默认值1
            return 1;
        }
        return 1;
    }

    /// <summary>
    /// 重置UI状态
    /// </summary>
    public void ResetUI()
    {
        // 隐藏胜利和失败面板
        if (_victoryPanel != null)
        {
            _victoryPanel.Hide();
        }
        
        if (_defeatPanel != null)
        {
            _defeatPanel.Hide();
        }
        
        // 显示游戏UI元素
        GameUI gameUI = GetPanel<GameUI>(GAME_UI);
        if (gameUI != null)
        {
            gameUI.ShowGameElements();
        }
    }
}
