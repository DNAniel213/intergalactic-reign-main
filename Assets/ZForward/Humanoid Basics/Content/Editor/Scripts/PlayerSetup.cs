/*
 * PlayerSetup.cs - Basement Media
 * @version: 1.0.0
 *
 * @note: We DO NOT recommend editing this script
 * 
*/

using System;
using Humanoid_Basics.Camera;
using Humanoid_Basics.Core;
using Humanoid_Basics.Npc;
using Humanoid_Basics.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace Humanoid_Basics.Editor.Scripts
{
    public class PlayerSetup : EditorWindow
    {
        public Transform baseHumanoid;
        private GameObject humanoidCharacter;
        private float sizeFactor = 3;
        private float scaleFactor = 1;
        public HumanoidCore.Type humanoidType;
        
        private HumanoidCore.Direction spineFacingDirection;
        
        public enum AnimatorType
        {
            Civilian,
            Ninja,
            Zombie,
            Drunk,
            
            Custom
        }
        public AnimatorType humanoidAnimatorType;

        float legCenter;
        Vector3 armsCenter, armsLenght, legsLenght;
        RuntimeAnimatorController playerAnimator;
        UnityEditor.Editor gameObjectEditor;

        Animator _charAnim;
        GameObject _char;
        Vector3 hipsScale;
        string message = "";
        
        // Plugins
        // private bool includeHealth = true;
        // private bool includeInventory = true;
        // private bool includeFootsteps = true;
        
        [MenuItem("ZForward/Humanoid Basics/Humanoid Setup")]
        public static void ShowWindow()
        {
            var charSetup = (PlayerSetup)GetWindow(typeof(PlayerSetup), true, "Humanoid Basics", focusedWindow);
            charSetup.minSize = new Vector2(325, 565);
            charSetup.maxSize = new Vector2(325, 565);
        }

        [MenuItem("ZForward/Humanoid Basics/Add Camera To Scene")]
        public static GameObject AddCamera()
        {
            var camera = GameObject.FindGameObjectWithTag("Camera");
            if (camera != null) return camera;
            var cameraPrefab = Resources.Load("Prefabs/Base/Camera") as GameObject;
            var instantiatePrefab = PrefabUtility.InstantiatePrefab(cameraPrefab) as GameObject;
            return instantiatePrefab;
        }
        
        [MenuItem("ZForward/Humanoid Basics/Add GameManager To Scene")]
        public static GameObject AddGameManager()
        {
            var gameManager = GameObject.FindGameObjectWithTag("GameManager");
            if (gameManager != null) return gameManager;
            var gameManagerPrefab = Resources.Load("Prefabs/Base/GameManager") as GameObject;
            var instantiatePrefab = PrefabUtility.InstantiatePrefab(gameManagerPrefab) as GameObject;
            return instantiatePrefab;
        }

        private void OnGUI()
        {
            var titleTexture = Resources.Load("Editor/Textures/PlayerSetup") as Texture;
            GUILayout.Label(titleTexture, EditorStyles.largeLabel);
            //EditorGUILayout.HelpBox("Step 1. Select Humanoid Type Player/Npc. You can only ever add 1 player to the scene. Npc are still experimental. Selecting Npc will result in a new Humanoid each time.\n\nStep 2. Select Animation Type. We recommend Civilian right now while we iron out glitches.\n\nStep 3. Select your 3D model, normally in the format of an FBX. Make sure your FBX is game ready. This includes, scaling & Z-Forward. Most issues occur when models are not setup correctly. If you need help with this see our support button below.", MessageType.None);

            GUILayout.Space(10);
            GUILayout.Label("Humanoid Settings", EditorStyles.boldLabel);
            humanoidType = (HumanoidCore.Type)EditorGUILayout.EnumPopup("Type", humanoidType);
            EditorGUILayout.HelpBox("Humanoid Type is either a human player or AI (Npc). You can only ever add 1 player to the scene. Npc is still experimental.", MessageType.Info);
            GUILayout.Space(10);
            
            humanoidAnimatorType = (AnimatorType)EditorGUILayout.EnumPopup("Animator Type", humanoidAnimatorType);
            //EditorGUILayout.HelpBox("To change the base Animation Set. Animation Type is still experimental. We recommend Civilian.", MessageType.Info);
            GUILayout.Space(10);

            if (humanoidType == HumanoidCore.Type.Player)
            {
                //GUILayout.Space(10);
                //GUILayout.Label("Player Specific", EditorStyles.boldLabel);
                //baseHumanoid = EditorGUILayout.ObjectField("Player Transform", baseHumanoid, typeof(HumanoidCore), true) as Transform;

                if (baseHumanoid == null)
                {
                    var searchResult = SearchForPlayer();
                    if (searchResult != null)
                    {
                        baseHumanoid = searchResult.transform;
                        Selection.activeGameObject = baseHumanoid.gameObject;
                        SceneView.FrameLastActiveSceneView();
                    }
                }
            }
            else
            {
                baseHumanoid = null;
            }

            // GUILayout.Space(10);
            GUILayout.Label("Model Settings", EditorStyles.boldLabel);
            humanoidCharacter = EditorGUILayout.ObjectField("Humanoid (FBX)", humanoidCharacter, typeof(GameObject), false) as GameObject;

            scaleFactor = EditorGUILayout.Slider("Scale Factor", scaleFactor, 0.5f, 3);
            GUILayout.Space(10);
            GUILayout.Label("Ragdoll Settings", EditorStyles.boldLabel);
            sizeFactor = EditorGUILayout.Slider("Collider Scale Factor", sizeFactor, 0.5f, 5);

            // GUILayout.Space(10);
            // GUILayout.Label("Transform Direction", EditorStyles.boldLabel);
            // spineFacingDirection = (HumanoidCore.Direction)EditorGUILayout.EnumPopup("Direction:", spineFacingDirection);
            // EditorGUILayout.HelpBox("We do not recommend changing this. Instead import your model Z-Forward. This will affect how your humanoid holds a weapon.", MessageType.None);
            
            RuntimeAnimatorController pAnim;
            switch (humanoidAnimatorType)
            {
                case AnimatorType.Civilian:
                    pAnim = Resources.Load("Animators/PlayerAnimator") as RuntimeAnimatorController;
                    break;
                case AnimatorType.Ninja:
                    pAnim = Resources.Load("Animators/NinjaAnimator") as RuntimeAnimatorController;
                    break;
                case AnimatorType.Zombie:
                    pAnim = Resources.Load("Animators/ZombieAnimator") as RuntimeAnimatorController;
                    break;
                case AnimatorType.Drunk:
                    pAnim = Resources.Load("Animators/DrunkAnimator") as RuntimeAnimatorController;
                    break;
                case AnimatorType.Custom:
                    pAnim = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            //includeHealth = EditorGUILayout.ToggleLeft("Health Plugin (Recommended)", includeHealth);
            //includeInventory = EditorGUILayout.ToggleLeft("Inventory Plugin (Recommended)", includeInventory);
            // includeFootsteps = EditorGUILayout.ToggleLeft("Footsteps Plugin", includeFootsteps);

            if (!pAnim)
            {
                GUILayout.Space(5);
                GUILayout.Label("(Resources/Animators)", EditorStyles.boldLabel);
                playerAnimator = EditorGUILayout.ObjectField("Player Animator Controller", playerAnimator, typeof(RuntimeAnimatorController), false) as RuntimeAnimatorController;
            }

            if (pAnim != null)
            {
                playerAnimator = pAnim;
            }
            
            if (humanoidCharacter != null)
            {
                var charAnim = humanoidCharacter.GetComponent<Animator>();
                if (charAnim == null)
                {
                    message = "This asset has no Animator";
                    humanoidCharacter = null;
                    return;
                }
                else
                {
                    if (charAnim.avatar == null)
                    {
                        message = "This Animator has no avatar";
                        humanoidCharacter = null;
                        return;
                    }
                    if (!charAnim.avatar.isHuman)
                    {
                        message = "This Asset is not humanoid";
                        humanoidCharacter = null;
                        return;
                    }
                }
            }
            GUILayout.Space(10);

            if (GUILayout.Button("Finish Setup"))
            {
                Setup();
            }
            
            // Message Reporting
            switch (message)
            {
                case "New Player Created!":
                    EditorGUILayout.HelpBox(message, MessageType.Info);
                    break;
                case "Complete":
                    EditorGUILayout.HelpBox("Setup Complete!", MessageType.Info);
                    break;
                default:
                {
                    if(message != "")
                    {
                        EditorGUILayout.HelpBox(message, MessageType.Error);
                    }
                    break;
                }
            }
            
            GUILayout.Space(5);
            // EditorGUILayout.HelpBox("Get help and support via Discord link below.", MessageType.Info);
            if (GUILayout.Button("Support"))
            {
                Application.OpenURL("https://discord.gg/eVvyjHZYRj");
            }
            GUILayout.Space(5);
            
            if (GUILayout.Button("Documentation"))
            {
                Application.OpenURL("http://markmozza.com/humanoid-basics/docs/");
            }
            GUILayout.Space(5);
            
        }

        private void Setup()
        {

            if (humanoidCharacter == null)
            {
                message = "Missing Humanoid Asset";
                return;
            }
            if (playerAnimator == null)
            {
                message = "Missing Player Animator Controller";
                return;
            }
            
            // So this is some shit, its basically finding a humanoid core to replace this one with.
            // but what if you want a new one?
            if (baseHumanoid == null)
            {
                var p = PrefabUtility.InstantiatePrefab((GameObject)Resources.Load("Prefabs/Base/Player")) as GameObject;
                PrefabUtility.UnpackPrefabInstance(p, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                p.name = humanoidType == HumanoidCore.Type.Player ? "Player" : "Npc";
                baseHumanoid = p.transform;
                baseHumanoid.transform.position = Vector3.zero;
                Selection.activeGameObject = baseHumanoid.gameObject;
                SceneView.FrameLastActiveSceneView();
                    
                // Emulate Base Prefab
                //var basePrefab = Resources.Load("Prefabs/Base/Base") as GameObject;
                //humanoidCharacter = basePrefab;
                message = "New "+(humanoidType == HumanoidCore.Type.Player ? "Player" : "Npc")+" Created!";
            }
            
            if (baseHumanoid == null)
            {
                message = "Missing Player Behaviour Transform";
                return;
            }
            
            var gameManager = GameObject.FindGameObjectWithTag("GameManager");
            if (gameManager == null)
            {
                AddGameManager();
            }

            if (baseHumanoid.GetComponentInChildren<Animator>() != null)
            {
                DestroyImmediate(baseHumanoid.GetComponentInChildren<Animator>().gameObject, false);
            }
            //baseHumanoid.transform.position = Vector3.zero;

            _char = Instantiate(humanoidCharacter, baseHumanoid.position, baseHumanoid.rotation) as GameObject;

            // Get HumanoidCore
            var humanoidCore = baseHumanoid.GetComponent<HumanoidCore>();
            humanoidCore.humanoidType = humanoidType;
            // humanoidCore.spineFacingDirection = spineFacingDirection;

            // HumanoidCore Type
            if (humanoidType == HumanoidCore.Type.Player)
            {
                baseHumanoid.name = "Player";
                baseHumanoid.tag = "Player";
                _char.tag = "Player";
            }
            else
            {
                baseHumanoid.name = "Npc";
                baseHumanoid.tag = "Npc";
                _char.tag = "Npc";
                
                // Add Npc NavMeshAgent
                var navMeshAgent = _char.AddComponent<NavMeshAgent>();
                
                // Add Npc Behaviour
                var npcBehaviour = baseHumanoid.gameObject.GetComponent<NpcBehaviour>();
                if (!npcBehaviour)
                {
                    npcBehaviour = baseHumanoid.gameObject.AddComponent<NpcBehaviour>();
                }
                npcBehaviour.humanoidCore = humanoidCore;
                npcBehaviour.agent = navMeshAgent;
                npcBehaviour.aiType = NpcBehaviour.AITarget.Idle;
            }
            
            _char.layer = LayerMask.NameToLayer("Humanoid");
            _char.name = "Animator";
        
            _char.transform.localScale = _char.transform.localScale * scaleFactor;
            _char.AddComponent<AnimationListener>();
            
            // Add Ragdoll Helper
            _char.AddComponent<RagdollHelper>();
            
            // Lets add our Ik Control Script
            var ikControl = _char.AddComponent<IKControl>();
            ikControl.environmentLayer = 1<<LayerMask.NameToLayer("Default");
            
            // Add Transform Path Maker
            var playerTransformPathMaker = _char.AddComponent<TransformPathMaker>();
            playerTransformPathMaker.reference = _char.transform;
            playerTransformPathMaker.points = new Vector3[4];
            playerTransformPathMaker.points[0] = new Vector3(0, 0, 0);
            playerTransformPathMaker.points[1] = new Vector3(0, 0, 0);
            playerTransformPathMaker.points[2] = new Vector3(0, 0, 0);
            playerTransformPathMaker.points[3] = new Vector3(0, 0, 0);
            playerTransformPathMaker.pointsTime = new float[4];
            playerTransformPathMaker.pointsTime[0] = 1;
            playerTransformPathMaker.pointsTime[1] = 0.5f;
            playerTransformPathMaker.pointsTime[2] = 1;
            playerTransformPathMaker.pointsTime[3] = 1;
            
            // Lets add our Capsule Collider to the Animator
            var playerCapsuleCollider = _char.AddComponent<CapsuleCollider>();
            playerCapsuleCollider.center = new Vector3(0, 0.9f, 0);
            playerCapsuleCollider.radius = 0.3f;
            playerCapsuleCollider.height = 1.8f;
            playerCapsuleCollider.material = new PhysicMaterial("Friction");
            
            // Lets add a Rigidbody to the Animator
            var playerRigidbody = _char.AddComponent<Rigidbody>();
            playerRigidbody.mass = 4;
            playerRigidbody.drag = 0;
            playerRigidbody.angularDrag = 0;
            playerRigidbody.useGravity = true;
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            
            var playerAudioSource = _char.AddComponent<AudioSource>();
            humanoidCore.audioSource = playerAudioSource;
            
            _char.transform.SetParent(baseHumanoid);
            _charAnim = _char.GetComponent<Animator>();

            _char.GetComponent<RagdollHelper>().pB = humanoidCore;
 


            humanoidCore.rb = humanoidCore.GetComponent<Rigidbody>();
            humanoidCore.capsuleCollider = humanoidCore.GetComponent<CapsuleCollider>();
            humanoidCore.playerAnimator = _charAnim;

            humanoidCore.ragdollHelper = humanoidCore.playerAnimator.GetComponent<RagdollHelper>();
            humanoidCore.ikControl = ikControl;
            humanoidCore.pathMaker = playerTransformPathMaker;

            if (humanoidType == HumanoidCore.Type.Player)
            {
                var camera = GameObject.FindGameObjectWithTag("Camera");
                if (camera == null)
                {
                    camera = AddCamera();
                }

                // Get CameraCore Component and Target Player
                var cameraCore = camera.GetComponent<CameraCore>();
                cameraCore.target = humanoidCore.playerAnimator.transform;
            }

            GameObject aimHelper;
            try
            {
                aimHelper = _charAnim.transform.Find("Aim Helper").gameObject;
                humanoidCore.aimHelper = aimHelper.transform;
            }
            catch
            {
                aimHelper = new GameObject("Aim Helper");

                aimHelper.transform.parent = _charAnim.GetBoneTransform(HumanBodyBones.Head);
                aimHelper.transform.localPosition = Vector3.zero;

                Transform _spine = _charAnim.GetBoneTransform(HumanBodyBones.Spine);
                aimHelper.transform.parent = _spine;
                humanoidCore.aimHelper = aimHelper.transform;
                humanoidCore.aimHelperSpine = _spine;

                _spine.TransformVector(_charAnim.transform.forward);
                ikControl.head = _charAnim.GetBoneTransform(HumanBodyBones.Head);
            }
            ikControl.humanoidCore = humanoidCore;

            // Setup Spine Position For Aiming Rotation.
            switch (humanoidAnimatorType)
            {
                case AnimatorType.Civilian:
                    humanoidCore.startSpineRot = new Quaternion(0, -0.2f, 0, 1) * humanoidCore.aimHelperSpine.rotation;
                    break;
                case AnimatorType.Drunk:
                    humanoidCore.startSpineRot = new Quaternion(0, -0.2f, 0, 1) * humanoidCore.aimHelperSpine.rotation;
                    break;
                case AnimatorType.Ninja:
                    humanoidCore.startSpineRot = new Quaternion(0.2f, -0.2f, 0, 1) * humanoidCore.aimHelperSpine.rotation;
                    break;
                case AnimatorType.Zombie:
                    humanoidCore.startSpineRot = new Quaternion(0, -0.2f, 0, 1) * humanoidCore.aimHelperSpine.rotation;
                    break;
                case AnimatorType.Custom:
                    humanoidCore.startSpineRot = new Quaternion(0, -0.2f, 0, 1) * humanoidCore.aimHelperSpine.rotation;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            humanoidCore.transformToRotate = humanoidCore.playerAnimator.transform;
            //pB.pathMaker.reference = pB.transformToRotate;
            // humanoidCore.audioSource = humanoidCore.GetComponent<AudioSource>();
            humanoidCore.boneRb = new Rigidbody[15];
            _charAnim.runtimeAnimatorController = playerAnimator;
            _charAnim.applyRootMotion = false;
            _charAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            AddCollider(HumanBodyBones.Hips, HumanBodyBones.Hips);

            AddCollider(HumanBodyBones.Chest, HumanBodyBones.Hips);

            AddCollider(HumanBodyBones.Head, HumanBodyBones.Chest);

            AddCollider(HumanBodyBones.LeftUpperArm, HumanBodyBones.Chest);
            AddCollider(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm);
            AddCollider(HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm);

            AddCollider(HumanBodyBones.RightUpperArm, HumanBodyBones.Chest);
            AddCollider(HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm);
            AddCollider(HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm);

            AddCollider(HumanBodyBones.RightUpperLeg, HumanBodyBones.Hips);
            AddCollider(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg);
            AddCollider(HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg);

            AddCollider(HumanBodyBones.LeftUpperLeg, HumanBodyBones.Hips);
            AddCollider(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg);
            AddCollider(HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg);

            humanoidCore.hipsParent = humanoidCore.boneRb[0].transform.parent;

            var spineForward = humanoidCore.aimHelperSpine.forward;
            if(spineForward.z < 0.5f)
            {
                humanoidCore.spineFacingDirection = spineForward.x < -0.5f ? HumanoidCore.Direction.Up : HumanoidCore.Direction.Down;
            }
            else
            {
                humanoidCore.spineFacingDirection = HumanoidCore.Direction.Forward;
            }

            //
            var footsteps = baseHumanoid.GetComponent<HumanoidFootsteps>();
            footsteps.leftFoot = _charAnim.GetBoneTransform(HumanBodyBones.LeftFoot);
            footsteps.rightFoot = _charAnim.GetBoneTransform(HumanBodyBones.RightFoot);
            
            // Game Manager & Controller
            if (humanoidType == HumanoidCore.Type.Player)
            {
                // Setup Game Manager
                GameManager.Instance.player = baseHumanoid.gameObject;
                GameManager.Instance.humanoidCore = humanoidCore;
                GameManager.Instance.humanoidHealth = humanoidCore.gameObject.GetComponent<HumanoidHealth>();
                
                // Setup Player Controller
                GameManager.Instance.playerController.humanoidCore = humanoidCore;
                GameManager.Instance.playerController.humanoidEquipment = humanoidCore.gameObject.GetComponent<HumanoidEquipment>();
            }

            message = "Complete";
        }
        void SetJointLimits(CharacterJoint joint, float low, float high, float swing, float swing1)
        {

            SoftJointLimitSpring cjTwist = joint.twistLimitSpring;
            SoftJointLimit cjLowLimit = joint.lowTwistLimit;
            SoftJointLimit cjHighLimit = joint.highTwistLimit;
            SoftJointLimit cjSwingLimit1 = joint.swing1Limit;
            SoftJointLimit cjSwingLimit2 = joint.swing2Limit;

            cjLowLimit.limit = low;
            cjHighLimit.limit = high;
            cjSwingLimit1.limit = swing;
            cjSwingLimit2.limit = swing1;

            joint.twistLimitSpring = cjTwist;
            joint.lowTwistLimit = cjLowLimit;
            joint.highTwistLimit = cjHighLimit;
            joint.swing1Limit = cjSwingLimit1;
            joint.swing2Limit = cjSwingLimit2;

        }

        private void AddCollider(HumanBodyBones bone, HumanBodyBones connectTo)
        {
            GameObject b = _charAnim.GetBoneTransform(bone).gameObject;
            Rigidbody cT = _charAnim.GetBoneTransform(connectTo).gameObject.GetComponent<Rigidbody>();

            b.tag = "Bone";
            b.layer = 2;
            b.AddComponent<Rigidbody>();

            Rigidbody r = b.GetComponent<Rigidbody>();

            CharacterJoint cJ = b.AddComponent<CharacterJoint>();
            cJ.connectedBody = cT;

            SetJointLimits(cJ, 0, 0, 0, 0);

            cJ.enableCollision = false;
            cJ.enableProjection = true;
            cJ.enablePreprocessing = false;

        
            float _sizefactor;
            _sizefactor = sizeFactor / 20;

            if (bone == HumanBodyBones.Head)
            {
                SphereCollider sc = b.AddComponent<SphereCollider>();
                sc.radius = 0.1f;
                sc.center = new Vector3(0, 0.09f, 0);
                cJ.axis = cJ.transform.InverseTransformDirection(-_char.transform.right);

            }
            else
            {
                BoxCollider c = b.AddComponent<BoxCollider>();
                c.size = c.size * _sizefactor;

                if (bone == HumanBodyBones.Hips)
                {
                    DestroyImmediate(cJ);

                    float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.Chest).position) / 1.2f;
                    float x = Vector3.Distance(_charAnim.GetBoneTransform(HumanBodyBones.LeftUpperArm).position, _charAnim.GetBoneTransform(HumanBodyBones.RightUpperArm).position) / 1.8f;
                    c.center = c.transform.InverseTransformDirection(baseHumanoid.up) * (y / 2);
                    c.center = c.transform.InverseTransformDirection(baseHumanoid.up) * (y / 2);
                    c.size = c.transform.InverseTransformDirection(baseHumanoid.forward) * _sizefactor +
                             c.transform.InverseTransformDirection(baseHumanoid.up) * y +
                             c.transform.InverseTransformDirection(baseHumanoid.right) * x;
                }
                if (bone == HumanBodyBones.Chest)
                {
                    c.size = c.size * _sizefactor * 8;
                    SetJointLimits(cJ, 0, 50, 10, 10);
                    cJ.axis = cJ.transform.InverseTransformDirection(-_char.transform.right);

                    float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.Head).position) / 1.4f;
                    float x = Vector3.Distance(_charAnim.GetBoneTransform(HumanBodyBones.LeftUpperArm).position, _charAnim.GetBoneTransform(HumanBodyBones.RightUpperArm).position) / 1.4f;
                    c.center = c.transform.InverseTransformDirection(baseHumanoid.up) * (y / 2);
                    c.center = c.transform.InverseTransformDirection(baseHumanoid.up) * (y / 2);
                    c.size = c.transform.InverseTransformDirection(baseHumanoid.forward) * _sizefactor +
                             c.transform.InverseTransformDirection(baseHumanoid.up) * y +
                             c.transform.InverseTransformDirection(baseHumanoid.right) * x;
                }
                if (bone == HumanBodyBones.RightUpperLeg || bone == HumanBodyBones.LeftUpperLeg)
                {
                    SetJointLimits(cJ, -90, 0, 0, 0);
                    cJ.axis = cJ.transform.InverseTransformDirection(-_char.transform.right);

                    if(bone == HumanBodyBones.RightUpperLeg)
                    {
                        float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.RightLowerLeg).position)/1.1f;
                        c.center = c.transform.InverseTransformDirection(-baseHumanoid.up) * (y / 2);
                        c.size = c.transform.InverseTransformDirection(baseHumanoid.forward) * _sizefactor +
                                 c.transform.InverseTransformDirection(baseHumanoid.up) * y +
                                 c.transform.InverseTransformDirection(baseHumanoid.right) * _sizefactor;
                    }
                    else
                    {
                        float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.LeftLowerLeg).position)/1.1f;
                        c.center = c.transform.InverseTransformDirection(-baseHumanoid.up) * (y / 2);
                        c.size = c.transform.InverseTransformDirection(baseHumanoid.forward) * _sizefactor +
                                 c.transform.InverseTransformDirection(baseHumanoid.up) * y +
                                 c.transform.InverseTransformDirection(baseHumanoid.right) * _sizefactor;
                    }
                }
                if (bone == HumanBodyBones.RightLowerLeg || bone == HumanBodyBones.LeftLowerLeg)
                {
                    SetJointLimits(cJ, 0, 120, 0, 0);
                    cJ.axis = cJ.transform.InverseTransformDirection(-_char.transform.right);
                    float y = Vector3.Distance(b.transform.position, cT.position)/1.1f;

                    c.center = c.transform.InverseTransformDirection(baseHumanoid.up) * (y);

                    c.center = c.transform.InverseTransformDirection(-baseHumanoid.up) * (y / 2);
                    c.size = c.transform.InverseTransformDirection(baseHumanoid.forward) * _sizefactor +
                             c.transform.InverseTransformDirection(baseHumanoid.up) * y +
                             c.transform.InverseTransformDirection(baseHumanoid.right) * _sizefactor;

                }
                if (bone == HumanBodyBones.LeftUpperArm || bone == HumanBodyBones.RightUpperArm)
                {
                    SetJointLimits(cJ, -90, 90, 90, 90);
                    cJ.axis = cJ.transform.InverseTransformDirection(_char.transform.forward);
               
                    if (bone == HumanBodyBones.LeftUpperArm)
                    {
                        float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.LeftLowerArm).position) / 1.1f;
                        c.center = c.transform.InverseTransformDirection(-baseHumanoid.right) * (y / 2);
                        c.size = c.transform.InverseTransformDirection(baseHumanoid.forward) * _sizefactor +
                                 c.transform.InverseTransformDirection(baseHumanoid.up) * _sizefactor +
                                 c.transform.InverseTransformDirection(-baseHumanoid.right) * y;
                    }
                    else
                    {
                        float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.RightLowerArm).position)/1.1f;
                        c.center = c.transform.InverseTransformDirection(baseHumanoid.right) * (y / 2);
                        c.size = c.transform.InverseTransformDirection(baseHumanoid.forward) * _sizefactor +
                                 c.transform.InverseTransformDirection(baseHumanoid.up) * _sizefactor +
                                 c.transform.InverseTransformDirection(baseHumanoid.right) * y;
                    }
                

                }
                if (bone == HumanBodyBones.LeftLowerArm || bone == HumanBodyBones.RightLowerArm)
                {
                    SetJointLimits(cJ, -120, 0, 10, 10);

                    if (bone == HumanBodyBones.LeftLowerArm)
                    {
                        cJ.axis = cJ.transform.InverseTransformDirection(_char.transform.forward);
                        float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.LeftHand).position) / 1.1f;
                        c.center = c.transform.InverseTransformDirection(-baseHumanoid.right) * (y / 2);
                        c.size = c.transform.InverseTransformDirection(baseHumanoid.forward) * _sizefactor +
                                 c.transform.InverseTransformDirection(baseHumanoid.up) * _sizefactor +
                                 c.transform.InverseTransformDirection(-baseHumanoid.right) * y;
                    }
                    else
                    {
                        cJ.axis = cJ.transform.InverseTransformDirection(_char.transform.forward);
                        float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.RightHand).position) / 1.1f;
                        c.center = c.transform.InverseTransformDirection(baseHumanoid.right) * (y / 2);
                        c.size = c.transform.InverseTransformDirection(baseHumanoid.forward) * _sizefactor +
                                 c.transform.InverseTransformDirection(baseHumanoid.up) * _sizefactor +
                                 c.transform.InverseTransformDirection(baseHumanoid.right) * y;
                    }
                }
                if (bone == HumanBodyBones.RightHand || bone == HumanBodyBones.LeftHand)
                {
                    SetJointLimits(cJ, -50, 50, 20, 20);
                    cJ.axis = cJ.transform.InverseTransformDirection(_char.transform.forward);

                    if (bone == HumanBodyBones.LeftHand)
                    {
                        float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.LeftLowerArm).position);
                        c.center = c.transform.InverseTransformDirection(-baseHumanoid.right) * (y / 2);
                    }
                    else
                    {
                        float y = Vector3.Distance(b.transform.position, _charAnim.GetBoneTransform(HumanBodyBones.RightLowerArm).position);
                        c.center = c.transform.InverseTransformDirection(baseHumanoid.right) * (y / 2);
                    }
                }
                if (bone == HumanBodyBones.RightFoot || bone == HumanBodyBones.LeftFoot)
                {
                    SetJointLimits(cJ, -20, 20, 10, 10);
                    cJ.axis = cJ.transform.InverseTransformDirection(-_char.transform.right);

                    DestroyImmediate(c);

                
                    float y = Vector3.Distance(b.transform.position, baseHumanoid.position);
                    SphereCollider sc = b.AddComponent<SphereCollider>();
                    sc.center = sc.transform.InverseTransformDirection(-baseHumanoid.up) * (y / 2.5f);
                    sc.radius = _sizefactor/2.5f ;

                    SphereCollider sc2 = b.AddComponent<SphereCollider>();
                    sc2.center = sc2.transform.InverseTransformDirection(-baseHumanoid.up) * (y / 2.5f) + sc2.transform.InverseTransformDirection(baseHumanoid.forward) * (y / 1.2f);
                    sc2.radius = _sizefactor / 2.8f;
                }
                if (bone == HumanBodyBones.Head)
                {
                    SetJointLimits(cJ, -50, 50, 50, 50);
                    cJ.axis = cJ.transform.InverseTransformDirection(-_char.transform.right);
                }
                if (c)
                {
                    c.size = new Vector3(Mathf.Abs(c.size.x), Mathf.Abs(c.size.y), Mathf.Abs(c.size.z));
                }
            }

            HumanoidCore pb = baseHumanoid.GetComponent<HumanoidCore>();
            for (int i = 0; i < pb.boneRb.Length; i++)
            {
                if (pb.boneRb[i] == null)
                {
                    pb.boneRb[i] = r;
                    return;
                }

            }


        }
        
        private HumanoidCore SearchForPlayer()
        {
            try
            {
                var searchForPlayer = FindObjectsOfType<HumanoidCore>();
                foreach (var pHumanoidCore in searchForPlayer) {
                    if (pHumanoidCore.humanoidType == HumanoidCore.Type.Player)
                    {
                        return pHumanoidCore;
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }
    }
}
