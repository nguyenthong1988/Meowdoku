using CaskFramework.Plugins;
using UnityEngine;

namespace Cast.Game
{
    [CreateAssetMenu(menuName = "Cast/Plugins/ATT Consent Gate", fileName = "AttConsentGateConfig")]
    public sealed class AttConsentGateConfig : ConsentGateConfig
    {
        [SerializeField] private float _timeoutSeconds = 30f;

        public override IConsentGate CreateGate() => new AttConsentGate(_timeoutSeconds);
    }
}
