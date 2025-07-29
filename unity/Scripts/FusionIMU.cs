using UnityEngine;
using System;

/// <summary>
/// Fusion IMU组件 - 用于头显追踪的传感器融合
/// 提供去除重力的线性加速度，用于位置估计
/// </summary>
public class FusionIMU : MonoBehaviour
{
    [Header("Fusion配置")]
    [Tooltip("AHRS算法设置")]
    public FusionWrapper.UnityAhrsSettings settings = FusionWrapper.UnityAhrsSettings.Default;
    
    [Header("调试信息")]
    [Tooltip("显示调试信息")]
    public bool showDebugInfo = true;
    
    [Tooltip("在Scene视图中显示坐标轴")]
    public bool showGizmosInScene = true;
    
    [Space]
    [Header("当前状态 (只读)")]
    [SerializeField] private Quaternion _currentAttitude;
    [SerializeField] private Vector3 _linearAcceleration;
    [SerializeField] private Vector3 _earthAcceleration;
    [SerializeField] private Vector3 _gravity;
    [SerializeField] private bool _isInitialising;
    [SerializeField] private bool _isAccelRejected;
    
    // 公共访问属性
    public Quaternion currentAttitude => _currentAttitude;
    public Vector3 linearAcceleration => _linearAcceleration;
    public Vector3 earthAcceleration => _earthAcceleration;
    public Vector3 gravity => _gravity;
    public bool isInitialising => _isInitialising;
    public bool isAccelerationRejected => _isAccelRejected;
    
    // 内部变量
    private IntPtr fusionAhrs;
    private bool isInitialized = false;
    
    // 事件
    public event Action OnInitializationComplete;
    public event Action<Vector3> OnLinearAccelerationUpdated;
    public event Action<Vector3> OnEarthAccelerationUpdated;
    
    void Start()
    {
        InitializeFusion();
    }
    
    void OnDestroy()
    {
        CleanupFusion();
    }
    
    /// <summary>
    /// 初始化Fusion库
    /// </summary>
    private void InitializeFusion()
    {
        try
        {
            // 创建Fusion实例
            fusionAhrs = FusionWrapper.FusionUnity_CreateAhrs();
            if (fusionAhrs == IntPtr.Zero)
            {
                Debug.LogError("[FusionIMU] 创建Fusion AHRS实例失败！");
                return;
            }
            
            // 设置参数
            FusionWrapper.FusionUnity_SetSettings(fusionAhrs, settings);
            
            isInitialized = true;
            Debug.Log("[FusionIMU] Fusion IMU初始化成功");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FusionIMU] 初始化失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 清理Fusion资源
    /// </summary>
    private void CleanupFusion()
    {
        if (fusionAhrs != IntPtr.Zero)
        {
            try
            {
                FusionWrapper.FusionUnity_DestroyAhrs(fusionAhrs);
                fusionAhrs = IntPtr.Zero;
                isInitialized = false;
                Debug.Log("[FusionIMU] Fusion资源已清理");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FusionIMU] 清理资源失败: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 更新IMU数据（6轴：陀螺仪+加速度计）
    /// </summary>
    /// <param name="gyroscope">陀螺仪数据 (rad/s)</param>
    /// <param name="accelerometer">加速度计数据 (m/s²)</param>
    /// <param name="deltaTime">时间间隔</param>
    public void UpdateIMU(Vector3 gyroscope, Vector3 accelerometer, float deltaTime)
    {
        if (!isInitialized || fusionAhrs == IntPtr.Zero) return;
        
        try
        {
            var gyro = FusionWrapper.UnityVector3.FromUnity(gyroscope);
            var accel = FusionWrapper.UnityVector3.FromUnity(accelerometer);
            
            FusionWrapper.FusionUnity_UpdateIMU(fusionAhrs, gyro, accel, deltaTime);
            
            UpdateResults();
        }
        catch (Exception e)
        {
            Debug.LogError($"[FusionIMU] 更新IMU数据失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 更新9DOF数据（陀螺仪+加速度计+磁力计）
    /// </summary>
    /// <param name="gyroscope">陀螺仪数据 (rad/s)</param>
    /// <param name="accelerometer">加速度计数据 (m/s²)</param>
    /// <param name="magnetometer">磁力计数据 (μT)</param>
    /// <param name="deltaTime">时间间隔</param>
    public void Update9DOF(Vector3 gyroscope, Vector3 accelerometer, Vector3 magnetometer, float deltaTime)
    {
        if (!isInitialized || fusionAhrs == IntPtr.Zero) return;
        
        try
        {
            var gyro = FusionWrapper.UnityVector3.FromUnity(gyroscope);
            var accel = FusionWrapper.UnityVector3.FromUnity(accelerometer);
            var mag = FusionWrapper.UnityVector3.FromUnity(magnetometer);
            
            FusionWrapper.FusionUnity_Update9DOF(fusionAhrs, gyro, accel, mag, deltaTime);
            
            UpdateResults();
        }
        catch (Exception e)
        {
            Debug.LogError($"[FusionIMU] 更新9DOF数据失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 更新结果并触发事件
    /// </summary>
    private void UpdateResults()
    {
        if (!isInitialized || fusionAhrs == IntPtr.Zero) return;
        
        try
        {
            // 获取结果
            var q = FusionWrapper.FusionUnity_GetQuaternion(fusionAhrs);
            var linear = FusionWrapper.FusionUnity_GetLinearAcceleration(fusionAhrs);
            var earth = FusionWrapper.FusionUnity_GetEarthAcceleration(fusionAhrs);
            var grav = FusionWrapper.FusionUnity_GetGravity(fusionAhrs);
            
            // 更新内部状态
            _currentAttitude = q.ToUnity();
            _linearAcceleration = linear.ToUnity();
            _earthAcceleration = earth.ToUnity();
            _gravity = grav.ToUnity();
            
            // 更新状态标志
            bool wasInitialising = _isInitialising;
            _isInitialising = FusionWrapper.FusionUnity_IsInitialising(fusionAhrs) != 0;
            _isAccelRejected = FusionWrapper.FusionUnity_IsAccelerationRejected(fusionAhrs) != 0;
            
            // 触发初始化完成事件
            if (wasInitialising && !_isInitialising)
            {
                OnInitializationComplete?.Invoke();
            }
            
            // 触发数据更新事件
            OnLinearAccelerationUpdated?.Invoke(_linearAcceleration);
            OnEarthAccelerationUpdated?.Invoke(_earthAcceleration);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FusionIMU] 更新结果失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 重新应用设置
    /// </summary>
    [ContextMenu("重新应用设置")]
    public void ApplySettings()
    {
        if (isInitialized && fusionAhrs != IntPtr.Zero)
        {
            FusionWrapper.FusionUnity_SetSettings(fusionAhrs, settings);
            Debug.Log("[FusionIMU] 设置已重新应用");
        }
    }
    
    /// <summary>
    /// 使用快速运动预设
    /// </summary>
    [ContextMenu("使用快速运动预设")]
    public void UseFastMotionPreset()
    {
        settings = FusionWrapper.UnityAhrsSettings.FastMotion;
        ApplySettings();
    }
    
    /// <summary>
    /// 使用默认预设
    /// </summary>
    [ContextMenu("使用默认预设")]
    public void UseDefaultPreset()
    {
        settings = FusionWrapper.UnityAhrsSettings.Default;
        ApplySettings();
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.BeginVertical("box");
        
        GUILayout.Label("Fusion IMU状态");
        GUILayout.Label($"初始化中: {(isInitialising ? "是" : "否")}");
        GUILayout.Label($"加速度被拒绝: {(isAccelerationRejected ? "是" : "否")}");
        GUILayout.Label($"姿态: {currentAttitude:F2}");
        GUILayout.Label($"线性加速度: {linearAcceleration:F3} m/s²");
        GUILayout.Label($"世界加速度: {earthAcceleration:F3} m/s²");
        GUILayout.Label($"重力: {gravity:F3} m/s²");
        
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
    
    void OnDrawGizmos()
    {
        if (!showGizmosInScene || !isInitialized) return;
        
        // 绘制坐标轴
        Gizmos.matrix = Matrix4x4.TRS(transform.position, currentAttitude, Vector3.one);
        
        // X轴 - 红色
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.zero, Vector3.right * 0.5f);
        
        // Y轴 - 绿色
        Gizmos.color = Color.green;
        Gizmos.DrawLine(Vector3.zero, Vector3.up * 0.5f);
        
        // Z轴 - 蓝色
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.5f);
        
        // 重力向量 - 黄色
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(Vector3.zero, gravity.normalized * 0.3f);
        
        Gizmos.matrix = Matrix4x4.identity;
    }
} 