
using System.Collections;
using CaskFramework.Audio;
using CaskFramework.Core;
using LitMotion;
using UnityEngine;

namespace Cast.Game
{

    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private CellBackgroundView _backgroundView;
        [SerializeField] private CharacterMarkView _characterView;
        [SerializeField] private HintMarkView _hintView;
        [SerializeField] private WrongMarkView _wrongView;
        [SerializeField] private SpriteAsset _spriteAsset;
        [SerializeField] private ParticleSystem _vfxRevealSuccess;
        [SerializeField] private float _punchScale = 1.08f;
        [SerializeField] private float _punchDuration = 0.08f;

        private float _baseScale = 1f;
        private GhostState _ghost = GhostState.None;
        private IAudioManager _audio;
        private Coroutine _vfxRevealSuccessRoutine;
        private MotionHandle _punchHandle;

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

            if (_punchHandle.IsActive()) _punchHandle.Cancel();
            transform.localScale = Vector3.one * _baseScale;

            _backgroundView.SetSprite(_spriteAsset.GetSprite(colorIndex) ?? PlaceholderSprites.Square);

            _ghost = GhostState.None;
            _backgroundView.ResetInstant();
            _characterView.ResetInstant();
            _hintView.ResetInstant();
            _wrongView.ResetInstant();
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
            bool showHint = !showCharacter && (CurrentMark == PlayerMark.Hint || ghostHint);
            bool showWrong = !showCharacter && !showHint && CurrentMark == PlayerMark.Wrong;

            if (showCharacter) _characterView.Show(ghostReveal && CurrentMark != PlayerMark.Character);
            else _characterView.Hide();

            if (showHint) _hintView.Show(ghostHint && CurrentMark != PlayerMark.Hint);
            else _hintView.Hide();

            if (showWrong) _wrongView.Show(false);
            else _wrongView.Hide();
        }

        public void SetHidden(Vector3 offset, float scaleFactor)
        {
            transform.localScale = Vector3.one * (_baseScale * scaleFactor);
            transform.position += offset;
            _backgroundView.ResetInstant();
        }

        public void AnimateIn(Vector3 target, float duration, float delay)
        {
            LMotion.Create(transform.position, target, duration).WithEase(Ease.OutCubic).WithDelay(delay)
                .Bind(this, (p, c) => c.transform.position = p);
            LMotion.Create(transform.localScale, Vector3.one * _baseScale, duration).WithEase(Ease.OutBack).WithDelay(delay)
                .Bind(this, (s, c) => c.transform.localScale = s);
            _backgroundView.AnimateIn(duration, delay);
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

        public void PunchPress()
        {
            if (_punchHandle.IsActive()) _punchHandle.Cancel();

            Vector3 baseScale = Vector3.one * _baseScale;
            Vector3 punchScale = baseScale * _punchScale;
            _punchHandle = LSequence.Create()
                .Append(LMotion.Create(transform.localScale, punchScale, _punchDuration).WithEase(Ease.OutQuad).Bind(this, (s, c) => c.transform.localScale = s))
                .Append(LMotion.Create(punchScale, baseScale, _punchDuration).WithEase(Ease.InQuad).Bind(this, (s, c) => c.transform.localScale = s))
                .Run();
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
            _backgroundView.SetSortOrder(order);
            _characterView.SetSortOrder(order + 1);
            _hintView.SetSortOrder(order + 1);
            _wrongView.SetSortOrder(order + 1);
        }

        public void SetSortingLayer(string layerName)
        {
            _backgroundView.SetSortingLayer(layerName);
            _characterView.SetSortingLayer(layerName);
            _hintView.SetSortingLayer(layerName);
            _wrongView.SetSortingLayer(layerName);
        }
    }
}
