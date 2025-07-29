using UnityEngine;

/// <summary>
/// 头显追踪示例 - 演示如何使用FusionIMU进行传感器融合
/// 结合Camera位置和IMU加速度进行头显定位
/// </summary>
public class HeadsetTrackingExample : MonoBehaviour
{
    [Header("组件引用")]
    [Tooltip("Fusion IMU组件")]
    public FusionIMU fusionIMU;
    
    [Header("IMU数据模拟（实际项目中替换为真实数据源）")]
    [Tooltip("模拟陀螺仪噪声强度")]
    public float gyroNoiseScale = 0.1f;
    [Tooltip("模拟加速度计噪声强度")]
    public float accelNoiseScale = 0.05f;
    
    [Header("Camera位置数据（来自solvePNP）")]
    [Tooltip("Camera提供的位置（50Hz）")]
    public Vector3 cameraPosition;
    [Tooltip("Camera位置的可信度")]
    [Range(0f, 1f)]
    public float cameraConfidence = 0.8f;
    
    [Header("融合结果")]
    [Tooltip("融合后的头显位置")]
    public Vector3 fusedPosition;
    [Tooltip("融合后的头显旋转")]
    public Quaternion fusedRotation;
    
    // 私有变量
    private Vector3 velocity;
    private Vector3 lastCameraPosition;
    private float lastCameraUpdateTime;
    private bool cameraInitialized = false;
    
    // 模拟数据
    private Vector3 simulatedEulerAngles;
    
    void Start()
    {
        // 确保有FusionIMU组件
        if (fusionIMU == null)
        {
            fusionIMU = GetComponent<FusionIMU>();
            if (fusionIMU == null)
            {
                fusionIMU = gameObject.AddComponent<FusionIMU>();
            }
        }
        
        // 订阅事件
        fusionIMU.OnInitializationComplete += OnFusionInitialized;
        fusionIMU.OnEarthAccelerationUpdated += OnEarthAccelerationUpdated;
        
        // 初始化
        fusedPosition = transform.position;
        fusedRotation = transform.rotation;
        lastCameraUpdateTime = Time.time;
        
        Debug.Log("[HeadsetTracking] 头显追踪示例已启动");
    }
    
    void Update()
    {
        // 1. 模拟IMU数据（实际项目中替换为真实IMU数据）
        SimulateIMUData();
        
        // 2. 模拟Camera数据（实际项目中替换为solvePNP结果）
        SimulateCameraData();
        
        // 3. 应用融合结果到Transform
        ApplyFusedTransform();
    }
    
    /// <summary>
    /// 模拟IMU数据（实际使用时替换为真实数据源）
    /// </summary>
    private void SimulateIMUData()
    {
        // 模拟头部运动
        simulatedEulerAngles += new Vector3(
            Mathf.Sin(Time.time * 0.5f) * 10f,  // Pitch
            Mathf.Sin(Time.time * 0.3f) * 15f,  // Yaw  
            Mathf.Sin(Time.time * 0.7f) * 5f    // Roll
        ) * Time.deltaTime;
        
        // 计算角速度（陀螺仪数据）
        Vector3 angularVelocity = new Vector3(
            Mathf.Cos(Time.time * 0.5f) * 0.5f * 10f,
            Mathf.Cos(Time.time * 0.3f) * 0.3f * 15f,
            Mathf.Cos(Time.time * 0.7f) * 0.7f * 5f
        ) * Mathf.Deg2Rad;
        
        // 添加噪声
        angularVelocity += Random.insideUnitSphere * gyroNoiseScale;
        
        // 模拟加速度计数据（包含重力和运动加速度）
        Vector3 acceleration = Vector3.down * 9.8f; // 重力
        acceleration += Random.insideUnitSphere * accelNoiseScale; // 噪声
        
        // 添加模拟的线性运动
        acceleration += new Vector3(
            Mathf.Sin(Time.time * 0.8f) * 2f,
            Mathf.Sin(Time.time * 1.2f) * 1f,
            Mathf.Sin(Time.time * 0.6f) * 1.5f
        );
        
        // 更新FusionIMU
        fusionIMU.UpdateIMU(angularVelocity, acceleration, Time.deltaTime);
    }
    
    /// <summary>
    /// 模拟Camera位置数据（实际使用时替换为solvePNP结果）
    /// </summary>
    private void SimulateCameraData()
    {
        // 模拟50Hz的Camera更新频率
        if (Time.time - lastCameraUpdateTime >= 0.02f) // 50Hz = 20ms
        {
            // 模拟Camera位置（带一些噪声）
            Vector3 truePosition = new Vector3(
                Mathf.Sin(Time.time * 0.2f) * 2f,
                Mathf.Sin(Time.time * 0.15f) * 1f,
                Mathf.Sin(Time.time * 0.1f) * 3f
            );
            
            cameraPosition = truePosition + Random.insideUnitSphere * 0.01f; // 1cm噪声
            
            // 模拟可信度变化
            cameraConfidence = 0.6f + 0.3f * Mathf.Sin(Time.time * 0.5f);
            
            lastCameraUpdateTime = Time.time;
            cameraInitialized = true;
            
            // 执行位置融合
            FusePosition();
        }
    }
    
    /// <summary>
    /// 融合Camera位置和IMU加速度
    /// </summary>
    private void FusePosition()
    {
        if (!cameraInitialized) return;
        
        // 获取IMU的世界加速度（已去除重力）
        Vector3 earthAcceleration = fusionIMU.earthAcceleration;
        
        // 简单的融合策略：
        // 1. 高可信度时，主要使用Camera位置，IMU作为预测
        // 2. 低可信度时，主要使用IMU预测，Camera作为校正
        
        float deltaTime = Time.time - lastCameraUpdateTime;
        
        if (cameraConfidence > 0.7f)
        {
            // 高可信度：主要信任Camera
            fusedPosition = Vector3.Lerp(fusedPosition, cameraPosition, 0.8f);
            
            // 用Camera位置估算速度
            if (cameraInitialized && deltaTime > 0)
            {
                velocity = (cameraPosition - lastCameraPosition) / deltaTime;
            }
        }
        else
        {
            // 低可信度：主要用IMU预测，Camera做轻微校正
            // 用IMU加速度更新速度和位置
            velocity += earthAcceleration * Time.deltaTime;
            Vector3 predictedPosition = fusedPosition + velocity * Time.deltaTime;
            
            // 轻微向Camera位置校正
            fusedPosition = Vector3.Lerp(predictedPosition, cameraPosition, cameraConfidence * 0.3f);
        }
        
        // 使用IMU的姿态（更可靠）
        fusedRotation = fusionIMU.currentAttitude;
        
        lastCameraPosition = cameraPosition;
    }
    
    /// <summary>
    /// 应用融合结果到Transform
    /// </summary>
    private void ApplyFusedTransform()
    {
        transform.position = fusedPosition;
        transform.rotation = fusedRotation;
    }
    
    /// <summary>
    /// Fusion初始化完成回调
    /// </summary>
    private void OnFusionInitialized()
    {
        Debug.Log("[HeadsetTracking] Fusion IMU初始化完成，开始可靠的姿态估计");
    }
    
    /// <summary>
    /// 世界加速度更新回调 - 用于位置预测
    /// </summary>
    /// <param name="earthAccel">世界坐标系下的加速度（已去重力）</param>
    private void OnEarthAccelerationUpdated(Vector3 earthAccel)
    {
        // 这里可以实现更复杂的位置预测逻辑
        // 例如：卡尔曼滤波、运动模型等
        
        // 简单示例：如果Camera长时间没更新，用IMU维持位置预测
        float timeSinceCamera = Time.time - lastCameraUpdateTime;
        if (timeSinceCamera > 0.1f) // 100ms没有Camera更新
        {
            velocity += earthAccel * Time.deltaTime;
            fusedPosition += velocity * Time.deltaTime;
            
            // 应用阻尼，避免速度无限增长
            velocity *= 0.98f;
        }
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        if (fusionIMU != null)
        {
            fusionIMU.OnInitializationComplete -= OnFusionInitialized;
            fusionIMU.OnEarthAccelerationUpdated -= OnEarthAccelerationUpdated;
        }
    }
    
    /// <summary>
    /// 重置追踪
    /// </summary>
    [ContextMenu("重置追踪")]
    public void ResetTracking()
    {
        fusedPosition = transform.position;
        fusedRotation = transform.rotation;
        velocity = Vector3.zero;
        cameraInitialized = false;
        Debug.Log("[HeadsetTracking] 追踪已重置");
    }
    
    void OnDrawGizmos()
    {
        // 绘制Camera位置
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(cameraPosition, 0.05f);
        Gizmos.DrawLine(cameraPosition, cameraPosition + Vector3.up * 0.2f);
        
        // 绘制融合位置
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(fusedPosition, 0.03f);
        
        // 绘制速度向量
        Gizmos.color = Color.red;
        Gizmos.DrawLine(fusedPosition, fusedPosition + velocity * 0.1f);
        
        // 绘制IMU加速度向量
        if (fusionIMU != null)
        {
            Gizmos.color = Color.blue;
            Vector3 earthAccel = fusionIMU.earthAcceleration;
            Gizmos.DrawLine(fusedPosition, fusedPosition + earthAccel * 0.05f);
        }
    }
} 