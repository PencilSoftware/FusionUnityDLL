#ifndef FUSION_UNITY_WRAPPER_H
#define FUSION_UNITY_WRAPPER_H

#ifdef _WIN32
    #define EXPORT __declspec(dllexport)
#else
    #define EXPORT __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Unity兼容的结构体
typedef struct {
    float w, x, y, z;
} UnityQuaternion;

typedef struct {
    float x, y, z;
} UnityVector3;

typedef struct {
    int convention;          // 0=NWU, 1=ENU, 2=NED
    float gain;
    float gyroscopeRange;
    float accelerationRejection;
    float magneticRejection;
    int recoveryTriggerPeriod;
} UnityAhrsSettings;

// 核心API函数
EXPORT void* FusionUnity_CreateAhrs();
EXPORT void FusionUnity_DestroyAhrs(void* ahrs);
EXPORT void FusionUnity_SetSettings(void* ahrs, UnityAhrsSettings settings);

// 更新函数
EXPORT void FusionUnity_UpdateIMU(void* ahrs, UnityVector3 gyro, UnityVector3 accel, float deltaTime);
EXPORT void FusionUnity_Update9DOF(void* ahrs, UnityVector3 gyro, UnityVector3 accel, UnityVector3 mag, float deltaTime);

// 结果获取函数
EXPORT UnityQuaternion FusionUnity_GetQuaternion(void* ahrs);
EXPORT UnityVector3 FusionUnity_GetLinearAcceleration(void* ahrs);
EXPORT UnityVector3 FusionUnity_GetEarthAcceleration(void* ahrs);
EXPORT UnityVector3 FusionUnity_GetGravity(void* ahrs);

// 状态查询函数
EXPORT int FusionUnity_IsInitialising(void* ahrs);
EXPORT int FusionUnity_IsAccelerationRejected(void* ahrs);

#ifdef __cplusplus
}
#endif

#endif 