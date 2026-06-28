using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game
{
    public sealed class HeartIcon : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Sprite _fullSprite;
        [SerializeField] private Sprite _emptySprite;
        [SerializeField] private Color _filledColor = Color.white;
        [SerializeField] private Color _emptyColor = new Color(1f, 1f, 1f, 0.25f);

        public void SetFilled(bool filled)
        {
            if (_image == null) return;

            if (_fullSprite != null && _emptySprite != null)
            {
                _image.sprite = filled ? _fullSprite : _emptySprite;
                _image.color = Color.white;
            }
            else
            {
                _image.color = filled ? _filledColor : _emptyColor;
            }
        }
    }
}
