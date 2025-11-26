using UnityEngine;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 伤害反馈管理器 - 负责处理所有伤害相关的视觉反馈
/// </summary>
public class DamageFeedbackManager : MonoBehaviour
{
    /// <summary>
    /// 单例实例
    /// </summary>
    public static DamageFeedbackManager Instance { get; private set; }

    [SerializeField] private GameObject _damageTextPrefab;
    [SerializeField] private float _damageTextLifetime = 1.5f;
    [SerializeField] private float _damageTextFloatSpeed = 1.0f;
    [SerializeField] private float _critScaleFactor = 1.5f;
    [SerializeField] private float _shakeDuration = 0.2f;
    [SerializeField] private float _shakeAmount = 0.1f;

    /// <summary>
    /// 对象池 - 存储伤害文本对象
    /// </summary>
    private Queue<GameObject> _damageTextPool = new Queue<GameObject>();
    private int _poolSize = 20;

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

        // 初始化对象池
        InitializePool();
    }

    private void OnEnable()
    {
        // 订阅伤害事件
        EventManager.Instance.Subscribe<DamageEventArgs>(OnDamageDealt);
    }

    private void OnDisable()
    {
        // 取消订阅伤害事件
        EventManager.Instance.Unsubscribe<DamageEventArgs>(OnDamageDealt);
    }

    /// <summary>
    /// 初始化对象池
    /// </summary>
    private void InitializePool()
    {
        if (_damageTextPrefab == null)
        {
            Debug.LogWarning("伤害文本预制体未设置");
            return;
        }

        for (int i = 0; i < _poolSize; i++)
        {
            GameObject damageText = Instantiate(_damageTextPrefab, transform);
            damageText.SetActive(false);
            _damageTextPool.Enqueue(damageText);
        }
    }

    /// <summary>
    /// 处理伤害事件
    /// </summary>
    private void OnDamageDealt(DamageEventArgs args)
    {
        // 显示伤害数字
        ShowDamageNumber(args.Damage, args.Position, args.IsCritical);

        // 如果是暴击，添加震动效果
        if (args.IsCritical)
        {
            StartCoroutine(ShakeCamera(_shakeDuration, _shakeAmount));
        }
    }

    /// <summary>
    /// 显示伤害数字
    /// </summary>
    public void ShowDamageNumber(int damage, Vector3 position, bool isCritical)
    {
        GameObject damageText = GetFromPool();
        if (damageText == null) return;

        // 设置位置
        damageText.transform.position = position;
        damageText.SetActive(true);

        // 设置文本和样式
        TextMesh textMesh = damageText.GetComponent<TextMesh>();
        if (textMesh != null)
        {
            // 暴击显示红色，普通伤害显示白色
            textMesh.color = isCritical ? Color.red : Color.white;
            
            // 暴击数字更大
            textMesh.fontSize = isCritical ? 36 : 24;
            
            // 添加暴击标记
            textMesh.text = isCritical ? $"{damage}!!" : damage.ToString();
        }

        // 启动浮动动画
        StartCoroutine(FloatDamageText(damageText));
    }

    /// <summary>
    /// 从对象池获取伤害文本对象
    /// </summary>
    private GameObject GetFromPool()
    {   
        if (_damageTextPool.Count > 0)
        {   
            return _damageTextPool.Dequeue();
        }
        
        // 如果对象池为空，创建新对象
        if (_damageTextPrefab != null)
        {   
            GameObject newText = Instantiate(_damageTextPrefab, transform);
            return newText;
        }
        
        return null;
    }

    /// <summary>
    /// 伤害文本浮动动画
    /// </summary>
    private IEnumerator FloatDamageText(GameObject damageText)
    {   
        Vector3 startPosition = damageText.transform.position;
        float elapsedTime = 0;
        Color startColor = damageText.GetComponent<TextMesh>().color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0);

        while (elapsedTime < _damageTextLifetime)
        {   
            float t = elapsedTime / _damageTextLifetime;
            
            // 向上浮动
            damageText.transform.position = startPosition + new Vector3(0, _damageTextFloatSpeed * t, 0);
            
            // 渐隐效果
            damageText.GetComponent<TextMesh>().color = Color.Lerp(startColor, endColor, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 回收对象
        damageText.SetActive(false);
        _damageTextPool.Enqueue(damageText);
    }

    /// <summary>
    /// 相机震动效果
    /// </summary>
    public IEnumerator ShakeCamera(float duration, float magnitude)
    {   
        Camera mainCamera = Camera.main;
        if (mainCamera == null) yield break;
        
        Vector3 originalPosition = mainCamera.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {   
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            mainCamera.transform.localPosition = originalPosition + new Vector3(x, y, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        // 恢复原位
        mainCamera.transform.localPosition = originalPosition;
    }
}