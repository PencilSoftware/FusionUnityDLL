#include "FusionUnityWrapper.h"
#include "FusionAhrs.h"
#include "FusionOffset.h"
#include "FusionCalibration.h"
#include <stdlib.h>
#include <string.h>

// 内部实例结构体
typedef struct {
    FusionAhrs ahrs;
    FusionOffset offset;
    int sampleRate;
} FusionUnityInstance;

EXPORT void* FusionUnity_CreateAhrs() {
    FusionUnityInstance* instance = malloc(sizeof(FusionUnityInstance));
    if (instance) {
        FusionAhrsInitialise(&instance->ahrs);
        FusionOffsetInitialise(&instance->offset, 100); // 默认100Hz
        instance->sampleRate = 100;
    }
    return instance;
}

EXPORT void FusionUnity_DestroyAhrs(void* ahrs) {
    if (ahrs) {
        free(ahrs);
    }
}

EXPORT void FusionUnity_SetSettings(void* ahrs, UnityAhrsSettings settings) {
    if (!ahrs) return;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    
    const FusionAhrsSettings ahrsSettings = {
        .convention = (FusionConvention)settings.convention,
        .gain = settings.gain,
        .gyroscopeRange = settings.gyroscopeRange,
        .accelerationRejection = settings.accelerationRejection,
        .magneticRejection = settings.magneticRejection,
        .recoveryTriggerPeriod = settings.recoveryTriggerPeriod,
    };
    
    FusionAhrsSetSettings(&instance->ahrs, &ahrsSettings);
}

EXPORT void FusionUnity_UpdateIMU(void* ahrs, UnityVector3 gyro, UnityVector3 accel, float deltaTime) {
    if (!ahrs) return;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    
    // 转换Unity向量到Fusion向量
    const FusionVector fusionGyro = {
        .array[0] = gyro.x,
        .array[1] = gyro.y,
        .array[2] = gyro.z
    };
    
    const FusionVector fusionAccel = {
        .array[0] = accel.x,
        .array[1] = accel.y,
        .array[2] = accel.z
    };
    
    // 更新AHRS（不使用磁力计）
    FusionAhrsUpdateNoMagnetometer(&instance->ahrs, fusionGyro, fusionAccel, deltaTime);
}

EXPORT void FusionUnity_Update9DOF(void* ahrs, UnityVector3 gyro, UnityVector3 accel, UnityVector3 mag, float deltaTime) {
    if (!ahrs) return;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    
    // 转换Unity向量到Fusion向量
    const FusionVector fusionGyro = {
        .array[0] = gyro.x,
        .array[1] = gyro.y,
        .array[2] = gyro.z
    };
    
    const FusionVector fusionAccel = {
        .array[0] = accel.x,
        .array[1] = accel.y,
        .array[2] = accel.z
    };
    
    const FusionVector fusionMag = {
        .array[0] = mag.x,
        .array[1] = mag.y,
        .array[2] = mag.z
    };
    
    // 更新AHRS（使用磁力计）
    FusionAhrsUpdate(&instance->ahrs, fusionGyro, fusionAccel, fusionMag, deltaTime);
}

EXPORT UnityQuaternion FusionUnity_GetQuaternion(void* ahrs) {
    UnityQuaternion result = {1.0f, 0.0f, 0.0f, 0.0f};
    if (!ahrs) return result;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionQuaternion q = FusionAhrsGetQuaternion(&instance->ahrs);
    
    result.w = q.array[0];
    result.x = q.array[1];
    result.y = q.array[2];
    result.z = q.array[3];
    
    return result;
}

EXPORT UnityVector3 FusionUnity_GetLinearAcceleration(void* ahrs) {
    UnityVector3 result = {0.0f, 0.0f, 0.0f};
    if (!ahrs) return result;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionVector accel = FusionAhrsGetLinearAcceleration(&instance->ahrs);
    
    result.x = accel.axis.x;
    result.y = accel.axis.y;
    result.z = accel.axis.z;
    
    return result;
}

EXPORT UnityVector3 FusionUnity_GetEarthAcceleration(void* ahrs) {
    UnityVector3 result = {0.0f, 0.0f, 0.0f};
    if (!ahrs) return result;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionVector accel = FusionAhrsGetEarthAcceleration(&instance->ahrs);
    
    result.x = accel.axis.x;
    result.y = accel.axis.y;
    result.z = accel.axis.z;
    
    return result;
}

EXPORT UnityVector3 FusionUnity_GetGravity(void* ahrs) {
    UnityVector3 result = {0.0f, 0.0f, -9.81f};
    if (!ahrs) return result;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    
    // 获取当前姿态四元数
    FusionQuaternion q = FusionAhrsGetQuaternion(&instance->ahrs);
    
    // 计算重力在body坐标系中的向量
    // 使用四元数旋转世界坐标系的重力向量[0,0,-9.81]到body坐标系
    float qw = q.array[0], qx = q.array[1], qy = q.array[2], qz = q.array[3];
    
    result.x = 2.0f * (qx * qz - qw * qy) * (-9.81f);
    result.y = 2.0f * (qy * qz + qw * qx) * (-9.81f);
    result.z = (qw * qw - qx * qx - qy * qy + qz * qz) * (-9.81f);
    
    return result;
}

EXPORT int FusionUnity_IsInitialising(void* ahrs) {
    if (!ahrs) return 1;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionAhrsFlags flags = FusionAhrsGetFlags(&instance->ahrs);
    
    return flags.initialising ? 1 : 0;
}

EXPORT int FusionUnity_IsAccelerationRejected(void* ahrs) {
    if (!ahrs) return 0;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionAhrsFlags flags = FusionAhrsGetFlags(&instance->ahrs);
    
    return flags.accelerationRejection ? 1 : 0;
}

EXPORT int FusionUnity_IsMagneticRejected(void* ahrs) {
    if (!ahrs) return 0;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionAhrsFlags flags = FusionAhrsGetFlags(&instance->ahrs);
    
    return flags.magneticRejection ? 1 : 0;
}

EXPORT void FusionUnity_Reset(void* ahrs) {
    if (!ahrs) return;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionAhrsReset(&instance->ahrs);
} 