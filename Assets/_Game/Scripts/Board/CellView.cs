
using System.Collections;
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
        [SerializeField] private SpriteRenderer _markHintL;
        [SerializeField] private SpriteRenderer _markHintR;
        [SerializeField] private SpriteRenderer _markWrongL;
        [SerializeField] private SpriteRenderer _markWrongR;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteAsset _spriteAsset;
        [SerializeField] private float _ghostAlpha = 0.45f;
        [SerializeField] private ParticleSystem _vfxRevealSuccess;

        private static readonly int StateIdle = Animator.StringToHash("Idle");
        private static readonly int StateReveal = Animator.StringToHash("Reveal");
        private static readonly int StateHint = Animator.StringToHash("Hint");
        private static readonly int StateWrong = Animator.StringToHash("Wrong");

        private float _baseScale = 1f;
        private GhostState _ghost = GhostState.None;
        private IAudioManager _audio;
        private int _currentState = StateIdle;
        private Coroutine _vfxRevealSuccessRoutine;

        private enum GhostState : byte { None = 0, Hint = 1, Reveal = 2 }

        public int Row { get; private set; }
        public int Col { get; private set; }
        public int ColorIndex { get; private set; }

        private void Awake()
        {
            if (_vfxRevealSuccess != null) _vfxRevealSuccess.gameObject.SetActive(false);
        }

        public void SetCell(int row, int col, int colorIndex, float cellSize)
        {
            Row = row;
            Col = col;
            ColorIndex = colorIndex;
            _baseScale = cellSize;

            transform.localScale = Vector3.one * _baseScale;

            if (_background != null) _background.sprite = _spriteAsset.GetSprite(colorIndex) != null ? _spriteAsset.GetSprite(colorIndex) : PlaceholderSprites.Square;

            _ghost = GhostState.None;
            _currentState = -1;
            SetMark(PlayerMark.None);
            SetSortingLayer("Gameplay");
            _audio = GameRuntime.Get<IAudioManager>();
        }

        public PlayerMark CurrentMark { get; private set; }

        public void SetMark(PlayerMark mark)
        {
            PlayerMark prev = CurrentMark;
            CurrentMark = mark;
            RefreshVisual();
            if (_audio != null)
            {
                if (mark == PlayerMark.Hint && prev != PlayerMark.Hint)
                    _audio.PlaySfx(AudioNames.SFX_HINT_SELECT);
                else if (mark == PlayerMark.None && prev == PlayerMark.Hint)
                    _audio.PlaySfx(AudioNames.SFX_HINT_UNSELECT);
            }
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

            int targetState = StateIdle;
            if (showCharacter) targetState = StateReveal;
            else if (showHint) targetState = StateHint;
            else if (CurrentMark == PlayerMark.Wrong) targetState = StateWrong;

            PlayState(targetState);

            float charAlpha = showCharacter
                ? (ghostReveal && CurrentMark != PlayerMark.Character ? _ghostAlpha : 1f)
                : 0f;

            float hintAlpha = showHint
                ? (ghostHint && CurrentMark != PlayerMark.Hint ? _ghostAlpha : 1f)
                : 0f;

            SetRendererAlpha(_character, charAlpha);
            SetRendererAlpha(_markHintL, hintAlpha);
            SetRendererAlpha(_markHintR, hintAlpha);
        }

        private void PlayState(int state)
        {
            if (_currentState == state) return;
            _currentState = state;
            if (_animator != null) _animator.Play(state);
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
            PlayVfxRevealSuccess();
        }

        private void PlayVfxRevealSuccess()
        {
            if (_vfxRevealSuccess == null) return;
            if (_vfxRevealSuccessRoutine != null) StopCoroutine(_vfxRevealSuccessRoutine);
            _vfxRevealSuccess.gameObject.SetActive(true);
            _vfxRevealSuccess.Play();
            _vfxRevealSuccessRoutine = StartCoroutine(DeactivateVfxRevealSuccessWhenDone());
        }

        private IEnumerator DeactivateVfxRevealSuccessWhenDone()
        {
            yield return new WaitWhile(() => _vfxRevealSuccess.IsAlive(true));
            _vfxRevealSuccess.gameObject.SetActive(false);
            _vfxRevealSuccessRoutine = null;
        }

        public void PlayShake()
        {
            _audio.PlaySfx(AudioNames.SFX_REVEAL_FAILURE);
            VFXManager.Instance?.Play(VfxIds.HeartBreak, transform.position);
        }

        private void SetAlpha(float a)
        {
            ApplyAlpha(_background, a);
            ApplyAlpha(_character, a);
            ApplyAlpha(_markHintL, a);
            ApplyAlpha(_markHintR, a);
            ApplyAlpha(_markWrongL, a);
            ApplyAlpha(_markWrongR, a);
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
            if (_markHintL != null) _markHintL.sortingOrder = order + 1;
            if (_markHintR != null) _markHintR.sortingOrder = order + 1;
            if (_markWrongL != null) _markWrongL.sortingOrder = order + 1;
            if (_markWrongR != null) _markWrongR.sortingOrder = order + 1;
        }

        public void SetSortingLayer(string layerName)
        {
            int sortingLayerID = SortingLayer.NameToID(layerName);

            if (_background != null) _background.sortingLayerID = sortingLayerID;
            if (_character != null) _character.sortingLayerID = sortingLayerID;
            if (_markHintL != null) _markHintL.sortingLayerID = sortingLayerID;
            if (_markHintR != null) _markHintR.sortingLayerID = sortingLayerID;
            if (_markWrongL != null) _markWrongL.sortingLayerID = sortingLayerID;
            if (_markWrongR != null) _markWrongR.sortingLayerID = sortingLayerID;
        }
    }
}
