using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;

namespace Cast.Game
{
    public static class VfxFlyer
    {
        private const float MinFlightDuration = 0.1f;

        private static readonly Dictionary<VfxInstance, Stack<VfxInstance>> _pools = new Dictionary<VfxInstance, Stack<VfxInstance>>();

        public static async UniTask Fly(VfxInstance prefab, Vector3 from, Transform target, FlyerPayload payload, FlightPath path, Action<int> onLanded, Action onCompleted = null)
        {
            if (prefab == null) return;

            int landingCount = Mathf.Max(1, payload.LandingCount);
            float flightDuration = Mathf.Max(MinFlightDuration, payload.FlightDuration);

            int baseAmount = payload.TotalAmount / landingCount;
            int remainder = payload.TotalAmount - baseAmount * landingCount;

            UniTask[] tasks = new UniTask[landingCount];
            for (int i = 0; i < landingCount; i++)
            {
                int amount = baseAmount + (i == landingCount - 1 ? remainder : 0);
                tasks[i] = FlyItem(prefab, from, target, payload, path, flightDuration, i, amount, onLanded);
            }

            await UniTask.WhenAll(tasks);
            onCompleted?.Invoke();
        }

        private static async UniTask FlyItem(VfxInstance prefab, Vector3 from, Transform target, FlyerPayload payload, FlightPath path, float flightDuration, int index, int amount, Action<int> onLanded)
        {
            if (payload.LaunchInterval > 0f && index > 0)
                await UniTask.Delay(TimeSpan.FromSeconds(payload.LaunchInterval * index));

            VfxInstance instance = Rent(prefab);
            instance.transform.position = from;
            Vector3 baseScale = instance.transform.localScale;
            instance.Play();

            if (payload.AppearDuration > 0f)
            {
                instance.transform.localScale = Vector3.zero;
                await LMotion.Create(Vector3.zero, baseScale, payload.AppearDuration)
                    .WithEase(Ease.OutBack)
                    .Bind(instance, (s, inst) => inst.transform.localScale = s)
                    .ToUniTask();
            }

            if (payload.DelayBeforeFlight > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(payload.DelayBeforeFlight));

            FlightPath itemPath = path != null ? path.CreateVariant() : null;
            Vector3 lastKnownTarget = target != null ? target.position : from;

            await LMotion.Create(0f, 1f, flightDuration)
                .WithEase(Ease.InQuad)
                .Bind(instance, (t, inst) =>
                {
                    if (inst == null) return;
                    if (target != null) lastKnownTarget = target.position;
                    inst.transform.position = itemPath != null
                        ? itemPath.Evaluate(from, lastKnownTarget, t)
                        : Vector3.LerpUnclamped(from, lastKnownTarget, t);
                })
                .ToUniTask();

            onLanded?.Invoke(amount);

            instance.Stop();
            Return(prefab, instance);
        }

        private static VfxInstance Rent(VfxInstance prefab)
        {
            if (_pools.TryGetValue(prefab, out Stack<VfxInstance> stack) && stack.Count > 0)
            {
                VfxInstance pooled = stack.Pop();
                if (pooled != null)
                {
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }
            }

            VfxInstance instance = UnityEngine.Object.Instantiate(prefab);
            return instance;
        }

        private static void Return(VfxInstance prefab, VfxInstance instance)
        {
            if (instance == null) return;

            instance.gameObject.SetActive(false);

            if (!_pools.TryGetValue(prefab, out Stack<VfxInstance> stack))
            {
                stack = new Stack<VfxInstance>();
                _pools[prefab] = stack;
            }
            stack.Push(instance);
        }
    }
}
