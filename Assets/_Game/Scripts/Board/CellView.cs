
using CaskFramework.Audio;
using CaskFramework.Core;
using LitMotion;
using UnityEngine;

namespace Cast.Game
{

    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _character;
        [SerializeField] private SpriteRenderer _markHint;
        [SerializeField] private SpriteRenderer _markWrong;
        [SerializeField] private SpriteAsset _spriteAsset;
        [SerializeField] private float _ghostAlpha = 0.45f;

        private float _baseScale = 1f;
        private GhostState _ghost = GhostState.None;
        private IAudioManager _audio;

        private enum GhostState : byte { None = 0, Hint = 1, Reveal = 2 }

        public int Row { get; private set; }
        public int Col { get; private set; }
        public int ColorIndex { get; private set; }

        public void SetCell(int row, int col, int colorIndex, float cellSize)
        {
            Row = row;
            Col = col;
            ColorIndex = colorIndex;
            _baseScale = cellSize;

            transform.localScale = Vector3.one * _baseScale;

            if (_background != null) _background.sprite = _spriteAsset.GetSprite(colorIndex) != null ? _spriteAsset.GetSprite(colorIndex) : PlaceholderSprites.Square;

            SetMark(PlayerMark.None);
            _audio = GameRuntime.Get<IAudioManager>();
        }

        public PlayerMark CurrentMark { get; private set; }

        public void SetMark(PlayerMark mark)
        {
            CurrentMark = mark;
            RefreshVisual();
        }

        public void SetGhostHint(bool on)
        {
            _ghost = on ? GhostState.Hint : GhostState.None;
            RefreshVisual();
        }

        public void SetGhostReveal(bool on)
        {
            _ghost = on ? GhostState.Reveal : GhostState.None;
            RefreshVisual();
        }

        public void ClearGhost()
        {
            if (_ghost == GhostState.None) return;
            _ghost = GhostState.None;
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            bool ghostHint = _ghost == GhostState.Hint;
            bool ghostReveal = _ghost == GhostState.Reveal;

            bool showCharacter = CurrentMark == PlayerMark.Character || ghostReveal;
            bool showHint = CurrentMark == PlayerMark.Hint || ghostHint;
            bool showWrong = CurrentMark == PlayerMark.Wrong;

            if (_character != null)
            {
                _character.enabled = showCharacter;
                SetRendererAlpha(_character, ghostReveal && CurrentMark != PlayerMark.Character ? _ghostAlpha : 1f);
            }
            if (_markHint != null)
            {
                _markHint.enabled = showHint;
                SetRendererAlpha(_markHint, ghostHint && CurrentMark != PlayerMark.Hint ? _ghostAlpha : 1f);
            }
            if (_markWrong != null) _markWrong.enabled = showWrong;
        }

        private static void SetRendererAlpha(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            Color col = sr.color;
            col.a = a;
            sr.color = col;
        }

        public void SetHidden(Vector3 offset, float scaleFactor)
        {
            transform.localScale = Vector3.one * (_baseScale * scaleFactor);
            transform.position += offset;
            SetAlpha(0f);
        }

        public void AnimateIn(Vector3 target, float duration, float delay)
        {
            LMotion.Create(transform.position, target, duration).WithEase(Ease.OutCubic).WithDelay(delay)
                .Bind(this, (p, c) => c.transform.position = p);
            LMotion.Create(transform.localScale, Vector3.one * _baseScale, duration).WithEase(Ease.OutBack).WithDelay(delay)
                .Bind(this, (s, c) => c.transform.localScale = s);
            LMotion.Create(0f, 1f, duration).WithDelay(delay)
                .Bind(this, (a, c) => c.SetAlpha(a));
        }

        public void PlayPlace()
        {
            _audio.PlaySfx(AudioNames.SFX_REVEAL_SUCCESS);
            LMotion.Create(_baseScale * 0.7f, _baseScale, 0.25f).WithEase(Ease.OutBack)
                .Bind(this, (s, c) => c.transform.localScale = Vector3.one * s);
        }

        public void PlayShake()
        {
            _audio.PlaySfx(AudioNames.SFX_REVEAL_FAILURE);
            LMotion.Create(_baseScale * 1.12f, _baseScale, 0.3f).WithEase(Ease.OutElastic)
                .Bind(this, (s, c) => c.transform.localScale = Vector3.one * s);
        }

        private void SetAlpha(float a)
        {
            ApplyAlpha(_background, a);
        }

        private static void ApplyAlpha(SpriteRenderer sr, float a)
        {
            if (sr == null) return;
            Color col = sr.color;
            col.a = a;
            sr.color = col;
        }

        [System.Serializable]
        private struct SpriteAsset
        {
            public Sprite[] Sprites;
            
            public Sprite GetSprite(int index)
            {
                if (Sprites == null || Sprites.Length == 0) return null;
                return Sprites[Mathf.Clamp(index, 0, Sprites.Length - 1)];
            }
        }

        public void SetSortOrder(int order)
        {
            if (_background != null) _background.sortingOrder = order;
            if (_character != null) _character.sortingOrder = order + 1;
            if (_markHint != null) _markHint.sortingOrder = order + 1;
            if (_markWrong != null) _markWrong.sortingOrder = order + 1;
        }

        public void SetSortingLayer(string layerName)
        {
            int sortingLayerID = SortingLayer.NameToID(layerName);

            if (_background != null) _background.sortingLayerID = sortingLayerID;
            if (_character != null) _character.sortingLayerID = sortingLayerID;
            if (_markHint != null) _markHint.sortingLayerID = sortingLayerID;
            if (_markWrong != null) _markWrong.sortingLayerID = sortingLayerID;
        }
    }
}
