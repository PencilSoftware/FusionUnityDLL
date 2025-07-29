# Fusion Unity DLL - 头显IMU传感器融合库

基于[xioTechnologies/Fusion](https://github.com/xioTechnologies/Fusion)的Unity原生插件，专为头显追踪和IMU传感器融合设计。

## ✨ 特性

- 🚀 **自动重力补偿** - 直接输出去除重力的线性加速度
- 🎯 **智能加速度拒绝** - 自动过滤不可信的加速度数据
- 🔄 **实时姿态估计** - 高精度的四元数姿态输出
- 📱 **多平台支持** - Windows、macOS、Linux、Android
- ⚡ **高性能** - 原生C代码，专为嵌入式优化
- 🛠️ **易于集成** - 简单的Unity组件接口

## 🏗️ 自动编译（GitHub Actions）

### 第一步：设置仓库

1. **Fork或下载此仓库**
2. **上传到您的GitHub账户**
3. **确保仓库是Public**（免费账户需要Public仓库才能使用Actions）

### 第二步：触发自动编译

有三种方式触发编译：

#### 方式1：推送代码（自动触发）
```bash
git add .
git commit -m "触发编译"
git push
```

#### 方式2：手动触发
1. 进入GitHub仓库页面
2. 点击 **Actions** 标签
3. 选择 **Build Fusion Unity Library**
4. 点击 **Run workflow** 按钮
5. 选择分支，点击 **Run workflow**

#### 方式3：创建Release（推荐）
1. 在GitHub仓库页面点击 **Releases**
2. 点击 **Create a new release**
3. 填写Tag version（如：v1.0.0）
4. 填写Release title和描述
5. 点击 **Publish release**

### 第三步：下载编译结果

#### 从Actions下载（临时文件）
1. 进入 **Actions** 页面
2. 点击最新的构建任务
3. 在 **Artifacts** 部分下载：
   - `FusionUnity-Package` - 完整Unity包
   - `FusionUnity-windows-x64` - Windows 64位库
   - `FusionUnity-android-arm64-v8a` - Android ARM64库
   - 其他平台库...

#### 从Releases下载（正式版本）
1. 进入 **Releases** 页面
2. 下载 `FusionUnity-Package.zip`

## 📦 Unity集成指南

### 安装步骤

1. **下载Unity包**
   ```
   下载 FusionUnity-Package.zip
   解压到您的Unity项目的 Assets 目录
   ```

2. **添加组件**
   ```csharp
   // 为GameObject添加FusionIMU组件
   GameObject headset = new GameObject("Headset");
   FusionIMU fusionIMU = headset.AddComponent<FusionIMU>();
   ```

3. **配置参数**
   ```csharp
   // 设置适合头显的参数
   fusionIMU.settings = FusionWrapper.UnityAhrsSettings.Default;
   ```

### 基本使用

```csharp
using UnityEngine;

public class HeadsetController : MonoBehaviour 
{
    public FusionIMU fusionIMU;
    
    void Update() 
    {
        // 1. 获取您的IMU数据（替换为实际数据源）
        Vector3 gyroscope = GetCardboardGyroscope();      // rad/s
        Vector3 accelerometer = GetCardboardAccelerometer(); // m/s²
        
        // 2. 更新Fusion算法
        fusionIMU.UpdateIMU(gyroscope, accelerometer, Time.deltaTime);
        
        // 3. 获取重力补偿后的加速度 - 这是关键输出！
        Vector3 linearAccel = fusionIMU.linearAcceleration;    // 传感器坐标系
        Vector3 earthAccel = fusionIMU.earthAcceleration;      // 世界坐标系
        
        // 4. 获取姿态
        Quaternion attitude = fusionIMU.currentAttitude;
        
        // 5. 与Camera位置融合（您的现有逻辑）
        FuseWithCameraPosition(earthAccel, attitude);
    }
    
    // 与Camera位置融合的示例
    void FuseWithCameraPosition(Vector3 imuAccel, Quaternion imuAttitude)
    {
        // 您的Camera solvePNP位置
        Vector3 cameraPosition = GetCameraPosition();
        float cameraConfidence = GetCameraConfidence();
        
        if (cameraConfidence > 0.7f)
        {
            // 高可信度：主要使用Camera位置
            transform.position = Vector3.Lerp(transform.position, cameraPosition, 0.8f);
        }
        else
        {
            // 低可信度：使用IMU加速度预测位置
            Vector3 velocity = GetCurrentVelocity();
            velocity += imuAccel * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;
        }
        
        // 姿态主要使用IMU（更可靠）
        transform.rotation = imuAttitude;
    }
}
```

### Android部署

1. **Build Settings配置**
   ```
   Platform: Android
   Target API Level: 21 or higher
   ```

2. **Player Settings配置**
   ```
   Configuration: Release
   Scripting Backend: IL2CPP
   Target Architectures: ARM64 (推荐) 或 ARMv7
   ```

3. **库文件自动包含**
   ```
   Unity会自动包含对应架构的.so文件：
   - ARM64: libFusionUnity.so (arm64-v8a)
   - ARMv7: libFusionUnity.so (armeabi-v7a)
   ```

## 🔧 API参考

### FusionIMU 组件

#### 主要属性
```csharp
Vector3 linearAcceleration      // 传感器坐标系下的线性加速度（已去重力）
Vector3 earthAcceleration       // 世界坐标系下的线性加速度（已去重力）
Quaternion currentAttitude      // 当前姿态四元数
Vector3 gravity                 // 当前重力向量
bool isInitialising            // 是否正在初始化
bool isAccelerationRejected    // 加速度是否被拒绝
```

#### 主要方法
```csharp
void UpdateIMU(Vector3 gyro, Vector3 accel, float deltaTime)           // 6轴更新
void Update9DOF(Vector3 gyro, Vector3 accel, Vector3 mag, float dt)   // 9轴更新
void ApplySettings()                                                   // 重新应用设置
void UseFastMotionPreset()                                           // 使用快速运动预设
```

#### 事件
```csharp
event Action OnInitializationComplete;                    // 初始化完成
event Action<Vector3> OnLinearAccelerationUpdated;       // 线性加速度更新
event Action<Vector3> OnEarthAccelerationUpdated;        // 世界加速度更新
```

### 配置选项

```csharp
public struct UnityAhrsSettings
{
    int convention;              // 坐标系：0=NWU, 1=ENU, 2=NED
    float gain;                  // 增益：0.5（推荐）
    float gyroscopeRange;        // 陀螺仪量程：2000°/s
    float accelerationRejection; // 加速度拒绝阈值：10°
    float magneticRejection;     // 磁力拒绝阈值：10°
    int recoveryTriggerPeriod;   // 恢复触发周期：250 samples
}
```

## 🎯 头显应用优化建议

### 1. 参数调优
```csharp
// 对于快速头部运动
var settings = FusionWrapper.UnityAhrsSettings.FastMotion;
settings.gain = 0.3f;                    // 更信任陀螺仪
settings.accelerationRejection = 15f;     // 更宽松的阈值
```

### 2. 与Camera融合策略
```csharp
// 姿态：主要使用IMU（可靠性高）
// 位置：Camera主导，IMU辅助预测
if (cameraConfidence > threshold)
{
    position = cameraPosition;
    rotation = imuRotation;  // IMU姿态更可靠
}
```

### 3. 性能优化
```csharp
// 减少Update频率（如果可能）
void FixedUpdate()  // 50Hz instead of 60+Hz
{
    fusionIMU.UpdateIMU(gyro, accel, Time.fixedDeltaTime);
}
```

## 🐛 故障排除

### 编译问题

**问题**: Actions编译失败
```
解决方案:
1. 检查仓库是否为Public
2. 确保有足够的Actions使用额度
3. 查看Actions日志了解具体错误
```

**问题**: Android编译失败
```
解决方案:
1. 检查NDK版本是否匹配
2. 确保CMake配置正确
3. 查看详细的编译日志
```

### Unity集成问题

**问题**: DLL加载失败
```
解决方案:
1. 确保库文件在正确的Plugins目录
2. 检查平台设置是否正确
3. 确保架构匹配（x64, ARM64等）
```

**问题**: Android上无法调用函数
```
解决方案:
1. 确保使用CallingConvention.Cdecl
2. 检查.so文件是否在正确的libs目录
3. 确保API Level >= 21
```

### 性能问题

**问题**: 初始化时间过长
```
解决方案:
1. 检查isInitialising状态
2. 等待OnInitializationComplete事件
3. 避免在初始化期间进行复杂计算
```

**问题**: 加速度数据不稳定
```
解决方案:
1. 检查isAccelerationRejected状态
2. 调整accelerationRejection阈值
3. 检查IMU数据质量和噪声水平
```

## 📚 更多资源

- **Fusion原理**: [Madgwick's AHRS Algorithm](https://github.com/xioTechnologies/Fusion)
- **Unity Native Plugins**: [Unity Manual](https://docs.unity3d.com/Manual/NativePlugins.html)
- **IMU传感器融合**: [Sebastian Madgwick's PhD Thesis](https://x-io.co.uk/res/doc/madgwick_internal_report.pdf)

## 📄 许可证

本项目使用MIT许可证 - 查看[LICENSE](LICENSE)文件了解详情。

## 🤝 贡献

欢迎提交Issues和Pull Requests！

1. Fork项目
2. 创建特性分支
3. 提交更改
4. 推送到分支
5. 创建Pull Request

## 📞 支持

如果您遇到问题：

1. 查看本README的故障排除部分
2. 搜索现有的Issues
3. 创建新的Issue并提供详细信息
4. 包含您的Unity版本、平台信息和错误日志

---

**享受使用Fusion进行头显追踪！** 🚀 