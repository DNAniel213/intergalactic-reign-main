using UnityEngine;
using UnityEngine.Serialization;
using Mirror;

namespace Humanoid_Basics.Player
{
    public class IKControl : NetworkBehaviour
    {

        [Header("Humanoid Core")] public HumanoidCore humanoidCore;

        [Header("Hand IK")] public float defaultHandLerpSpeed = 6f;
        public float weaponHandLerpSpeed = 10f;
        public float climbHandLerpSpeed = 4f;

        [HideInInspector] public Vector3 rightHandPos, leftHandPos;

        [HideInInspector] public Quaternion rightHandRot, leftHandRot;
        public Animator animator;
        private float rightHandWeight, leftHandWeight, rightHandRotWeight, leftHandRotWeight, lookAtWeight;
        public Vector3 lookAtPosition;

        [HideInInspector] public Transform head;

        [Space] [Header("Foot IK")] public bool enableFeetIk = true;
        public bool enableFeetIkRotation = true;
        public bool enableFeetIkPelvisAdjustment = false;

        [Tooltip("Uses advanced animation curve in Animation")]
        public bool useAnimationCurve = true;

        [Range(0, 2)] [SerializeField] private float heightFromGroundRaycast = 0.5f;
        [Range(0, 2)] [SerializeField] private float raycastDownDistance = 0.35f;
        [SerializeField] public LayerMask environmentLayer;
        [SerializeField] private float pelvisOffset;
        [SerializeField] private float pelvisUpAndDownSpeed = 0.28f;
        [SerializeField] private float feetToIkPositionSpeed = 0.5f;
        private Vector3 rightFootPosition, leftFootPosition, leftFootIkPosition, rightFootIkPosition;
        private Quaternion leftFootIkRotation, rightFootIkRotation;
        private float lastPelvisPositionY, lastRightFootPositionY, lastLeftFootPositionY;

        public string leftFootAnimVariableName = "LeftFootCurve";
        public string rightFootAnimVariableName = "RightFootCurve";

        public bool debug = false;
        private bool isPlayerAnimatorNull;

        private void Start()
        {
            leftHandWeight = 0;
            rightHandWeight = 0;
            leftHandRotWeight = 0;
            rightHandRotWeight = 0;
            lookAtWeight = 0;
            isPlayerAnimatorNull = animator == null;
        }

        private void FixedUpdate()
        {
            if (enableFeetIk == false) return;

            if (isPlayerAnimatorNull) return;

            AdjustFeetTarget(ref rightFootPosition, HumanBodyBones.RightFoot);
            AdjustFeetTarget(ref leftFootPosition, HumanBodyBones.LeftFoot);

            FeetPositionSolver(rightFootPosition, ref rightFootIkPosition, ref rightFootIkRotation);
            FeetPositionSolver(leftFootPosition, ref leftFootIkPosition, ref leftFootIkRotation);
        }

        void OnAnimatorIK(int layerIndex)
        {
            //if(isLocalPlayer)
               // DoIK(layerIndex);
        }

       

        private void FeetPositionSolver(Vector3 skyPosition, ref Vector3 feetIkPosition, ref Quaternion feetIkRotation)
        {
            RaycastHit feetOutHit;

            if (debug)
            {
                Debug.DrawLine(skyPosition, skyPosition + Vector3.down * (raycastDownDistance + heightFromGroundRaycast),Color.yellow);
            }

            if (Physics.Raycast(skyPosition, Vector3.down, out feetOutHit,
                raycastDownDistance + heightFromGroundRaycast, environmentLayer))
            {
                feetIkPosition = skyPosition;
                feetIkPosition.y = feetOutHit.point.y + pelvisOffset;
                feetIkRotation = Quaternion.FromToRotation(Vector3.up, feetOutHit.normal) * transform.rotation;

                return;
            }
            
            feetIkPosition = Vector3.zero; //
        }

        private void AdjustFeetTarget(ref Vector3 feetPositions, HumanBodyBones foot)
        {
            feetPositions = animator.GetBoneTransform(foot).position;
            feetPositions.y = transform.position.y + heightFromGroundRaycast;
        }

         private void DoIK(int layerIndex)
        {
            if (!animator) return;

            if (humanoidCore.inMoveState)
            {
                if (humanoidCore.canBox && !humanoidCore.equippedWeapon)
                {
                    LerpLookAtWeight(0, 5);
                }
                else
                {
                    LerpLookAtWeight(.5f, 5);
                }
            }
            else
            {
                LerpLookAtWeight(0, 5);
            }

            if (humanoidCore.equippedWeapon)
            {
                LerpLeftHandWeight(humanoidCore.currentWeapon.usingLeftHand ? 1 : 0, weaponHandLerpSpeed);
                LerpRightHandWeight(1, weaponHandLerpSpeed);
                LerpLeftHandRotWeight(humanoidCore.currentWeapon.usingLeftHand ? 1 : 0, weaponHandLerpSpeed);
                LerpRightHandRotWeight(1, weaponHandLerpSpeed);
                leftHandRot = humanoidCore.leftHandInWeapon.rotation;
                rightHandRot = humanoidCore.rightHandInWeapon.rotation;
                leftHandPos = humanoidCore.leftHandInWeapon.position;
                rightHandPos = humanoidCore.rightHandInWeapon.position;
            }
            else if (humanoidCore.climbing)
            {
                LerpLeftHandWeight(1, climbHandLerpSpeed);
                LerpRightHandWeight(1, climbHandLerpSpeed);
                LerpLeftHandRotWeight(0, 100f);
                LerpRightHandRotWeight(0, 100f);
            }
            else
            {
                LerpLeftHandWeight(0, defaultHandLerpSpeed);
                LerpRightHandWeight(0, defaultHandLerpSpeed);
                LerpLeftHandRotWeight(0, defaultHandLerpSpeed);
                LerpRightHandRotWeight(0, defaultHandLerpSpeed);
            }

            // Set Head Position
            animator.SetLookAtPosition(lookAtPosition);
            animator.SetLookAtWeight(lookAtWeight);

            // Right Hand Position
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, rightHandWeight);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandPos);

            // Right Hand Rotation
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, rightHandRotWeight);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandRot);

            // Left Hand Position
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, leftHandWeight);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandPos);

            // Left Hand Rotation
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, leftHandRotWeight);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandRot);


            // Foot IK
            if (enableFeetIk)
            {
                var rightRootWeight = useAnimationCurve ? animator.GetFloat(rightFootAnimVariableName) : 1f;
                var leftRootWeight = useAnimationCurve ? animator.GetFloat(leftFootAnimVariableName) : 1f;

                if (CanMovePelvis())
                {
                    MovePelvisHeight();
                }
                
                animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightRootWeight);
                animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftRootWeight);
                if (enableFeetIkRotation)
                {
                    animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftRootWeight);
                    animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightRootWeight);
                }

                MoveFeetToIkPoint(AvatarIKGoal.RightFoot, rightFootIkPosition, rightFootIkRotation,
                    ref lastRightFootPositionY);
                MoveFeetToIkPoint(AvatarIKGoal.LeftFoot, leftFootIkPosition, leftFootIkRotation,
                    ref lastLeftFootPositionY);

                
            }


        }

        

        private bool CanMovePelvis()
        {
            if (!enableFeetIkPelvisAdjustment) return false;
            if ((humanoidCore.climbing || humanoidCore.ragdollHelper.ragdolled || !humanoidCore.grounded))
            {
                return false;
            }

            return true;
        }

        private void LerpLeftHandWeight(float to, float t)
        {
            leftHandWeight = Mathf.Lerp(leftHandWeight, to, t * Time.deltaTime);
        }

        private void LerpRightHandWeight(float to, float t)
        {
            rightHandWeight = Mathf.Lerp(rightHandWeight, to, t * Time.deltaTime);
        }

        private void LerpLeftHandRotWeight(float to, float t)
        {
            leftHandRotWeight = Mathf.Lerp(leftHandRotWeight, to, t * Time.deltaTime);
        }

        private void LerpRightHandRotWeight(float to, float t)
        {
            rightHandRotWeight = Mathf.Lerp(rightHandRotWeight, to, t * Time.deltaTime);
        }

        private void LerpLookAtWeight(float to, float t)
        {
            lookAtWeight = Mathf.Lerp(lookAtWeight, to, t * Time.deltaTime);
        }
        
        private void MoveFeetToIkPoint(AvatarIKGoal foot, Vector3 position, Quaternion rotation,
            ref float lastFootPositionY)
        {
            Vector3 targetIkPosition = animator.GetIKPosition(foot);

            if (position != Vector3.zero)
            {
                targetIkPosition = transform.InverseTransformPoint(targetIkPosition);
                position = transform.InverseTransformPoint(position);

                float yVariable = Mathf.Lerp(lastFootPositionY, position.y, feetToIkPositionSpeed);
                targetIkPosition.y += yVariable;

                lastFootPositionY = yVariable;

                targetIkPosition = transform.TransformPoint(targetIkPosition);
                
                animator.SetIKRotation(foot, rotation);
            }
            
            animator.SetIKPosition(foot, targetIkPosition);
        }

        private void MovePelvisHeight()
        {
            if (rightFootIkPosition == Vector3.zero || leftFootIkPosition == Vector3.zero || lastPelvisPositionY == 0)
            {
                lastPelvisPositionY = animator.bodyPosition.y;
                return;
            }

            float leftOffsetPosition = leftFootIkPosition.y - transform.position.y;
            float rightOffsetPosition = rightFootIkPosition.y - transform.position.y;

            float totalOffset = (leftOffsetPosition < rightOffsetPosition) ? leftOffsetPosition : rightOffsetPosition;

            Vector3 newPelvisPosition = animator.bodyPosition + Vector3.up * totalOffset;

            newPelvisPosition.y = Mathf.Lerp(lastPelvisPositionY, newPelvisPosition.y, pelvisUpAndDownSpeed);

            animator.bodyPosition = newPelvisPosition;
            
            lastPelvisPositionY = animator.bodyPosition.y;
        }
    }
}