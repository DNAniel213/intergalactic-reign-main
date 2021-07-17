/*
 * DestroyAfterTime.cs - Basement Media
 * @version: 1.0.0
*/
using UnityEngine;

namespace Humanoid_Basics.Core.Helpers
{
	public class DestroyAfterTime : MonoBehaviour {
		public float time;
		private void Start() {
			Destroy(gameObject, time);
		}
	}
}