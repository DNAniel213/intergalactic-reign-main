/*
 * PlayerController.cs - Basement Media
 * @version: 1.0.0
*/

using System;
using UnityEngine;
using UnityEngine.Serialization;
using Humanoid_Basics.Camera;
using Mirror;

namespace Humanoid_Basics.Player
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Humanoid Core")]
        public HumanoidCore humanoidCore;
        
        [Header("Humanoid Plugins")]
        public HumanoidEquipment humanoidEquipment;
        public HumanoidPickup humanoidPickup;

        [HideInInspector]
        public CameraCore cameraCore;

        [Header("Options")]
        public bool canSwitchAimSide = true;
        public bool autoLean = true;
        public AimMode aimMode = AimMode.Hold;
        public float aimCameraDifference = 0.3f;
        public float aimIsRightSide;
        public bool useDebug;
        
        public RunMode runMode = RunMode.Hold;

        // Keyboard
        [Header("Controls")]
        [FormerlySerializedAs("JumpKey")] public KeyCode jumpKey = KeyCode.Space;
        [FormerlySerializedAs("RunKey")] public KeyCode runKey = KeyCode.LeftShift;
        [FormerlySerializedAs("CrouchKey")] public KeyCode crouchKey = KeyCode.C;
        [FormerlySerializedAs("ShootKey")] public KeyCode shootKey = KeyCode.Mouse0;
        [FormerlySerializedAs("AimKey")] public KeyCode aimKey = KeyCode.Mouse1;
        [FormerlySerializedAs("SwitchAimSideKey")] public KeyCode switchAimSideKey = KeyCode.T;
        [FormerlySerializedAs("ReloadKey")] public KeyCode reloadKey = KeyCode.R;
        [FormerlySerializedAs("PickUpWeaponKey")] public KeyCode pickUpWeaponKey = KeyCode.E;
        [FormerlySerializedAs("EquipWeaponKey")] public KeyCode equipWeaponKey = KeyCode.Tab;
        public float mouseSensitivity = 3;
        
        //
        public Vector2 movementAxis;
        public Vector2 cameraAxis;

        [HideInInspector]
        public KeyCode[] keyCodes = {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4,
            KeyCode.Alpha5,
            KeyCode.Alpha6,
            KeyCode.Alpha7,
            KeyCode.Alpha8,
            KeyCode.Alpha9,
        };

        // Plugins
        private bool loadedEquipmentPlugin;
        private bool loadedPickupPlugin;
        private bool sideCollision, currentSideCollision, oppositeSideCollision;
        public float originalSide;
        
        public enum AimMode
        {
            Hold,
            Toggle
        }
        
        public enum RunMode
        {
            Hold,
            Toggle
        }
        
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            loadedEquipmentPlugin = humanoidEquipment != null;
            if (loadedEquipmentPlugin)
            {
                Debug.Log("[PlayerController] Loaded Equipment Plugin");
            }
            
            loadedPickupPlugin = humanoidPickup != null;
            if (loadedPickupPlugin)
            {
                Debug.Log("[PlayerController] Loaded Pickup Plugin");
            }
            
            cameraCore = CameraCore.Instance;
            originalSide = cameraCore.targetOffset.x;
        }

        private void Update()
        {
            if (!Application.isPlaying) { return; }
            if (Input.GetMouseButtonDown(0)) { Cursor.lockState = CursorLockMode.Locked; }
            
            if (!humanoidCore.isAttacking)
            {
                movementAxis.x = Input.GetAxisRaw("Horizontal");
                movementAxis.y = Input.GetAxisRaw("Vertical");
                humanoidCore.SetXAxis(Input.GetAxisRaw("Horizontal"));
                humanoidCore.SetYAxis(Input.GetAxisRaw("Vertical"));

                // Update Camera 2.0
                cameraCore.SetCameraX(Input.GetAxisRaw("Mouse X") * mouseSensitivity);
                cameraCore.SetCameraZ(Input.GetAxisRaw("Mouse Y") * -mouseSensitivity);
            }
            else
            {
                humanoidCore.SetXAxis(0);
                humanoidCore.SetYAxis(0);

                // Update Camera 2.0
                cameraCore.SetCameraX(0);
                cameraCore.SetCameraZ(0);
            }



            if(isLocalPlayer)
            {
                HandleCameraTarget();
                PlayerMovement();
                HandleKeyPress();

                CmdPlayerMovement();
                //CmdHandleKeyPress();
            }
            
            // Debug Aim
            if (useDebug && humanoidCore.aim && humanoidCore.equippedWeapon)
            {
                var cameraTransform = cameraCore.cameraObject.transform;
                var cameraPosition = cameraTransform.position;
                var cameraForward = cameraTransform.forward;
                var color = Color.yellow;
                Physics.Raycast(cameraPosition, cameraForward, out var centerHit);
                Debug.DrawLine(cameraPosition, cameraPosition + cameraForward * (100f), color);                        
                Debug.DrawRay(humanoidCore.currentWeapon.barrel.position, centerHit.point  - humanoidCore.currentWeapon.barrel.transform.position, color);
            }
            
        }

        private void LateUpdate()
        {
            if(isLocalPlayer)
            {
                LocalLateUpdate();
                //CmdLateUpdate();
            }
        }   
        [Command]
        void CmdLateUpdate()
        {
            RpcLateUpdate();
        }

        [ClientRpc]
        private void RpcLateUpdate()
        {
            LocalLateUpdate();
        }

        private void LocalLateUpdate()
        {
            if (!Application.isPlaying) { return; }

            if (humanoidCore.humanoidType == HumanoidCore.Type.Npc) return;
            if (humanoidCore.humanoidStatus == HumanoidCore.Status.Dead) { return; }
            
            if (humanoidCore.aim) 
            {
                var cameraTransform = cameraCore.cameraObject.transform;
                var cameraTransformForward = cameraTransform.forward;
                var spineOffset = cameraTransformForward - cameraTransform.up / 5;
                var armsOffset = cameraTransformForward;
                
                // Ik Head Look At Position
                if (humanoidCore.canBox && !humanoidCore.equippedWeapon)
                {
                    humanoidCore.ikControl.lookAtPosition = humanoidCore.ikControl.head.position + cameraTransform.right * humanoidCore.boxingAimOffset;
                }
                else
                {
                    humanoidCore.ikControl.lookAtPosition = humanoidCore.ikControl.head.position + humanoidCore.aimHelper.forward;
                }
                
                if (cameraCore.targetOffset.x <= 0.0f)
                {
                    armsOffset -= cameraTransform.right / 3 ;
                }
                spineOffset.y = Mathf.Clamp(spineOffset.y, -0.4f, 0.35f);
                
                if (humanoidCore.SomethingInFront())
                {
                    spineOffset.y = Mathf.Clamp(spineOffset.y, 0, 0f);
                    armsOffset.y = Mathf.Clamp(armsOffset.y, 0, 1);
                }
                
                // Auto Lean
                if (autoLean)
                {
                    if (SomethingInFrontAim(2) && humanoidCore.currentWeapon && !humanoidCore.currentWeapon.reloadProgress)
                    {
                        if (cameraCore.targetOffset.x > 0.0f)
                        {
                            humanoidCore.lean = -.2f;
                        }
                        else
                        {
                            humanoidCore.lean = .2f;
                        }
                    }
                    else
                    {
                        humanoidCore.lean = 0;
                    }
                }
                
                if (humanoidCore.crouch)
                {
                    armsOffset.y = Mathf.Clamp(armsOffset.y, -.7f, 0.4f);
                    if (cameraCore.targetOffset.x <= 0.0f)
                    {
                        spineOffset -= cameraTransform.right * .3f;
                    }
                    else
                    {
                        spineOffset += cameraTransform.right * .3f;
                    }
                    if (!humanoidCore.SomethingInFront())
                    {
                        spineOffset.y = Mathf.Clamp(spineOffset.y, -.5f, -.5f);
                    }
                }
                
                humanoidCore.aimRotationAux = Quaternion.Lerp(humanoidCore.aimRotationAux, Quaternion.LookRotation((humanoidCore.transformToRotate.position + armsOffset + cameraTransform.up * humanoidCore.recoil / 10) - humanoidCore.transformToRotate.position), 10 * Time.deltaTime);

                if (humanoidCore.canBox && !humanoidCore.equippedWeapon) return;
                humanoidCore.aimRotationSpineAux = Quaternion.Lerp(humanoidCore.aimRotationSpineAux, Quaternion.LookRotation((humanoidCore.transformToRotate.position + spineOffset + cameraTransform.up * humanoidCore.recoil / 5) - humanoidCore.transformToRotate.position) * new Quaternion(0, 0.5f, humanoidCore.lean, 1) * humanoidCore.startSpineRot, 10 * Time.deltaTime);
            }
            else
            {
                humanoidCore.lean = 0;
                
                // Ik Head Look At Position
                humanoidCore.ikControl.lookAtPosition = humanoidCore.ikControl.head.position + cameraCore.cameraObject.transform.forward;
                
                humanoidCore.aimRotationSpineAux = Quaternion.Lerp(humanoidCore.aimRotationSpineAux, humanoidCore.aimHelperSpine.rotation, 20 * Time.deltaTime);

                Vector3 off;
                switch (humanoidCore.spineFacingDirection)
                {
                    case HumanoidCore.Direction.Forward:
                        off = humanoidCore.aimHelperSpine.forward;
                        break;
                    case HumanoidCore.Direction.Back:
                        off = -humanoidCore.aimHelperSpine.forward;
                        break;
                    case HumanoidCore.Direction.Up:
                        off = humanoidCore.aimHelperSpine.up;
                        break;
                    case HumanoidCore.Direction.Down:
                        off = -humanoidCore.aimHelperSpine.up;
                        break;
                    case HumanoidCore.Direction.Left:
                        off = -humanoidCore.aimHelperSpine.right;
                        break;
                    case HumanoidCore.Direction.Right:
                        off = humanoidCore.aimHelperSpine.right;
                        break;
                    default:
                        off = Vector3.zero;
                        break;
                }

                off.y = Mathf.Clamp(off.y, 0, 5);
                if (humanoidCore.crouch)
                {
                    off -= humanoidCore.transformToRotate.right * 0.3f;
                }
            
                humanoidCore.aimRotationAux = Quaternion.Lerp(humanoidCore.aimRotationAux, Quaternion.LookRotation((humanoidCore.aimHelper.position + off) - humanoidCore.aimHelper.position), 10 * Time.deltaTime);

            }

            if (humanoidCore.playerAnimator.enabled)
                humanoidCore.aimHelperSpine.rotation = humanoidCore.aimRotationSpineAux;

        }

        [Command]
        void CmdPlayerMovement()
        {
            RpcPlayerMovement();
        }

        [ClientRpc]
        void RpcPlayerMovement()
        {
            PlayerMovement();
        }

        private void PlayerMovement()
        {

            if (humanoidCore.humanoidStatus == HumanoidCore.Status.Dead) { return; }

            if (humanoidCore.inMoveState && !humanoidCore.climbing)
            {
                var cameraObject = cameraCore.cameraObject;
                var cameraObjectTransform = cameraObject.transform;
                var cameraObjectTransformForward = cameraObjectTransform.forward;
                
                // As this is camera related i think this needs to be moved...
                var orientedX = movementAxis.x * cameraObjectTransform.right;
                var orientedY = movementAxis.y * cameraObjectTransformForward;

                orientedX.y = 0;
                orientedY.y = 0;

                humanoidCore.moveAxis = orientedY + orientedX;
                var lookForward = cameraObjectTransformForward;

                if (humanoidCore.aim)
                {
                    if (cameraCore.targetOffset.x <= 0.0f && humanoidCore.crouch)
                    {
                        lookForward -= cameraObject.transform.right / 2;
                    }
                    lookForward.y = 0;

                    // Get the new rotation based on the camera looking forward
                    humanoidCore.rotationAux = Quaternion.LookRotation((humanoidCore.transformToRotate.position + lookForward) - humanoidCore.transformToRotate.position);
                }
                
                // Lets rotate the player animator to the new position if aiming.
                humanoidCore.transformToRotate.rotation = Quaternion.Lerp(humanoidCore.playerAnimator.transform.rotation, humanoidCore.rotationAux, 10f * Time.deltaTime);

                // If we are moving and not aiming then calculate rotation for the next frame...
                if (humanoidCore.moveAxis != Vector3.zero)
                {
                    if (!humanoidCore.aim)
                    {
                        humanoidCore.rotationAux = Quaternion.LookRotation((humanoidCore.transformToRotate.position + humanoidCore.moveAxis) - humanoidCore.transformToRotate.position);
                    }
                }

            }
        }
        [Command]
        void CmdHandleCameraTarget()
        {
            RpcHandleCameraTarget();
        }
        [ClientRpc]
        void RpcHandleCameraTarget()
        {
            HandleCameraTarget();
        }

        private void HandleCameraTarget()
        {
            // Camera Parent Transform
            var cameraTransform = cameraCore.cameraObject.transform;
            var cameraTransformForward = cameraTransform.forward;
            var cameraTransformRight = cameraTransform.right;
            
            // Collision aim detection
            var startPoint = cameraCore.cameraPivot[0].position;
            currentSideCollision = Physics.SphereCast(startPoint, .2f, -cameraTransformForward + cameraTransformRight * (cameraCore.useTargetOffset?(cameraCore.targetOffset.x * 1):0), out _, cameraCore.targetDistance / 2);
            oppositeSideCollision = Physics.SphereCast(startPoint, .2f, -cameraTransformForward + cameraTransformRight * (cameraCore.useTargetOffset?(cameraCore.targetOffset.x * -1):0), out _, cameraCore.targetDistance / 2);
            sideCollision = humanoidCore.aim && currentSideCollision;
            
            // Check for collision while aiming
            if (sideCollision && !oppositeSideCollision)
            {
                originalSide = cameraCore.targetOffset.x;
                cameraCore.targetOffset.x *= -1;
            }
            else if(!humanoidCore.aim) 
            {
                cameraCore.targetOffset.x = originalSide;
            }
            
            // Check for ragdoll, if so lock camera to root bone, if not back to animator
            if (humanoidCore.ragdollHelper.ragdolled)
            {
                cameraCore.SetTarget(humanoidCore.boneRb[0].transform);
                cameraCore.useTargetOffset = false;
            }
            else
            {
                cameraCore.SetTarget(humanoidCore.playerAnimator.transform);
                cameraCore.useTargetOffset = true;
            }
        }
        [Command]
        void CmdHandleKeyPress()
        {
            RpcHandleKeyPress();
        }
        
        [ClientRpc]
        void RpcHandleKeyPress()
        {
            HandleKeyPress();
        }
        private void HandleKeyPress()
        {
            
            /////////////////////
            // Camera Controls //
            /////////////////////
            
            // Switch Camera Sides
            if (Input.GetKeyDown(switchAimSideKey) && canSwitchAimSide)
            {
                SwitchCameraSide();
            }
            
            ///////////////////////
            // Humanoid Controls //
            ///////////////////////
            
            // Handle Jump & Standing
            if (Input.GetKeyDown(jumpKey))
            {
                humanoidCore.Jump();
                humanoidCore.SetStand();
            }
            
            // Handle Crouch
            if (Input.GetKeyDown(crouchKey))
            {
                humanoidCore.SetCrouch();
            }
            
            // Handle Weapon Switch
            for (var i = 0; i < keyCodes.Length; i++)
            {
                if (!Input.GetKeyDown(keyCodes[i])) continue;
                humanoidCore.SwitchWeapon(i);
            }
            
            // Run
            if (runMode == RunMode.Hold)
            {
                humanoidCore.PlayerRun(Input.GetKey(runKey));
            }
            else if (Input.GetKeyDown(runKey) && runMode == RunMode.Toggle)
            {
                humanoidCore.ToggleRun();
            }
            
            // Aiming
            if (aimMode == AimMode.Hold)
            {
                humanoidCore.SetAim(Input.GetKey(aimKey) || (Input.GetKey(shootKey) && humanoidCore.equippedWeapon));
            } 
            else if (Input.GetKeyDown(aimKey) && aimMode == AimMode.Toggle)
            {
                humanoidCore.ToggleAim();
            }

            // Adjust Camera if we are pressing Aim
            if (humanoidCore.aim)
            {
                cameraCore.SetTargetDistanceModifier(aimCameraDifference);
            } 
            else
            {
                cameraCore.SetTargetDistanceModifier(0);
            }
            
            // Toggle Weapon
            if (Input.GetKeyDown(equipWeaponKey))
            {
                humanoidCore.ToggleWeapon();
            }            
            
            // Shoot
            if (Input.GetKey(shootKey) && humanoidCore.equippedWeapon)
            {
                humanoidCore.UseWeapon();
            }
            else if (Input.GetKeyDown(shootKey) && !humanoidCore.equippedWeapon)
            {
                humanoidCore.UseWeapon();
            }  
            
            // Reload
            if (Input.GetKeyDown(reloadKey))
            {
                humanoidCore.ReloadWeapon();
            }

            // Pick Up Item
            if (loadedPickupPlugin)
            {
                if (!humanoidPickup.automaticPickup && Input.GetKeyDown(pickUpWeaponKey))
                {
                    humanoidPickup.PickUpItem();
                }
            }

        }

        private bool SomethingInFrontAim(float distance)
        {
            var cameraObject = cameraCore.cameraObject;
            var cameraObjectTransform = cameraObject.transform;
            var camF = cameraObjectTransform.forward;
            var camRight = cameraObjectTransform.right;
            camF.y = 0;
            var offset = cameraCore.targetOffset.x <= 0.0f ? camRight * 0.15f : -camRight * 0.15f;
            var posToDetect = humanoidCore.transformToRotate.position + humanoidCore.transformToRotate.up * .5f;
            return Physics.Raycast(posToDetect, camF + offset, distance) && !Physics.Raycast(posToDetect + (offset * -5), camF + (offset * -5), distance);
        }

        private void SwitchCameraSide()
        {
            if (oppositeSideCollision) return;
            cameraCore.targetOffset.x *= -1;
            originalSide = cameraCore.targetOffset.x;
        }
        
    }
}
