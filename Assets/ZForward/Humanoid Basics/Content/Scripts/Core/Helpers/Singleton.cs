/*
 * Singleton.cs - Basement Media
 * @version: 1.0.0
*/
using UnityEngine;

namespace Humanoid_Basics.Core.Helpers {
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
        private static T _instance;
        public static bool IsInitialized => _instance != null;
        public static T Instance {
            get {
                if (_instance != null) return _instance;
                var instances = FindObjectsOfType<T>();

                if (instances.Length > 0) {
                    _instance = instances[0];
                }

                if (instances.Length > 1) {
                    Debug.LogError("[Humanoid_Basics.Core.Helpers.Singleton] Something went wrong - there should never be more than 1 singleton in the scene! The first instance found will be used, and all others will be destroyed.");
                    return _instance;
                }

                if (_instance != null) return _instance;
                Debug.LogError("[Humanoid_Basics.Core.Helpers.Singleton] Something went wrong - specified Singleton does not found!");
                return default;
            }
        }
    }
}