using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cast.Game
{
    public class Boostrap : MonoBehaviour
    {
        internal static class DeviceTierConstants
        {
            internal const int LOW_MEMORY_THRESHOLD_MB = 3000;
            internal const int MID_MEMORY_THRESHOLD_MB = 4000;
            internal const int LOW_PROCESSOR_THRESHOLD_MHZ = 1500;
            internal const int LOW_CORE_COUNT = 4;

            internal static readonly int s_memMb = SystemInfo.systemMemorySize;
            internal static readonly int s_mhz = SystemInfo.processorFrequency;
            internal static readonly int s_cores = SystemInfo.processorCount;
            internal static readonly string s_processorType = SystemInfo.processorType ?? string.Empty;
            internal static readonly bool s_isA53 = s_processorType.IndexOf("A53", StringComparison.OrdinalIgnoreCase) >= 0;
            internal static readonly bool s_isLowMem = s_memMb < LOW_MEMORY_THRESHOLD_MB;
            internal static readonly bool s_isWeak = s_isA53 || (s_mhz > 0 && s_mhz < LOW_PROCESSOR_THRESHOLD_MHZ) || (s_cores <= LOW_CORE_COUNT && s_isLowMem);
        }

        public static class BootQualitySetup
        {
            private const int ASYNC_UPLOAD_BUFFER_LOW_MB = 8;
            private const int ASYNC_UPLOAD_BUFFER_MID_MB = 16;
            private const int ASYNC_UPLOAD_BUFFER_HIGH_MB = 32;
            private const int ASYNC_UPLOAD_TIMESLICE_WEAK_MS = 4;
            private const int ASYNC_UPLOAD_TIMESLICE_MID_MS = 8;
            private const int ASYNC_UPLOAD_TIMESLICE_HIGH_MS = 16;
            private const int BOOT_TARGET_FRAMERATE = 30;

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
            private static void Apply()
            {
                bool isLowMem = DeviceTierConstants.s_isLowMem;
                bool isWeak = DeviceTierConstants.s_isWeak;
                int memMb = DeviceTierConstants.s_memMb;

                Application.targetFrameRate = BOOT_TARGET_FRAMERATE;
                QualitySettings.vSyncCount = 1;

                Application.backgroundLoadingPriority = isWeak ? ThreadPriority.BelowNormal : ThreadPriority.Normal;  

                QualitySettings.asyncUploadBufferSize =
                    memMb < DeviceTierConstants.LOW_MEMORY_THRESHOLD_MB ? ASYNC_UPLOAD_BUFFER_LOW_MB :
                    memMb < DeviceTierConstants.MID_MEMORY_THRESHOLD_MB ? ASYNC_UPLOAD_BUFFER_MID_MB :
                                                                            ASYNC_UPLOAD_BUFFER_HIGH_MB;

                QualitySettings.asyncUploadTimeSlice =
                    isWeak ? ASYNC_UPLOAD_TIMESLICE_WEAK_MS :
                    memMb < DeviceTierConstants.MID_MEMORY_THRESHOLD_MB ? ASYNC_UPLOAD_TIMESLICE_MID_MS :
                                                                            ASYNC_UPLOAD_TIMESLICE_HIGH_MS;

                QualitySettings.asyncUploadPersistentBuffer = true;

                Debug.Log($"[BootQualitySetup] tier=weak:{isWeak} a53:{DeviceTierConstants.s_isA53} lowMem:{isLowMem} mem={memMb}MB cpu={DeviceTierConstants.s_mhz}MHz/{DeviceTierConstants.s_cores}c proc={DeviceTierConstants.s_processorType}");
            }
        }

        private const int MAIN_SCENE_INDEX = 1;
        private const int GAMEPLAY_TARGET_FRAMERATE = 60;
        private const int GAMEPLAY_UPLOAD_TIMESLICE_MS = 2;
        private const int GAMEPLAY_BUFFER_WEAK_MB = 8;
        private const int GAMEPLAY_BUFFER_DEFAULT_MB = 16;

        private void Start()
        {
            InitializeSceneAsync().Forget();
        }

        private async UniTask InitializeSceneAsync()
        {
            bool isWeak = DeviceTierConstants.s_isWeak;
            Scene bootScene = gameObject.scene;
            float bootStart = Time.realtimeSinceStartup;
            ApplyGameplaySettings(isWeak);
            try
            {
                AsyncOperation loadOp = SceneManager.LoadSceneAsync(MAIN_SCENE_INDEX, LoadSceneMode.Additive);
                if (loadOp == null)
                {
                    Debug.LogError($"[Initializer] Scene index {MAIN_SCENE_INDEX} missing in Build Settings.");
                    return;
                }

                while (!loadOp.isDone)
                    await UniTask.NextFrame();
                if (bootScene.IsValid() && bootScene.isLoaded)
                    await SceneManager.UnloadSceneAsync(bootScene);

                Debug.Log($"[Initializer] Cold start completed in {Time.realtimeSinceStartup - bootStart:F2}s.");
            }
            catch (Exception err)
            {
                Debug.LogException(err);
            }
        }

        private static void ApplyGameplaySettings(bool isWeak)
        {
            Application.targetFrameRate = GAMEPLAY_TARGET_FRAMERATE;
            Application.backgroundLoadingPriority = isWeak
                ? ThreadPriority.BelowNormal
                : ThreadPriority.Normal;
            QualitySettings.asyncUploadTimeSlice = GAMEPLAY_UPLOAD_TIMESLICE_MS;
            QualitySettings.asyncUploadBufferSize = isWeak ? GAMEPLAY_BUFFER_WEAK_MB : GAMEPLAY_BUFFER_DEFAULT_MB;
        }
    }
}
