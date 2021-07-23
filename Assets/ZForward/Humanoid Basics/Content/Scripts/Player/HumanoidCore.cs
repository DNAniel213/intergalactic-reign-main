/*
 * HumanoidCore.cs - Basement Media
 * Core script, this can be managed by PlayerController or NpcController
 * @version: 1.0.0
*/

using System;
using System.Collections;
using Humanoid_Basics.Weapon;
using UnityEngine;
using Mirror;
namespace Humanoid_Basics.Player
{
    [RequireComponent(typeof(HumanoidInventory))]


    [ExecuteInEditMode]
    public class HumanoidCore : NetworkBehaviour
    {

        [HideInInspector]
        public Transform aimHelper, aimHelperSpine;

        public enum Direction
        {
            Forward,
            Back,
            Up,
            Down,
            Left,
            Right
        }
        
        public enum Type
        {
            Player,
            Npc,
        }
        public Type humanoidType = Type.Player;
        
        public enum Status
        {
            Active,
            Swimming,
            Dead
        }
        public Status humanoidStatus = Status.Active;
        
        [HideInInspector]
        public HumanoidInventory humanoidInventory;

        [HideInInspector]
        public Animator playerAnimator;
        
        [HideInInspector]
        public RagdollHelper ragdollHelper;

        [HideInInspector]
        public Rigidbody rb;
        
        [HideInInspector]
        public CapsuleCollider capsuleCollider;
        
        public IKControl ikControl;

        [HideInInspector]
        public TransformPathMaker pathMaker;

        [HideInInspector]
        public Transform transformToRotate;

        [HideInInspector]
        public Vector3 moveAxis;

        [HideInInspector]
        public Rigidbody[] boneRb;

        [HideInInspector]
        public Transform hipsParent;
        
        private PhysicMaterial pM;

        [Header("Player Settings")]
        public bool canBox = true;
        public float boxingAimOffset = -20f;
        
        [HideInInspector]
        public bool isAttacking = false;
        public LayerMask groundLayers;

        [SerializeField]
        public float crouchSpeed = 1f, walkSpeed = 2.3f, runSpeed = 4.6f, aimWalkSpeed = 1.5f;
        [SyncVar]
        public bool crouch;
        [HideInInspector]
        public bool aim;
        public float jumpForce = 7;

        public float switchWeaponTime = .5f;
        public bool ragdollWhenFall = true;
        private float characterHeight = 1;
        [Range(0, 2)]
        public float crouchHeight = 0.75f;
        [Range(-1, 1)]
        public float bellyOffset = 0;

        [Header("Change this if holding weapons looks weird")]
        [SyncVar]
        public Direction spineFacingDirection;
        
        [HideInInspector]
        public AudioSource audioSource;

        // WEAPON STUFF
        public event Action OnWeaponSwitch;
        public event Action OnWeaponShoot;
        private bool equippedBefore;

        private float climbY;
        private float xAxis, yAxis;
        [HideInInspector]
        public Quaternion rotationAux, aimRotationSpineAux, aimRotationAux;

        [HideInInspector]
        public float lean;
        [HideInInspector]
        public float recoil;
        private float capsuleSize;
        [SyncVar]
        public float currentMovementState;
        [SyncVar]
        public bool isRunning;
        [HideInInspector]
        public float runKeyPressed;
        private AnimatorStateInfo currentAnimatorState;
        [HideInInspector]
        public bool grounded, inMoveState, climbing, climbHit, switchingWeapons, halfSwitchingWeapons;

        [HideInInspector]
        public WeaponBase currentWeapon;

        [HideInInspector]
        public int currentWeaponID;

        [HideInInspector]
        public bool equippedWeapon;

        [HideInInspector]
        public Transform leftHandInWeapon, rightHandInWeapon;
        
        [HideInInspector]
        public Quaternion startSpineRot = new Quaternion(0, 0, 0, 1);

        // Animation Hashes
        private static readonly int Move = Animator.StringToHash("Move");
        private static readonly int Climb1 = Animator.StringToHash("Climb");
        private static readonly int AxisX = Animator.StringToHash("AxisX");
        private static readonly int AxisY = Animator.StringToHash("AxisY");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int HoldingWeapon = Animator.StringToHash("HoldingWeapon");
        private static readonly int State = Animator.StringToHash("State");
        private static readonly int Property = Animator.StringToHash("Jump Forward");
        private static readonly int CanAttack = Animator.StringToHash("CanAttack");
        private static readonly int CanAttackCombo = Animator.StringToHash("CanAttackCombo");
        private static readonly int CanAttackFinish = Animator.StringToHash("CanAttackComboFinish");

        public void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            if (!Application.isPlaying) return;
            // startSpineRot = aimHelperSpine.rotation;
            // startSpineRot = aimHelper.rotation;
            //startSpineRot = new Quaternion(0.2f, -0.2f, 0, 1);
            rotationAux = new Quaternion(0, 0, 0, 1);
            aimRotationAux = rotationAux;
            aimRotationSpineAux = rotationAux;
            crouch = false;
            halfSwitchingWeapons = true;

            humanoidInventory = GetComponent<HumanoidInventory>();
            rb = playerAnimator.GetComponent<Rigidbody>();
            capsuleCollider = playerAnimator.GetComponent<CapsuleCollider>();
            pathMaker = playerAnimator.GetComponent<TransformPathMaker>();

            foreach (var r in boneRb)
            {
                var bc = r.GetComponent<BoxCollider>();
                var sc = r.GetComponents<SphereCollider>();
                if (bc != null) {
                    Physics.IgnoreCollision(capsuleCollider, bc);
                }

                if (sc == null) continue;
                foreach (var s in sc)
                {
                    Physics.IgnoreCollision(capsuleCollider, s);
                }
            }
            pM = capsuleCollider.material;
        }
        
        private void Update()
        {
            
            if (!Application.isPlaying) { return; }
            //if (Input.GetMouseButtonDown(0)) { Cursor.lockState = CursorLockMode.Locked; }
            Climb();
            RagdollWhenFall();
            StandUp();
            if(isLocalPlayer)
            {
            AnimatorMovementState();
                PlayerMovement();
                //CmdPlayerMovement();
            }
            GroundCheck();
            Gravity();
        }
        
        private void LateUpdate()
        {
            if (!Application.isPlaying) { return; }

            if (humanoidType == Type.Npc) return;
            if (humanoidStatus == Status.Dead) { return; }
            
            // Recoil
            recoil = Mathf.Lerp(recoil, 0, 10 * Time.deltaTime);
            
            //
            if (humanoidType == Type.Npc)
            {
                    
                if (aim)
                {
                    Quaternion lookForward = playerAnimator.transform.rotation;
                    Quaternion aimRotation = Quaternion.LookRotation(aimHelper.position - aimHelper.position) ;
                    Quaternion newRotation = new Quaternion(aimHelper.rotation.x, aimHelper.rotation.y - playerAnimator.transform.rotation.y, aimHelper.rotation.z, 1);;
                    // newRotation.y *= lookForward.y;
                    // lookForward.y = 0;
                    
                    //aimRotationAux = Quaternion.Lerp(aimRotationAux, aimHelper.rotation, 10 * Time.deltaTime);
                    //aimHelperSpine.rotation = aimRotationAux;
                    // Get the new rotation based on the camera looking forward
                    //rotationAux = Quaternion.LookRotation((transformToRotate.position + lookForward) - transformToRotate.position);
                }
                    
                //transformToRotate.rotation = Quaternion.Lerp(playerAnimator.transform.rotation, rotationAux, 10f * Time.deltaTime);
            } 
            // else if (humanoidType == Type.Player)
            // {
            //
            //     if (aim)
            //     {
            //         var cameraCore = CameraCore.Instance;
            //         var cameraTransform = cameraCore.cameraObject.transform;
            //         var cameraTransformForward = cameraTransform.forward;
            //         var spineOffset = cameraTransformForward - cameraTransform.up / 5;
            //         var armsOffset = cameraTransformForward;
            //         
            //         if (cameraCore.targetOffset.x <= 0.0f)
            //         {
            //             armsOffset -= cameraTransform.right / 3 ;
            //         }
            //         spineOffset.y = Mathf.Clamp(spineOffset.y, -0.4f, 0.35f);
            //         
            //         if (SomethingInFront())
            //         {
            //             spineOffset.y = Mathf.Clamp(spineOffset.y, 0, 0f);
            //             armsOffset.y = Mathf.Clamp(armsOffset.y, 0, 1);
            //         }
            //         
            //         // if (SomethingInFrontAim(2) && currentWeapon && !currentWeapon.reloadProgress)
            //         // {
            //         //     lean = cameraBehaviour.aimIsRightSide ? -.2f : .2f;
            //         // }
            //         // else
            //         // {
            //         //     lean = 0;
            //         // }
            //         
            //         if (crouch)
            //         {
            //             armsOffset.y = Mathf.Clamp(armsOffset.y, -.7f, 0.4f);
            //             if (cameraCore.targetOffset.x <= 0.0f)
            //             {
            //                 spineOffset -= cameraTransform.right * .3f;
            //             }
            //             else
            //             {
            //                 spineOffset += cameraTransform.right * .3f;
            //             }
            //             if (!SomethingInFront())
            //             {
            //                 spineOffset.y = Mathf.Clamp(spineOffset.y, -.5f, -.5f);
            //             }
            //         }
            //         
            //         aimRotationAux = Quaternion.Lerp(aimRotationAux, Quaternion.LookRotation((transformToRotate.position + armsOffset + cameraTransform.up * recoil / 10) - transformToRotate.position), 10 * Time.deltaTime);
            //
            //         if (canBox && !equippedWeapon) return;
            //         aimRotationSpineAux = Quaternion.Lerp(aimRotationSpineAux, Quaternion.LookRotation((transformToRotate.position + spineOffset + cameraTransform.up * recoil / 5) - transformToRotate.position) * new Quaternion(0, 0.5f, lean, 1) * startSpineRot, 10 * Time.deltaTime);
            //     }
            //     else
            //     {
            //         lean = 0;
            //
            //         aimRotationSpineAux = Quaternion.Lerp(aimRotationSpineAux, aimHelperSpine.rotation, 20 * Time.deltaTime);
            //
            //         var off = spineFacingDirection switch
            //         {
            //             Direction.Forward => aimHelperSpine.forward,
            //             Direction.Back => -aimHelperSpine.forward,
            //             Direction.Up => aimHelperSpine.up,
            //             Direction.Down => -aimHelperSpine.up,
            //             Direction.Left => -aimHelperSpine.right,
            //             Direction.Right => aimHelperSpine.right,
            //             _ => Vector3.zero
            //         };
            //
            //         off.y = Mathf.Clamp(off.y, 0, 5);
            //         if (crouch)
            //         {
            //             off -= transformToRotate.right * 0.3f;
            //         }
            //     
            //         aimRotationAux = Quaternion.Lerp(aimRotationAux, Quaternion.LookRotation((aimHelper.position + off) - aimHelper.position), 10 * Time.deltaTime);
            //
            //     }
            //
            //     if (playerAnimator.enabled)
            //         aimHelperSpine.rotation = aimRotationSpineAux;
            //
            // }

        }

        public void UseWeapon()
        {
            if (climbing || switchingWeapons || humanoidStatus == Status.Dead) return;

            if (!equippedWeapon)
            {
                // We can punch
                if (!playerAnimator.GetBool(CanAttack))
                {
                    isAttacking = true;
                    playerAnimator.SetBool(CanAttack, true);
                } 
                else if (playerAnimator.GetBool(CanAttack) && !playerAnimator.GetBool(CanAttackCombo) && !playerAnimator.GetBool(CanAttackFinish))
                {
                    isAttacking = true;
                    playerAnimator.SetBool(CanAttackCombo, true);
                } 
                else if (playerAnimator.GetBool(CanAttack) && playerAnimator.GetBool(CanAttackCombo) && !playerAnimator.GetBool(CanAttackFinish))
                {
                    isAttacking = true;
                    playerAnimator.SetBool(CanAttackFinish, true);
                }
                
            }
            else
            {
                currentWeapon.Shoot();
                OnWeaponShoot?.Invoke();
            }
        }

        public void ToggleWeapon()
        {
            if (switchingWeapons || !(humanoidInventory.WeaponCount() > 0) || climbing) return;
            EquipWeaponToggle();
        }

        public void ReloadWeapon()
        {
            if (!equippedWeapon || switchingWeapons) return;
            currentWeapon.Reload();
            OnWeaponShoot?.Invoke();
        }

        public void PlayerRun(bool run)
        {
            isRunning = run;
        }
        
        public void ToggleRun()
        {
            isRunning = !isRunning;
        }

        public void SetXAxis(float x)
        {
            xAxis = x;
        }
        
        public void SetYAxis(float y)
        {
            yAxis = y;
        }
        [Command]
        public void CmdPlayerMovement()
        {
            RpcPlayerMovement();
        }

        [TargetRpc]
        public void RpcPlayerMovement()
        {
            PlayerMovement();
        }
        public void PlayerMovement()
        {

            //if (humanoidStatus == Status.Dead) { return; }



            // This stays here
            if (ragdollHelper.state == RagdollHelper.RagdollState.blendToAnim)
            {
                transformToRotate.localPosition = Vector3.Lerp(transformToRotate.localPosition, Vector3.zero, 20 * Time.deltaTime);
            }

            if (inMoveState && !climbing)
            {
                
                // This stays here
                if (ragdollHelper.state == RagdollHelper.RagdollState.blendToAnim)
                {
                    transformToRotate.localPosition = Vector3.Lerp(transformToRotate.localPosition, Vector3.zero, 20 * Time.deltaTime);
                }

                if (humanoidType == Type.Npc)
                {
                    
                    if (aim)
                    {
                        Vector3 lookForward = transform.forward;
                        lookForward.y = 0;
                        


                        // Get the new rotation based on the camera looking forward
                        //rotationAux = Quaternion.LookRotation((transformToRotate.position + lookForward) - transformToRotate.position);
                    }
                    
                    //transformToRotate.rotation = Quaternion.Lerp(playerAnimator.transform.rotation, rotationAux, 10f * Time.deltaTime);
                }
                // else
                // {
                //     if (aim && equippedWeapon)
                //     {
                //         RaycastHit centerHit;
                //         Physics.Raycast(cameraBehaviour.cam.position, cameraBehaviour.cam.forward, out centerHit);
                //
                //         Debug.DrawLine(cameraBehaviour.cam.position, cameraBehaviour.cam.position + cameraBehaviour.cam.forward * (100f), Color.yellow);                        
                //         Debug.DrawRay(currentWeapon.barrel.position, centerHit.point  - currentWeapon.barrel.transform.position, Color.yellow);   
                //         
                //         //Renderer rend = centerHit.transform.GetComponent<Renderer>();
                //         //MeshCollider meshCollider = centerHit.collider as MeshCollider;
                //         
                //
                //     }
                //     
                // }
                
                // // As this is camera related i think this needs to be moved...
                // Vector3 orientedX = xAxis * cam.right;
                // Vector3 orientedY = yAxis * cam.forward;
                //
                // orientedX.y = 0;
                // orientedY.y = 0;
                //
                // moveAxis = orientedY + orientedX;
                // Vector3 lookForward = cam.forward;
                //
                // if (aim)
                // {
                //     if (!cameraBehaviour.aimIsRightSide && crouch)
                //     {
                //         lookForward -= cam.right / 2;
                //     }
                //     lookForward.y = 0;
                //
                //     // Get the new rotation based on the camera looking forward
                //     rotationAux = Quaternion.LookRotation((transformToRotate.position + lookForward) - transformToRotate.position);
                // }
                //
                // // Lets rotate the player animator to the new position if aiming.
                // transformToRotate.rotation = Quaternion.Lerp(playerAnimator.transform.rotation, rotationAux, 10f * Time.deltaTime);
                //
                // // If we are moving and not aiming then calculate rotation for the next frame...
                // if (moveAxis != Vector3.zero)
                // {
                //     if (!aim)
                //     {
                //         rotationAux = Quaternion.LookRotation((transformToRotate.position + moveAxis) - transformToRotate.position);
                //     }
                // }

                // SPEED CHANGE
                float speed = 0;
                if (currentMovementState < 0.5f)
                {
                    speed = crouchSpeed;

                }
                else if (currentMovementState < 1.5f)
                {
                    if (runKeyPressed > 1.5f && !crouch && !aim)
                    {
                        speed = runSpeed;

                    }
                    else
                    {
                        speed = walkSpeed;
                    }

                }
                else if (currentMovementState < 3.5f)
                {
                    speed = aimWalkSpeed;
                }

                // Handle movement based on move Axis
                if (!SomethingInFront())
                {
                    if (isRunning && moveAxis != Vector3.zero) { LerpSpeed(2); } else { LerpSpeed(1); }
                    
                    if (grounded)
                    {
                        var moveSpeed = moveAxis.normalized * speed;
                        rb.velocity = new Vector3(moveSpeed.x, rb.velocity.y, moveSpeed.z);
                    }

                }
                else
                {
                    if (aim)
                    {
                        if (grounded)
                        {
                            var moveSpeed = moveAxis.normalized * speed;
                            rb.velocity = new Vector3(moveSpeed.x, rb.velocity.y, moveSpeed.z);
                        }

                    }
                }
                Crouch();
                Aim();
            }
        }

        void EquipWeaponToggle()
        {
            equippedWeapon = !equippedWeapon;

            if (humanoidInventory.WeaponCount() > 0)
            {
                if (equippedWeapon)
                {
                    currentWeapon = GetCurrentWeapon();
                    leftHandInWeapon = currentWeapon.leftHand;
                    rightHandInWeapon = currentWeapon.rightHand;
                }
                currentWeapon.ToggleRenderer(equippedWeapon);
            }

            OnWeaponSwitch?.Invoke();
        }

        public void SwitchWeapon(int numberPressed)
        {
            if (numberPressed >= humanoidInventory.WeaponCount() || GetCurrentWeapon().reloadProgress) return;
            if (numberPressed != currentWeaponID)
            {
                if (!switchingWeapons)
                {
                    StartCoroutine(WeaponSwitchProgress(numberPressed));
                }
            }
            if (!equippedWeapon)
            {
                EquipWeaponToggle();
            }
        }

        private IEnumerator WeaponSwitchProgress(int numberP)
        {
            switchingWeapons = true;
            halfSwitchingWeapons = false;
            yield return new WaitForSeconds(switchWeaponTime/2);
            halfSwitchingWeapons = true;
            if (currentWeapon)
            {
                currentWeapon.ToggleRenderer(false);
            }
            currentWeaponID = numberP;
            currentWeapon = GetCurrentWeapon();
            OnWeaponSwitch?.Invoke();
            if (equippedWeapon)
            {
                currentWeapon.ToggleRenderer(true);
                leftHandInWeapon = currentWeapon.leftHand;
                rightHandInWeapon = currentWeapon.rightHand;
            }
            yield return new WaitForSeconds(switchWeaponTime);
            switchingWeapons = false;
        }

        private void AnimatorMovementState()
        {
            currentAnimatorState = playerAnimator.GetCurrentAnimatorStateInfo(0);
            inMoveState = currentAnimatorState.IsName("Grounded");
            playerAnimator.SetBool(Grounded, grounded);
            playerAnimator.SetFloat(Speed, runKeyPressed);
            playerAnimator.SetBool(HoldingWeapon, equippedWeapon);
            currentMovementState = playerAnimator.GetFloat(State);


            if (crouch)
            {
                playerAnimator.SetFloat(State, Mathf.Lerp(currentMovementState, 0, 5 * Time.deltaTime));
            }
            else if (aim)
            {
                if (canBox && !equippedWeapon)
                {
                    //playerAnimator.SetLayerWeight(1,Mathf.Lerp(boxingLayerWeight, 1, 5 * Time.deltaTime));
                    playerAnimator.SetFloat(State, Mathf.Lerp(currentMovementState, 3, 5 * Time.deltaTime));
                }
                else
                {
                    //playerAnimator.SetLayerWeight(1,Mathf.Lerp(boxingLayerWeight, 0, 5 * Time.deltaTime));
                    playerAnimator.SetFloat(State, Mathf.Lerp(currentMovementState, 2, 5 * Time.deltaTime));
                }
            }
            else
            {
                //playerAnimator.SetLayerWeight(1,Mathf.Lerp(boxingLayerWeight, 0, 5 * Time.deltaTime));
                playerAnimator.SetFloat(State, Mathf.Lerp(currentMovementState, 1, 5 * Time.deltaTime));
            }
            if (!SomethingInFront())
            {
                var m = Mathf.Clamp01(Mathf.Abs(xAxis) + Mathf.Abs(yAxis));
                playerAnimator.SetFloat(Move, Mathf.Lerp(playerAnimator.GetFloat(Move), m * runKeyPressed, 10 * Time.deltaTime));
                playerAnimator.SetFloat(AxisX, Mathf.Lerp(playerAnimator.GetFloat(AxisX), xAxis, 10 * Time.deltaTime));
                playerAnimator.SetFloat(AxisY, Mathf.Lerp(playerAnimator.GetFloat(AxisY), yAxis, 10 * Time.deltaTime));
            }
            else
            {
                if (aim)
                {
                    float _yAxis = yAxis;
                    _yAxis = Mathf.Clamp(_yAxis, -1, 0);

                    float _m = Mathf.Clamp01(Mathf.Abs(xAxis) + Mathf.Abs(_yAxis));

                    playerAnimator.SetFloat(Move, Mathf.Lerp(playerAnimator.GetFloat(Move), _m * runKeyPressed, 10 * Time.deltaTime));
                    playerAnimator.SetFloat(AxisX, Mathf.Lerp(playerAnimator.GetFloat(AxisX), xAxis, 10 * Time.deltaTime));
                    playerAnimator.SetFloat(AxisY, Mathf.Lerp(playerAnimator.GetFloat(AxisY), _yAxis, 10 * Time.deltaTime));
                }
                else
                {
                    playerAnimator.SetFloat(Move, Mathf.Lerp(playerAnimator.GetFloat(Move), 0, 5 * Time.deltaTime));
                    playerAnimator.SetFloat(AxisX, Mathf.Lerp(playerAnimator.GetFloat(AxisX), 0, 10 * Time.deltaTime));
                    playerAnimator.SetFloat(AxisY, Mathf.Lerp(playerAnimator.GetFloat(AxisY), 0, 10 * Time.deltaTime));
                }
            }
        }

        public bool SomethingInFront()
        {
            Vector3 posToDetect = transformToRotate.position + transformToRotate.up * .5f;
            return Physics.Raycast(posToDetect, transformToRotate.forward, 0.5f);
        }

        public void SetAim(bool newAim)
        {
            if (newAim == aim) return;
            if (climbing || !grounded || ragdollHelper.ragdolled) return;
            aim = newAim;
            if (equippedWeapon) currentWeapon.AimAudio();
        }
        
        public void ToggleAim()
        {
            if (equippedWeapon)
            {
                var currentW = currentWeapon;
                if (!currentW.reloadProgress && !currentW.shootProgress)
                {
                    SetAim(!aim);
                }
                else
                {
                    SetAim(true);
                }
            }
            else
            {
                SetAim(!aim);
            }
        }

        private void Aim()
        {
            if (!equippedWeapon || humanoidStatus == Status.Dead) return;
            currentWeapon.MoveTo(!aim ? transformToRotate : aimHelper);
            if (humanoidType == Type.Player)
            {
                aimHelper.rotation = aimRotationAux;
            }
        }

        public void SetCrouch()
        {
            if (humanoidStatus == Status.Dead) return;
            var somethingAbove = Physics.Raycast(transform.position + transform.up * .5f, transform.up, 1.4f);
            if (crouch && !somethingAbove)
            {
                crouch = false;
            }
            else
            {
                crouch = true;
            }
        }

        public void SetStand()
        {
            if (humanoidStatus == Status.Dead) return;
            var somethingAbove = Physics.Raycast(transform.position + transform.up * .5f, transform.up, 1.4f);
            if (crouch && !somethingAbove)
            {
                crouch = false;
            }
            
            if (!ragdollHelper.ragdolled) return;

            if (Physics.SphereCast(transform.position + transform.up * 1, 0.2f, -transform.up, out _, 3f))
            {
                ToggleRagdoll();
            }
        }

        private void Crouch()
        {
            if (humanoidStatus == Status.Dead) return;
            var somethingAbove = Physics.Raycast(transform.position + transform.up * .5f, transform.up, 1.4f);
            if (somethingAbove)
            {
                crouch = true;
            }

            capsuleSize = Mathf.Lerp(capsuleSize, crouch ? crouchHeight : characterHeight, 5 * Time.deltaTime);
            capsuleCollider.center = new Vector3(0, .9f * capsuleSize, 0);
            capsuleCollider.height = 1.8f * capsuleSize;
        }

        public void Jump()
        {
            var canJumpBasedOnWeapon = true;

            if (currentWeapon != null){

                if (currentWeapon.reloadProgress)
                {
                    canJumpBasedOnWeapon = false;
                }
            }

            if (grounded && inMoveState && !climbing && !crouch && !climbHit && !aim && !ragdollHelper.ragdolled && canJumpBasedOnWeapon)
            {
                playerAnimator.SetTrigger(Property);
                if (moveAxis != Vector3.zero && !SomethingInFront())
                {
                    rb.velocity = transformToRotate.up * jumpForce + transformToRotate.forward * 4;
                }
                else
                {
                    rb.velocity = transformToRotate.up * jumpForce / 1.1f;
                }
            }
        }

        private void Climb()
        {
            if (crouch) return;
            var canClimbBasedOnWeapon = true;

            if (currentWeapon != null)
            {
                if (currentWeapon.reloadProgress)
                {
                    canClimbBasedOnWeapon = false;
                }
            }
            
            var climbRayPos = playerAnimator.transform.position + transformToRotate.forward * 0.45f + transformToRotate.up * (2.1f * characterHeight);
            if (Physics.Raycast(climbRayPos, -playerAnimator.transform.up, out var hit, 1.8f) && !ragdollHelper.ragdolled && canClimbBasedOnWeapon)
            {
                climbHit = true;

                climbY = hit.point.y;
                var dist = climbY - playerAnimator.transform.position.y;

                if (hit.collider.CompareTag("Climbable"))
                {

                    var right = transformToRotate.right;
                    var forward = transformToRotate.forward;
                    ikControl.leftHandPos = hit.point + right * -0.3f + forward * -0.3f;
                    ikControl.rightHandPos = hit.point + right * 0.3f + forward * -0.3f;
                    
                    if (pathMaker.play == false)
                    {
                        equippedBefore = equippedWeapon;
                        if (equippedWeapon)
                        {
                            EquipWeaponToggle();
                        }

                        if (dist > 1f && dist < 1.8f)
                        {
                            climbing = true;
                            aim = false;
                            playerAnimator.SetTrigger(Climb1);
                            
                            pathMaker.pointsTime[0] = Vector3.Distance(playerAnimator.transform.position, pathMaker.points[0]);
                            pathMaker.points[0].y = climbY - 1.5f;

                            pathMaker.pointsTime[1] = 1;
                            pathMaker.points[1].y = climbY + 0.8f;
                            pathMaker.points[1].z = 1f;

                            pathMaker.pointsTime[2] = 1;
                            pathMaker.points[2].y = climbY + 1.3f;
                            pathMaker.points[2].z = 1f;
                            pathMaker.Play();
                            return;
                        }
                    }
                        
                }
                if (climbing)
                {

                }
            }
            else
            {
                climbHit = false;
                climbing = false;
                if (equippedBefore)
                {
                    equippedBefore = false;
                    EquipWeaponToggle();
                }
            }
        }

        public void StandUp()
        {
            if (boneRb[0].transform.parent == null && ragdollHelper.ragdolled)
            {
                transform.position = boneRb[0].position;
            }
        }

        public void ToggleRagdoll()
        {
            if(boneRb[0].velocity.magnitude > 1) { return; }
            var ragdoll = !ragdollHelper.ragdolled;
            
            foreach (var r in boneRb)
            {
                if (ragdoll == false) {
                    capsuleCollider.enabled = true;
                    ragdollHelper.ragdolled = false;
                    r.isKinematic = true;
                    r.velocity = Vector3.zero;
                    boneRb[0].transform.parent = hipsParent;
                    // if (humanoidType == Type.Npc)
                    // {
                    //     NpcBehaviour npc = GetComponent<NpcBehaviour>();
                    //     npc.aiType = NpcBehaviour.AITarget.Waypoints;
                    // }
                    //cameraParent.parent = transform;
                }
                else 
                {
                    // if (humanoidType == Type.Npc)
                    // {
                    //     NpcBehaviour npc = GetComponent<NpcBehaviour>();
                    //     npc.aiType = NpcBehaviour.AITarget.Idle;
                    // }
                    if (equippedWeapon) EquipWeaponToggle();
                    crouch = false;
                    ragdollHelper.ragdolled = true;
                    aim = false;
                    pathMaker.Reset();
                    rb.useGravity = false;
                    r.isKinematic = false;
                    r.velocity = rb.velocity * 1.5f;
                    playerAnimator.SetFloat(Move, 0);
                    playerAnimator.enabled = false;
                    rb.velocity = Vector3.zero;
                    rb.isKinematic = true;
                    capsuleCollider.enabled = false;
                    boneRb[0].transform.parent = null;
                    //cameraParent.parent = null;
                }
            }
        }

        public void RagdollWhenFall()
        {
            if (ragdollHelper.ragdolled || !ragdollWhenFall) return;
            if (rb.velocity.y < -15)
            {
                ToggleRagdoll();
            }
        }

        private void GroundCheck()
        {
            if(Physics.SphereCast(playerAnimator.transform.position + playerAnimator.transform.up * 2, .15f, -playerAnimator.transform.up, out _, 2.5f, groundLayers))
            {
                grounded = true;
                if (moveAxis == Vector3.zero || ragdollHelper.state == RagdollHelper.RagdollState.blendToAnim)
                {
                    pM.staticFriction = 3;
                    pM.dynamicFriction = 3;
                }
                else
                {
                    pM.staticFriction = 0;
                    pM.dynamicFriction = 0;
                }
            }
            else
            {
                grounded = false;
                pM.staticFriction = 0;
                pM.dynamicFriction = 0;
            }
        }

        private void LerpSpeed(float final)
        {
            runKeyPressed = Mathf.Lerp(runKeyPressed, final, 10 * Time.deltaTime);
        }

        private void Gravity()
        {
            if (ragdollHelper.state != RagdollHelper.RagdollState.animated) return;
            Vector3 velocity = rb.velocity;
            velocity.y -= 10F * Time.deltaTime;
            rb.velocity = velocity;
        }

        public WeaponBase GetCurrentWeapon()
        {
            return humanoidInventory.WeaponCount() > 0 ? humanoidInventory.weapons[currentWeaponID] : null;
        }
    }
}