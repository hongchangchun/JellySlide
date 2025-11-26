using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// 失败面板UI - 负责显示失败界面和相关信息
/// </summary>
public class DefeatPanelUI : MonoBehaviour
{
    [Header("文本组件")]
    [SerializeField] private Text _levelText;
    [SerializeField] private Text _failureReasonText;
    [SerializeField] private Text _scoreText;
    [SerializeField] private Text _enemiesKilledText;
    [SerializeField] private Text _timeUsedText;

    [Header("按钮组件")]
    [SerializeField] private Button _retryButton;
    [SerializeField] private Button _mainMenuButton;
    [SerializeField] private Button _exitButton;

    [Header("动画设置")]
    [SerializeField] private float _fadeInDuration = 0.5f;
    [SerializeField] private AnimationCurve _fadeInCurve;
    [SerializeField] private float _scaleUpDuration = 0.5f;
    [SerializeField] private AnimationCurve _scaleUpCurve;

    private CanvasGroup _canvasGroup;
    private Vector3 _originalScale;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _originalScale = transform.localScale;

        // 初始化时隐藏
        gameObject.SetActive(false);

        // 绑定按钮事件
        if (_retryButton != null)
        {
            _retryButton.onClick.AddListener(OnRetryClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }

        if (_exitButton != null)
        {
            _exitButton.onClick.AddListener(OnExitClicked);
        }
    }

    /// <summary>
    /// 显示失败面板
    /// </summary>
    public void Show(int levelId, FailureData failureData)
    {
        gameObject.SetActive(true);

        // 设置初始状态
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0;
        }
        transform.localScale = _originalScale * 0.8f;

        // 更新文本信息
        UpdateFailureInfo(levelId, failureData);

        // 播放入场动画
        StartCoroutine(PlayEntranceAnimation());
    }

    /// <summary>
    /// 隐藏失败面板
    /// </summary>
    public void Hide()
    {
        StartCoroutine(PlayExitAnimation());
    }

    /// <summary>
    /// 更新失败信息
    /// </summary>
    private void UpdateFailureInfo(int levelId, FailureData data)
    {
        if (_levelText != null)
        {
            _levelText.text = $"关卡 {levelId} 失败";
        }

        if (_failureReasonText != null)
        {
            _failureReasonText.text = GetFailureReasonText(data.Reason);
        }

        if (_scoreText != null)
        {
            _scoreText.text = $"最终得分: {data.Score}";
        }

        if (_enemiesKilledText != null)
        {
            _enemiesKilledText.text = $"消灭敌人: {data.EnemiesKilled}";
        }

        if (_timeUsedText != null)
        {
            _timeUsedText.text = $"存活时间: {FormatTime(data.TimeUsed)}";
        }
    }

    /// <summary>
    /// 获取失败原因文本
    /// </summary>
    private string GetFailureReasonText(FailureReason reason)
    {
        switch (reason)
        {
            case FailureReason.PlayerDied:
                return "主角被击败了！";
            case FailureReason.TimeOut:
                return "时间到！";
            case FailureReason.Trapped:
                return "掉进陷阱了！";
            case FailureReason.EnemyReachedGoal:
                return "敌人到达终点！";
            default:
                return "游戏失败";
        }
    }

    /// <summary>
    /// 入场动画
    /// </summary>
    private IEnumerator PlayEntranceAnimation()
    {
        float elapsedTime = 0;

        while (elapsedTime < _fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = _fadeInCurve.Evaluate(elapsedTime / _fadeInDuration);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = t;
            }

            transform.localScale = _originalScale * (0.8f + 0.2f * t);
            yield return null;
        }
    }

    /// <summary>
    /// 退场动画
    /// </summary>
    private IEnumerator PlayExitAnimation()
    {
        float elapsedTime = 0;

        while (elapsedTime < _fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = 1.0f - _fadeInCurve.Evaluate(elapsedTime / _fadeInDuration);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = t;
            }

            transform.localScale = _originalScale * (0.8f + 0.2f * t);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    /// <summary>
    /// 格式化时间
    /// </summary>
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60);
        return $"{minutes:00}:{remainingSeconds:00}";
    }

    /// <summary>
    /// 重试按钮点击
    /// </summary>
    private void OnRetryClicked()
    {
        Debug.Log("重试按钮点击");
        Hide();
        
        // 调用JellyProcedure重置关卡
        var jellyProcedure = FindObjectOfType<JellyProcedure>();
        if (jellyProcedure != null)
        {
            jellyProcedure.ResetLevel();
        }
    }

    /// <summary>
    /// 返回主菜单按钮点击
    /// </summary>
    private void OnMainMenuClicked()
    {
        Debug.Log("返回主菜单按钮点击");
        // 这里应该加载主菜单场景
        // SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// 退出游戏按钮点击
    /// </summary>
    private void OnExitClicked()
    {
        Debug.Log("退出游戏按钮点击");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 失败原因枚举
    /// </summary>
    public enum FailureReason
    {
        PlayerDied,
        TimeOut,
        Trapped,
        EnemyReachedGoal
    }

    /// <summary>
    /// 失败数据结构
    /// </summary>
    [System.Serializable]
    public class FailureData
    {
        public FailureReason Reason;
        public int Score;
        public int EnemiesKilled;
        public float TimeUsed;

        /// <summary>
        /// 计算失败得分
        /// </summary>
        public static FailureData Calculate(FailureReason reason, int enemiesKilled, float timeUsed)
        {
            FailureData data = new FailureData();
            data.Reason = reason;
            data.EnemiesKilled = enemiesKilled;
            data.TimeUsed = timeUsed;

            // 计算失败时的得分（基于敌人消灭数量）
            data.Score = enemiesKilled * 100;

            return data;
        }
    }
}