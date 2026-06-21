using UnityEngine;

namespace Cast.Game.Board
{

    public readonly struct BoardLayout
    {
        public readonly int Size;
        public readonly float CellSize;

        public readonly Vector2 Origin;

        public BoardLayout(int size, float cellSize, Vector2 origin)
        {
            Size = size;
            CellSize = cellSize;
            Origin = origin;
        }

        public Vector3 CellToWorld(int row, int col) =>
            new Vector3(Origin.x + col * CellSize, Origin.y - row * CellSize, 0f);

        public bool WorldToCell(Vector3 world, out int row, out int col)
        {
            col = Mathf.RoundToInt((world.x - Origin.x) / CellSize);
            row = Mathf.RoundToInt((Origin.y - world.y) / CellSize);
            return row >= 0 && row < Size && col >= 0 && col < Size;
        }

        public static BoardLayout Fit(int size, Camera cam, float padding)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            float usable = 2f * (Mathf.Min(halfWidth, halfHeight) - padding);
            float cellSize = size > 0 ? usable / size : usable;

            Vector3 camPos = cam.transform.position;
            float half = (size - 1) * 0.5f * cellSize;
            var origin = new Vector2(camPos.x - half, camPos.y + half);
            return new BoardLayout(size, cellSize, origin);
        }
    }
}
