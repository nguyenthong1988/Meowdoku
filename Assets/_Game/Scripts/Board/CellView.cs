using Cast.Game.Gameplay;
using LitMotion;
using UnityEngine;

namespace Cast.Game.Board
{

    public sealed class CellView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _background;
        [SerializeField] private SpriteRenderer _cat;
        [SerializeField] private SpriteRenderer _mark;

        [SerializeField] private Color _catColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        [SerializeField] private Color _hintColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color _wrongColor = new Color(0.85f, 0.15f, 0.15f, 1f);

        private float _baseScale = 1f;

        public int Row { get; private set; }
        public int Col { get; private set; }

        public void SetCell(int row, int col, Color color, float cellSize)
        {
            Row = row;
            Col = col;
            _baseScale = cellSize;

            EnsureSprites();
            transform.localScale = Vector3.one * _baseScale;

            if (_background != null) _background.color = color;
            if (_cat != null) _cat.color = _catColor;
            SetMark(PlayerMark.None);
        }

        public void SetMark(PlayerMark mark)
        {
            if (_cat != null) _cat.enabled = mark == PlayerMark.Cat;
            if (_mark != null)
            {
                bool showMark = mark == PlayerMark.Hint || mark == PlayerMark.Wrong;
                _mark.enabled = showMark;
                if (showMark) _mark.color = mark == PlayerMark.Hint ? _hintColor : _wrongColor;
            }
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

        private void EnsureSprites()
        {
            if (_background != null && _background.sprite == null) _background.sprite = PlaceholderSprites.Square;
            if (_cat != null && _cat.sprite == null) _cat.sprite = PlaceholderSprites.Circle;
            if (_mark != null && _mark.sprite == null) _mark.sprite = PlaceholderSprites.Circle;
        }
    }
}
