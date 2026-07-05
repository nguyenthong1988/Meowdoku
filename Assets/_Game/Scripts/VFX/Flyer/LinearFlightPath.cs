using UnityEngine;

namespace Cast.Game
{
    public sealed class LinearFlightPath : FlightPath
    {
        public override Vector3 Evaluate(Vector3 from, Vector3 to, float t)
        {
            return Vector3.LerpUnclamped(from, to, t);
        }
    }
}
