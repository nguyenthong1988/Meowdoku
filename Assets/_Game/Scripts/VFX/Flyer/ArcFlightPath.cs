using UnityEngine;

namespace Cast.Game
{
    public sealed class ArcFlightPath : FlightPath
    {
        private readonly float _arcHeight;
        private readonly float _scatter;
        private readonly Vector3 _offset;

        public ArcFlightPath(float arcHeight, float scatter = 0f)
        {
            _arcHeight = arcHeight;
            _scatter = scatter;
            _offset = Vector3.zero;
        }

        private ArcFlightPath(float arcHeight, float scatter, Vector3 offset)
        {
            _arcHeight = arcHeight;
            _scatter = scatter;
            _offset = offset;
        }

        public override Vector3 Evaluate(Vector3 from, Vector3 to, float t)
        {
            Vector3 midpoint = (from + to) * 0.5f;
            Vector3 control = midpoint + Vector3.up * _arcHeight + _offset;

            float inverse = 1f - t;
            return inverse * inverse * from + 2f * inverse * t * control + t * t * to;
        }

        public override FlightPath CreateVariant()
        {
            Vector3 randomOffset = _scatter > 0f
                ? (Vector3)(Random.insideUnitCircle * _scatter)
                : Vector3.zero;
            return new ArcFlightPath(_arcHeight, _scatter, randomOffset);
        }
    }
}
