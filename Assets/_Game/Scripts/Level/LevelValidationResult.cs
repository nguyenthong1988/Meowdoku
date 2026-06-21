using System.Collections.Generic;
using System.Text;

namespace Cast.Game.Level
{

    public enum LevelRule
    {
        
        BoardSize,

        BoardDataLength,

        CharacterEncoding,

        Malformed,

        PaletteRange,

        EmptyCell,

        OneCatPerColor,

        OneCatPerRegion,

        NoSharedRow,

        NoSharedColumn,

        NoContact,
    }

    public readonly struct LevelIssue
    {
        public readonly LevelRule Rule;
        public readonly string Message;

        public LevelIssue(LevelRule rule, string message)
        {
            Rule = rule;
            Message = message;
        }

        public override string ToString() => $"[{Rule}] {Message}";
    }

    public sealed class LevelValidationResult
    {
        private readonly List<LevelIssue> _issues = new List<LevelIssue>();

        public bool IsValid => _issues.Count == 0;

        public IReadOnlyList<LevelIssue> Issues => _issues;

        public void Add(LevelRule rule, string message) =>
            _issues.Add(new LevelIssue(rule, message));

        public string Summary()
        {
            if (_issues.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < _issues.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(_issues[i].ToString());
            }
            return sb.ToString();
        }
    }
}
