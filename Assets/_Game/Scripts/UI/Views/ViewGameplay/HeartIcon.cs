using UnityEngine;
using UnityEngine.UI;

namespace Cast.Game
{
    public sealed class HeartIcon : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Color _emptyColor;
        [SerializeField] private Color _filledColor;

        public void SetFilled(bool filled)
        {
            if (_image == null) return;

            _image.color = filled ? _filledColor : _emptyColor;
        }
    }
}
