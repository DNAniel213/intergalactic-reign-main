using Humanoid_Basics.Core.Helpers;
using UnityEngine;

namespace Humanoid_Basics.Camera
{
    public class CameraCore : Singleton<CameraCore>
    {

        // The main camera
        [HideInInspector]
        public UnityEngine.Camera cameraObject;
        
        // The target of the camera.
        public Transform target;
        
        // The offsets from the target of the camera.
        public Vector2 targetOffset = new Vector2(0.5f, 1.5f);
        
        // The distance from the target.
        public float targetDistance = 2;

        // The target modifiers
        [HideInInspector]
        public float targetDistanceModifier;
        [HideInInspector]
        public Vector3 targetPositionModifier;
        [HideInInspector]
        public Quaternion targetRotationModifier;

        // Reference to the camera pivot transforms.
        [HideInInspector]
        public Transform[] cameraPivot = new Transform[2];
        
        // Rotate the target to camera forward
        public bool useTargetOffset;

        // Private Vars
        private float currentCamDistance, cameraXAxis, cameraZAxis, cameraZClamp;

        private void Start()
        {
            // Find the camera attached to this GameObject
            cameraObject = GetComponentInChildren<UnityEngine.Camera>();
            currentCamDistance = targetDistance;
        }

        private void Update()
        {
            // Camera Parent Transform
            var cameraParentTransform = transform;
            var cameraTransform = cameraObject.transform;
            var cameraTransformForward = cameraTransform.forward;
            var cameraTransformRight = cameraTransform.right;

            // X Axis
            cameraPivot[0].localEulerAngles = new Vector3(0, cameraXAxis, 0);
            
            // Z Axis
            cameraZClamp = Mathf.Lerp(cameraZClamp, 70, 8 * Time.deltaTime);
            cameraZAxis = Mathf.Clamp(cameraZAxis, -60, cameraZClamp);
            cameraPivot[1].localEulerAngles = new Vector3(cameraZAxis, 0, 0);
            
            // Collision detection
            var startPoint = cameraPivot[0].position;
            if (Physics.SphereCast(startPoint, 0.1f, -cameraTransformForward, out var h, targetDistance/2) ||
                Physics.SphereCast(startPoint, 0.2f, -cameraTransformForward + cameraTransformRight * (targetOffset.x / 2), out h, targetDistance))
            {
                var dist = Vector3.Distance(cameraPivot[0].position, h.point) - targetDistanceModifier;
                currentCamDistance = Mathf.Clamp(dist, .1f, targetDistance);
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, new Vector3(0, 0, -currentCamDistance + 0.3f), 100f * Time.deltaTime);
            }
            else
            {
                currentCamDistance = targetDistance - targetDistanceModifier;
                cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, new Vector3(useTargetOffset?targetOffset.x:0, 0, -currentCamDistance), 10f * Time.deltaTime);
            }

            // Lock the camera position onto the target
            cameraParentTransform.position = target.transform.position + target.root.transform.up * (useTargetOffset?(targetOffset.y):0);

            // Adjust for rotation modifier
            cameraTransform.localRotation = Quaternion.Lerp(cameraTransform.localRotation, targetRotationModifier, 2 * Time.deltaTime);
        }

        public void SetTarget(Transform targetTransform)
        {
            target = targetTransform;
        }

        public void SetCameraX(float xAxis)
        {
            cameraXAxis += xAxis;
        }

        public void SetCameraZ(float zAxis)
        {
            cameraZAxis += zAxis;
        }

        public void SetTargetDistanceModifier(float modifier)
        {
            targetDistanceModifier = modifier;
        }

    }
}
