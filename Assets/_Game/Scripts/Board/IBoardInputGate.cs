using System;
using System.Collections.Generic;

namespace Cast.Game
{

    public interface IBoardInput
    {
        BoardInputMode Mode { get; }
        void SetMode(BoardInputMode mode);
        void BeginHintPreview(IReadOnlyCollection<(int Row, int Col)> allowedCells, Action<int, int> onTap, Action<int, int> onDoubleTap);
        void EndHintPreview();
    }
}
