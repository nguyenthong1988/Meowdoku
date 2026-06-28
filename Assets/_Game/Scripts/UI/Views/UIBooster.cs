

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game
{
    public sealed class UIBooster : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private BoosterType _type;
        [SerializeField] private Image _icon;
        [SerializeField] private GameObject _groupUnlock;
        [SerializeField] private GameObject _groupBadge;
        [SerializeField] private GameObject _groupAds;
        [SerializeField] private TextMeshProUGUI _badgeText;

        private IBoosterInventory _inventory;
        private bool _unlocked;

        public BoosterType Type => _type;

        public void Bind(IGameSession session)
        {
            if (session == null) return;

            Unbind();
            _unlocked = session.IsBoosterUnlocked(_type);

            if (_inventory != null)
                _inventory.CountChanged += OnCountChanged;

            Refresh();
        }

        private void OnCountChanged(BoosterType type, int count)
        {
            if (type == _type)
                Refresh();
        }

        public Button Button => _button;

        private void Refresh()
        {
            if (!_unlocked)
            {
                ApplyState(interactable: false, showUnlock: true, showBadge: false, showAds: false, iconAlpha: 1f);
                return;
            }

            int count = _inventory?.Count(_type) ?? 0;

            if (count > 0)
            {
                if (_badgeText != null)
                    _badgeText.text = count.ToString();
                ApplyState(interactable: true, showUnlock: false, showBadge: true, showAds: false, iconAlpha: 1f);
            }
            else
            {
                ApplyState(interactable: true, showUnlock: false, showBadge: false, showAds: true, iconAlpha: 0.75f);
            }
        }

        private void ApplyState(bool interactable, bool showUnlock, bool showBadge, bool showAds, float iconAlpha)
        {
            if (_button != null) _button.interactable = interactable;
            if (_groupUnlock != null) _groupUnlock.SetActive(showUnlock);
            if (_groupBadge != null) _groupBadge.SetActive(showBadge);
            if (_groupAds != null) _groupAds.SetActive(showAds);

            if (_icon != null)
            {
                Color c = _icon.color;
                c.a = iconAlpha;
                _icon.color = c;
            }
        }

        private void Unbind()
        {
            if (_inventory != null)
                _inventory.CountChanged -= OnCountChanged;
            _inventory = null;
        }

        private void OnDestroy() => Unbind();
    }
}
