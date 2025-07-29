using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class FusionWrapper
{
    // 平台相关的库名称
    #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        const string DLL_NAME = "FusionUnity";
    #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        const string DLL_NAME = "FusionUnity";
    #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        const string DLL_NAME = "FusionUnity";
    #elif UNITY_ANDROID
        const string DLL_NAME = "FusionUnity";
    #elif UNITY_IOS
        const string DLL_NAME = "__Internal";
    #else
        const string DLL_NAME = "FusionUnity";
    #endif

    [StructLayout(LayoutKind.Sequential)]
    public struct UnityQuaternion
    {
        public float w, x, y, z;
        
        public Quaternion ToUnity()
        {
            return new Quaternion(x, y, z, w);
        }
        
        public static UnityQuaternion FromUnity(Quaternion q)
        {
            return new UnityQuaternion { w = q.w, x = q.x, y = q.y, z = q.z };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UnityVector3
    {
        public float x, y, z;
        
        public Vector3 ToUnity()
        {
            return new Vector3(x, y, z);
        }
        
        public static UnityVector3 FromUnity(Vector3 v)
        {
            return new UnityVector3 { x = v.x, y = v.y, z = v.z };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct UnityAhrsSettings
    {
        public int convention;          // 0=NWU, 1=ENU, 2=NED
        public float gain;
        public float gyroscopeRange;
        public float accelerationRejection;
        public float magneticRejection;
        public int recoveryTriggerPeriod;
        
        /// <summary>
        /// 适合Unity和头显的默认设置
        /// </summary>
        public static UnityAhrsSettings Default => new UnityAhrsSettings
        {
            convention = 1,      // ENU坐标系，适合Unity
            gain = 0.5f,         // 平衡陀螺仪和加速度计
            gyroscopeRange = 2000f,    // 2000度/秒
            accelerationRejection = 10f,  // 10度阈值
            magneticRejection = 10f,      // 10度阈值  
            recoveryTriggerPeriod = 250   // 5秒@50Hz
        };
        
        /// <summary>
        /// 适合头显快速运动的设置
        /// </summary>
        public static UnityAhrsSettings FastMotion => new UnityAhrsSettings
        {
            convention = 1,
            gain = 0.3f,         // 更信任陀螺仪
            gyroscopeRange = 2000f,
            accelerationRejection = 15f,  // 更宽松的阈值
            magneticRejection = 15f,
            recoveryTriggerPeriod = 150   // 3秒@50Hz
        };
    }

    // DLL函数声明
    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr FusionUnity_CreateAhrs();

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FusionUnity_DestroyAhrs(IntPtr ahrs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FusionUnity_SetSettings(IntPtr ahrs, UnityAhrsSettings settings);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FusionUnity_UpdateIMU(IntPtr ahrs, UnityVector3 gyro, UnityVector3 accel, float deltaTime);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern void FusionUnity_Update9DOF(IntPtr ahrs, UnityVector3 gyro, UnityVector3 accel, UnityVector3 mag, float deltaTime);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UnityQuaternion FusionUnity_GetQuaternion(IntPtr ahrs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UnityVector3 FusionUnity_GetLinearAcceleration(IntPtr ahrs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UnityVector3 FusionUnity_GetEarthAcceleration(IntPtr ahrs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern UnityVector3 FusionUnity_GetGravity(IntPtr ahrs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int FusionUnity_IsInitialising(IntPtr ahrs);

    [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
    public static extern int FusionUnity_IsAccelerationRejected(IntPtr ahrs);
} 