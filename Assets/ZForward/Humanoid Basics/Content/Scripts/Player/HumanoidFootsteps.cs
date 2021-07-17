/*
 * HumanoidFootsteps.cs - Basement Media
 * @version: 1.0.0
*/

using System;
using Humanoid_Basics.Core;
using UnityEngine;

namespace Humanoid_Basics.Player
{
    [RequireComponent(typeof(HumanoidCore))]
    public class HumanoidFootsteps : MonoBehaviour
    {
        [Header("Humanoid Core")]
        public HumanoidCore humanoidCore;
        
        [Header("Audio")]
        public AudioClip defaultAudioClip;
        public AudioClip dirtAudioClip;
        public AudioClip grassAudioClip;
        public AudioClip stoneAudioClip;
        public AudioClip metalAudioClip;
        public AudioClip snowAudioClip;

        [Header("Feet")]
        public Transform leftFoot, rightFoot;
        private bool leftCanStep;

        // [HideInInspector]
        public string groundType;
        public float distance;
        public float factor = 0.65f;
        public float hitDistance = 100000f;

        // Start is called before the first frame update
        private void Start()
        {
            humanoidCore = GetComponent<HumanoidCore>();
        }

        // Update is called once per frame
        private void Update()
        {
            FootStepAudio();
        }

        private void FootStepAudio()
        {
            if(!humanoidCore.grounded) { return; }
            if (humanoidCore.climbing) { return; }
            distance = Vector3.Distance(leftFoot.position, rightFoot.position);
            if(distance > factor) leftCanStep = true;
            if (!leftCanStep || !(distance < factor)) return;
            leftCanStep = false;
            
            // TODO: Detect floor type (Grass, Dirt, Metal etc)
            var transform1 = transform;
            Vector3 rayPosition = transform1.position;
            rayPosition.y = rayPosition.y + 0.5f;
            var audioClip = defaultAudioClip;
            groundType = "Default";
            var ray = new Ray(rayPosition,  Vector3.down);
            // const int layerMask = 6;
            LayerMask layerMask = LayerMask.GetMask("Ground");
            if (Physics.Raycast(ray, out var hit, hitDistance, layerMask))
            {
                Debug.Log("Hit: "+hit.transform.gameObject.layer);
                Debug.Log("Layer Mask: "+layerMask.value);
                if (hit.transform.gameObject.layer == 6)
                {
                    Debug.Log("Layer Mask Confirmed: "+layerMask);
                    
                    var floorType = hit.transform.gameObject.GetComponent<FloorType>().type;
                    Debug.Log("Layer Mask Confirmed: "+floorType.ToString());
                    switch (floorType)
                    {
                        case FloorType.Types.Dirt:
                            audioClip = dirtAudioClip;
                            groundType = "Dirt";
                            break;
                        case FloorType.Types.Grass:
                            audioClip = grassAudioClip;
                            groundType = "Grass";
                            break;
                        case FloorType.Types.Metal:
                            audioClip = metalAudioClip;
                            groundType = "Metal";
                            break;
                        case FloorType.Types.Snow:
                            audioClip = snowAudioClip;
                            groundType = "Snow";
                            break;
                        case FloorType.Types.Stone:
                            audioClip = stoneAudioClip;
                            groundType = "Stone";
                            break;
                        case FloorType.Types.Default:
                            audioClip = defaultAudioClip;
                            groundType = "Default";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            
            humanoidCore.audioSource.PlayOneShot(audioClip);
        }
    }
}
