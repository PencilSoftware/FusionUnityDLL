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
    
    const FusionAhrsSettings fusionSettings = {
        .convention = (FusionConvention)settings.convention,
        .gain = settings.gain,
        .gyroscopeRange = settings.gyroscopeRange,
        .accelerationRejection = settings.accelerationRejection,
        .magneticRejection = settings.magneticRejection,
        .recoveryTriggerPeriod = settings.recoveryTriggerPeriod,
    };
    
    FusionAhrsSetSettings(&instance->ahrs, &fusionSettings);
}

EXPORT void FusionUnity_UpdateIMU(void* ahrs, UnityVector3 gyro, UnityVector3 accel, float deltaTime) {
    if (!ahrs) return;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    
    // 转换为Fusion向量格式
    FusionVector gyroscope = {gyro.x, gyro.y, gyro.z};
    FusionVector accelerometer = {accel.x, accel.y, accel.z};
    
    // 陀螺仪偏移校正
    gyroscope = FusionOffsetUpdate(&instance->offset, gyroscope);
    
    // 更新AHRS（仅使用陀螺仪和加速度计）
    FusionAhrsUpdateNoMagnetometer(&instance->ahrs, gyroscope, accelerometer, deltaTime);
}

EXPORT void FusionUnity_Update9DOF(void* ahrs, UnityVector3 gyro, UnityVector3 accel, UnityVector3 mag, float deltaTime) {
    if (!ahrs) return;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    
    // 转换为Fusion向量格式
    FusionVector gyroscope = {gyro.x, gyro.y, gyro.z};
    FusionVector accelerometer = {accel.x, accel.y, accel.z};
    FusionVector magnetometer = {mag.x, mag.y, mag.z};
    
    // 陀螺仪偏移校正
    gyroscope = FusionOffsetUpdate(&instance->offset, gyroscope);
    
    // 更新AHRS（使用所有九轴数据）
    FusionAhrsUpdate(&instance->ahrs, gyroscope, accelerometer, magnetometer, deltaTime);
}

EXPORT UnityQuaternion FusionUnity_GetQuaternion(void* ahrs) {
    UnityQuaternion result = {1, 0, 0, 0};
    if (!ahrs) return result;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionQuaternion q = FusionAhrsGetQuaternion(&instance->ahrs);
    
    result.w = q.element.w;
    result.x = q.element.x;
    result.y = q.element.y;
    result.z = q.element.z;
    
    return result;
}

EXPORT UnityVector3 FusionUnity_GetLinearAcceleration(void* ahrs) {
    UnityVector3 result = {0, 0, 0};
    if (!ahrs) return result;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionVector accel = FusionAhrsGetLinearAcceleration(&instance->ahrs);
    
    result.x = accel.element.x;
    result.y = accel.element.y;
    result.z = accel.element.z;
    
    return result;
}

EXPORT UnityVector3 FusionUnity_GetEarthAcceleration(void* ahrs) {
    UnityVector3 result = {0, 0, 0};
    if (!ahrs) return result;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionVector accel = FusionAhrsGetEarthAcceleration(&instance->ahrs);
    
    result.x = accel.element.x;
    result.y = accel.element.y;
    result.z = accel.element.z;
    
    return result;
}

EXPORT UnityVector3 FusionUnity_GetGravity(void* ahrs) {
    UnityVector3 result = {0, 0, 0};
    if (!ahrs) return result;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionVector gravity = FusionAhrsGetGravity(&instance->ahrs);
    
    result.x = gravity.element.x;
    result.y = gravity.element.y;
    result.z = gravity.element.z;
    
    return result;
}

EXPORT int FusionUnity_IsInitialising(void* ahrs) {
    if (!ahrs) return 0;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionAhrsFlags flags = FusionAhrsGetFlags(&instance->ahrs);
    
    return flags.initialising ? 1 : 0;
}

EXPORT int FusionUnity_IsAccelerationRejected(void* ahrs) {
    if (!ahrs) return 0;
    
    FusionUnityInstance* instance = (FusionUnityInstance*)ahrs;
    FusionAhrsInternalStates states = FusionAhrsGetInternalStates(&instance->ahrs);
    
    return states.accelerometerIgnored ? 1 : 0;
} 