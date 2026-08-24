using CaskFramework.Plugins;
using UnityEngine;

namespace Cast.Game
{
    [CreateAssetMenu(menuName = "Cast/Plugins/Facebook", fileName = "FacebookPluginConfig")]
    public sealed class FacebookPluginConfig : PluginConfig
    {
        [SerializeField] private bool _autoActivateApp = true;
        [SerializeField] private float _initTimeoutSeconds = 10f;

        public bool AutoActivateApp => _autoActivateApp;
        public float InitTimeoutSeconds => _initTimeoutSeconds;

        public override IGamePlugin CreatePlugin() => new FacebookPlugin(this);
    }
}
