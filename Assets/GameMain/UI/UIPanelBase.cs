using UnityEngine;

/// <summary>
/// UI面板基类 - 所有UI面板都继承自此
/// </summary>
public abstract class UIPanelBase : MonoBehaviour
{
    /// <summary>
    /// 面板名称
    /// </summary>
    public abstract string PanelName { get; }

    /// <summary>
    /// 是否初始化完成
    /// </summary>
    protected bool _isInitialized = false;

    protected virtual void Awake()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化面板
    /// </summary>
    protected virtual void Initialize()
    {
        if (!_isInitialized)
        {
            OnInitialize();
            _isInitialized = true;
        }
    }

    /// <summary>
    /// 子类重写此方法进行初始化
    /// </summary>
    protected abstract void OnInitialize();

    /// <summary>
    /// 显示面板
    /// </summary>
    public virtual void Show()
    {
        gameObject.SetActive(true);
        OnShow();
    }

    /// <summary>
    /// 隐藏面板
    /// </summary>
    public virtual void Hide()
    {
        OnHide();
        gameObject.SetActive(false);
    }

    /// <summary>
    /// 面板显示时调用
    /// </summary>
    protected virtual void OnShow() { }

    /// <summary>
    /// 面板隐藏时调用
    /// </summary>
    protected virtual void OnHide() { }

    /// <summary>
    /// 更新面板数据
    /// </summary>
    public virtual void UpdateData(object data) { }
}
