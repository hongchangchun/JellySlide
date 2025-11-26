using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 胜利面板UI - 负责显示胜利界面和相关结算信息
/// </summary>
public class VictoryPanelUI : MonoBehaviour
{
    [Header("文本组件")]
    [SerializeField] private Text _levelText;
    [SerializeField] private Text _scoreText;
    [SerializeField] private Text _enemiesKilledText;
    [SerializeField] private Text _timeUsedText;
    [SerializeField] private Text _starsEarnedText;

    [Header("星级显示")]
    [SerializeField] private GameObject[] _starObjects;
    [SerializeField] private float _starAnimationDelay = 0.3f;

    [Header("按钮组件")]
    [SerializeField] private Button _nextLevelButton;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _mainMenuButton;

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
        if (_nextLevelButton != null)
        {
            _nextLevelButton.onClick.AddListener(OnNextLevelClicked);
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        if (_mainMenuButton != null)
        {
            _mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    /// <summary>
    /// 显示胜利面板
    /// </summary>
    public void Show(int levelId, VictoryData victoryData)
    {
        gameObject.SetActive(true);

        // 设置初始状态
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0;
        }
        transform.localScale = _originalScale * 0.8f;

        // 更新文本信息
        UpdateVictoryInfo(levelId, victoryData);

        // 播放入场动画
        StartCoroutine(PlayEntranceAnimation(victoryData));
    }

    /// <summary>
    /// 隐藏胜利面板
    /// </summary>
    public void Hide()
    {
        StartCoroutine(PlayExitAnimation());
    }

    /// <summary>
    /// 更新胜利信息
    /// </summary>
    private void UpdateVictoryInfo(int levelId, VictoryData data)
    {
        if (_levelText != null)
        {
            _levelText.text = $"关卡 {levelId} 完成！";
        }

        if (_scoreText != null)
        {
            _scoreText.text = $"得分: {data.Score}";
        }

        if (_enemiesKilledText != null)
        {
            _enemiesKilledText.text = $"敌人消灭: {data.EnemiesKilled}";
        }

        if (_timeUsedText != null)
        {
            _timeUsedText.text = $"用时: {FormatTime(data.TimeUsed)}";
        }

        if (_starsEarnedText != null)
        {
            _starsEarnedText.text = $"获得星星: {data.StarsEarned}/3";
        }

        // 初始化星星状态
        ResetStars();
    }

    /// <summary>
    /// 重置星星显示
    /// </summary>
    private void ResetStars()
    {
        foreach (var star in _starObjects)
        {
            if (star != null)
            {
                star.SetActive(false);
                // 重置星星动画状态
                star.transform.localScale = Vector3.zero;
            }
        }
    }

    /// <summary>
    /// 入场动画
    /// </summary>
    private IEnumerator PlayEntranceAnimation(VictoryData data)
    {
        float elapsedTime = 0;

        // 淡入和放大动画
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

        // 显示星星动画
        for (int i = 0; i < data.StarsEarned; i++)
        {
            if (i < _starObjects.Length && _starObjects[i] != null)
            {
                _starObjects[i].SetActive(true);
                StartCoroutine(PlayStarAnimation(_starObjects[i]));
            }
            yield return new WaitForSeconds(_starAnimationDelay);
        }
    }

    /// <summary>
    /// 星星动画
    /// </summary>
    private IEnumerator PlayStarAnimation(GameObject star)
    {
        Vector3 originalScale = star.transform.localScale;
        star.transform.localScale = Vector3.zero;

        float elapsedTime = 0;
        while (elapsedTime < _scaleUpDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = _scaleUpCurve.Evaluate(elapsedTime / _scaleUpDuration);
            star.transform.localScale = originalScale * t;
            yield return null;
        }

        star.transform.localScale = originalScale;
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
    /// 下一关按钮点击
    /// </summary>
    private void OnNextLevelClicked()
    {
        Debug.Log("下一关按钮点击");
        Hide();
        
        // 调用JellyProcedure进入下一关
        var jellyProcedure = FindObjectOfType<JellyProcedure>();
        if (jellyProcedure != null)
        {
            jellyProcedure.NextLevel();
        }
    }

    /// <summary>
    /// 重新开始按钮点击
    /// </summary>
    private void OnRestartClicked()
    {
        Debug.Log("重新开始按钮点击");
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
    /// 胜利数据结构
    /// </summary>
    [System.Serializable]
    public class VictoryData
    {
        public int Score;
        public int EnemiesKilled;
        public float TimeUsed;
        public int StarsEarned;

        /// <summary>
        /// 计算得分和星级评价
        /// </summary>
        public static VictoryData Calculate(int enemiesKilled, float timeUsed)
        {
            VictoryData data = new VictoryData();
            data.EnemiesKilled = enemiesKilled;
            data.TimeUsed = timeUsed;

            // 计算得分（基于敌人消灭数量和用时）
            int enemyBonus = enemiesKilled * 100;
            int timeBonus = Mathf.Max(0, Mathf.FloorToInt(1000 - timeUsed * 10));
            data.Score = enemyBonus + timeBonus;

            // 计算星级评价
            // 3星：快速通关且消灭所有敌人
            // 2星：普通通关
            // 1星：基本通关
            if (enemiesKilled >= 5 && timeUsed <= 60)
            {
                data.StarsEarned = 3;
            }
            else if (enemiesKilled >= 3 && timeUsed <= 120)
            {
                data.StarsEarned = 2;
            }
            else
            {
                data.StarsEarned = 1;
            }

            return data;
        }
    }
}