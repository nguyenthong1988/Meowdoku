using UnityEngine;

namespace Cast.Game
{
    public abstract class FlightPath
    {
        public abstract Vector3 Evaluate(Vector3 from, Vector3 to, float t);

        public virtual FlightPath CreateVariant() => this;
    }
}
