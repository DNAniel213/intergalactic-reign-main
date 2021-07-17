using UnityEngine;

namespace Humanoid_Basics.Core
{
    public class FloorType: MonoBehaviour
    {
        public enum Types {Default, Dirt, Grass, Stone, Metal, Snow};
        
        public Types type;
    }
}
