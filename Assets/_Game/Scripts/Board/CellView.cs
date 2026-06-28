using Cast.Game.Gameplay;
using LitMotion;
using UnityEngine;

namespace Cast.Game.Board
{

    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _cat;
        [SerializeField] private SpriteRenderer _markHint;
        [SerializeField] private SpriteRenderer _markWrong;
        [SerializeField] private SpriteAsset _spriteAsset;

        private float _baseScale = 1f;

        public int Row { get; private set; }
        public int Col { get; private set; }

        public void SetCell(int row, int col, int colorIndex, float cellSize)
        {
            Row = row;
            Col = col;
            _baseScale = cellSize;

            transform.localScale = Vector3.one * _baseScale;

            if (_background != null) _background.sprite = _spriteAsset.GetSprite(colorIndex) != null ? _spriteAsset.GetSprite(colorIndex) : PlaceholderSprites.Square;

            SetMark(PlayerMark.None);
        }

        public void SetMark(PlayerMark mark)
        {
            if (_cat != null) _cat.enabled = mark == PlayerMark.Cat;
            if (_markHint != null) _markHint.enabled = mark == PlayerMark.Hint;
            if (_markWrong != null) _markWrong.enabled = mark == PlayerMark.Wrong;
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
            LMotion.Create(_baseScale * 0.7f, _baseScale, 0.25f).WithEase(Ease.OutBack)
                .Bind(this, (s, c) => c.transform.localScale = Vector3.one * s);
        }

        public void PlayShake()
        {
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
    }
}
