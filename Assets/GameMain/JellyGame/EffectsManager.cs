using UnityEngine;
using UnityGameFramework.Runtime;
using System.Collections.Generic;

namespace StarForce
{
    /// <summary>
    /// 特效管理器 - 负责处理游戏中的视觉和音效特效
    /// </summary>
    public class EffectsManager : MonoBehaviour
    {
        private static EffectsManager s_Instance;
        public static EffectsManager Instance
        {  
            get 
            { 
                if (s_Instance == null)
                { 
                    GameObject obj = new GameObject("EffectsManager");
                    s_Instance = obj.AddComponent<EffectsManager>();
                    DontDestroyOnLoad(obj);
                } 
                return s_Instance; 
            } 
        }
        
        // 特效预设缓存
        private Dictionary<string, GameObject> _effectPrefabs = new Dictionary<string, GameObject>();
        
        // 特效实例池
        private Dictionary<string, Queue<GameObject>> _effectPools = new Dictionary<string, Queue<GameObject>>();
        
        // 粒子系统组件缓存
        private Dictionary<GameObject, ParticleSystem[]> _particleSystems = new Dictionary<GameObject, ParticleSystem[]>();
        
        // 最大缓存数量
        private int _maxPoolSize = 5;
        
        /// <summary>
        /// 初始化特效管理器
        /// </summary>
        public void Init()
        { 
            // 初始化特效池
            InitEffectPool("WallBreak");
            InitEffectPool("TrapTrigger");
            InitEffectPool("HitEffect");
            InitEffectPool("DeathEffect");
            Log.Info("EffectsManager initialized");
        }
        
        /// <summary>
        /// 初始化特效对象池
        /// </summary>
        private void InitEffectPool(string effectName)
        { 
            if (!_effectPools.ContainsKey(effectName))
            { 
                _effectPools[effectName] = new Queue<GameObject>();
                // 预创建少量特效实例到池中
                for (int i = 0; i < 2; i++)
                { 
                    CreateEffectInstance(effectName);
                }
            } 
        }
        
        /// <summary>
        /// 创建特效实例
        /// </summary>
        private GameObject CreateEffectInstance(string effectName)
        { 
            GameObject prefab = GetEffectPrefab(effectName);
            GameObject effectObj = null;
            
            if (prefab != null)
            { 
                effectObj = Instantiate(prefab);
                effectObj.SetActive(false);
                
                // 缓存粒子系统组件
                ParticleSystem[] systems = effectObj.GetComponentsInChildren<ParticleSystem>();
                _particleSystems[effectObj] = systems;
            } 
            else
            { 
                // 如果没有预设，创建一个简单的粒子效果作为替代
                effectObj = CreateFallbackEffect(effectName);
            }
            
            return effectObj;
        }
        
        /// <summary>
        /// 获取特效预设
        /// </summary>
        private GameObject GetEffectPrefab(string effectName)
        { 
            if (_effectPrefabs.TryGetValue(effectName, out GameObject prefab))
                return prefab;
            
            // 尝试加载预设（这里使用Resources作为简单示例，实际应通过资源管理器）
            prefab = Resources.Load<GameObject>($"Effects/{effectName}");
            if (prefab != null)
                _effectPrefabs[effectName] = prefab;
            
            return prefab;
        }
        
        /// <summary>
        /// 创建回退特效（当没有预设时）
        /// </summary>
        private GameObject CreateFallbackEffect(string effectName)
        { 
            GameObject effectObj = new GameObject($"{effectName}_Fallback");
            ParticleSystem ps = effectObj.AddComponent<ParticleSystem>();
            
            // 根据特效名称设置不同的粒子参数
            var main = ps.main;
            var emission = ps.emission;
            var shape = ps.shape;
            var velocity = ps.velocityOverLifetime;
            
            // 基本设置
            main.startLifetime = 0.5f;
            main.startSpeed = 3f;
            main.startSize = 0.5f;
            main.startColor = GetEffectColor(effectName);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            
            emission.rateOverTime = 20f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0, 20) });
            
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;
            
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            
            // 销毁模块
            var destroyModule = ps.destroyModule;
            destroyModule.mode = ParticleSystemDestroyMode.Automatic;
            
            return effectObj;
        }
        
        /// <summary>
        /// 根据特效名称获取对应的颜色
        /// </summary>
        private Color GetEffectColor(string effectName)
        { 
            switch (effectName)
            { 
                case "WallBreak":
                    return new Color(0.8f, 0.6f, 0.3f); // 饼干墙-金黄色
                case "TrapTrigger":
                    return new Color(0.8f, 0.2f, 0.2f); // 陷阱-红色
                case "HitEffect":
                    return new Color(0.9f, 0.4f, 0.4f); // 命中-橙色
                case "DeathEffect":
                    return new Color(1f, 1f, 1f, 0.8f); // 死亡-白色半透明
                default:
                    return Color.white;
            } 
        }
        
        /// <summary>
        /// 从对象池中获取特效
        /// </summary>
        private GameObject GetEffectFromPool(string effectName)
        { 
            if (!_effectPools.ContainsKey(effectName))
                InitEffectPool(effectName);
            
            Queue<GameObject> pool = _effectPools[effectName];
            GameObject effectObj = null;
            
            if (pool.Count > 0)
            { 
                effectObj = pool.Dequeue();
            }
            else
            { 
                effectObj = CreateEffectInstance(effectName);
            }
            
            return effectObj;
        }
        
        /// <summary>
        /// 回收特效到对象池
        /// </summary>
        private void ReturnToPool(GameObject effectObj, string effectName)
        { 
            if (effectObj == null || !_effectPools.ContainsKey(effectName))
                return;
            
            Queue<GameObject> pool = _effectPools[effectName];
            
            // 如果池已满，则销毁对象
            if (pool.Count >= _maxPoolSize)
            { 
                Destroy(effectObj);
                _particleSystems.Remove(effectObj);
            }
            else
            { 
                effectObj.SetActive(false);
                pool.Enqueue(effectObj);
            }
        }
        
        /// <summary>
        /// 播放墙壁破坏特效
        /// </summary>
        public void PlayWallBreakEffect(Vector3 position)
        { 
            PlayEffect("WallBreak", position, Quaternion.identity, 0.5f);
            PlaySound("WallBreak", 0.8f, 1.0f);
        }
        
        /// <summary>
        /// 播放陷阱触发特效
        /// </summary>
        public void PlayTrapEffect(Vector3 position)
        { 
            PlayEffect("TrapTrigger", position, Quaternion.identity, 0.5f);
            PlaySound("TrapTrigger", 1.0f, 1.0f);
        }
        
        /// <summary>
        /// 播放命中特效
        /// </summary>
        public void PlayHitEffect(Vector3 position)
        { 
            PlayEffect("HitEffect", position, Quaternion.identity, 0.3f);
            PlaySound("Hit", 0.6f, 1.0f);
        }
        
        /// <summary>
        /// 播放死亡特效
        /// </summary>
        public void PlayDeathEffect(Vector3 position)
        { 
            PlayEffect("DeathEffect", position, Quaternion.identity, 0.8f);
            PlaySound("Death", 0.8f, 0.9f);
        }
        
        /// <summary>
        /// 播放特效
        /// </summary>
        private void PlayEffect(string effectName, Vector3 position, Quaternion rotation, float duration)
        { 
            GameObject effectObj = GetEffectFromPool(effectName);
            effectObj.transform.position = position;
            effectObj.transform.rotation = rotation;
            
            // 重置并播放粒子系统
            ParticleSystem[] systems = _particleSystems[effectObj];
            foreach (ParticleSystem ps in systems)
            { 
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
            }
            
            effectObj.SetActive(true);
            
            // 延迟后回收
            StartCoroutine(RecycleAfterDelay(effectObj, effectName, duration));
        }
        
        /// <summary>
        /// 播放音效
        /// </summary>
        private void PlaySound(string soundName, float volume, float pitch)
        { 
            // 这里可以通过GameEntry.Sound播放音效
            // 由于简化实现，暂时不播放实际音效
            Log.Info($"Play sound: {soundName}, Volume: {volume}, Pitch: {pitch}");
        }
        
        /// <summary>
        /// 延迟回收协程
        /// </summary>
        private System.Collections.IEnumerator RecycleAfterDelay(GameObject effectObj, string effectName, float delay)
        { 
            yield return new WaitForSeconds(delay);
            ReturnToPool(effectObj, effectName);
        }
    }
}