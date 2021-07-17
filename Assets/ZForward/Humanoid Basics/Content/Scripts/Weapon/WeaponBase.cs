using System;
using System.Collections;
using Humanoid_Basics.Camera;
using Humanoid_Basics.Core;
using Humanoid_Basics.Core.Scriptables;
using Humanoid_Basics.Player;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Humanoid_Basics.Weapon
{
    [ExecuteInEditMode]
    public class WeaponBase : MonoBehaviour {

        [AttributeUsage(AttributeTargets.Field)]
        private class SettingsGroup : Attribute
        { }

        [AttributeUsage(AttributeTargets.Field)]
        public class AdvancedSetting : Attribute
        { }

        private GameObject mesh;
        public Rigidbody rb;
        public BoxCollider bc;
        public Transform barrel;

        [HideInInspector]
        public bool usingLeftHand;

        [HideInInspector]
        public bool canShoot;


        public HumanoidCore pB;

        [HideInInspector]
        public Transform leftHand, rightHand;
        [HideInInspector]
        public Mesh leftHandMesh, rightHandMesh;

        [HideInInspector]
        public Animator animator;

        [HideInInspector]
        public bool shootProgress;

        [HideInInspector]
        public bool isColliding;

        [HideInInspector]
        public bool reloadProgress;
        [Header("Settings")]   
        public string weapon;
        [Range(1, 16)]
        public int raysPerShot = 1;
        public ShootingMode shootingMode = ShootingMode.Raycast;
        public GameObject projectile;
        public int damage;
        public int maxInClipBullets;
        public int reloadBullets;
        public float fireRate;
        public float recoil;
        public float reloadTime;
        public int currentAmmo;
        [Header("Positioning")]
        public bool previewHands = false;
        public UseLeftHand useLeftHand = UseLeftHand.Yes;
        public bool updatePreset = false;
        [FormerlySerializedAs("PositioningPreset")] public PositioningPresets positioningPreset;

        private readonly Vector3 inventoryPos = new Vector3(0, -.5f, .2f);
        private readonly Quaternion inventoryRot = new Quaternion(.5f, -.35f, 0, 1);

        [Serializable]
        public struct CurrentPositioning
        {
            public Vector3 defaultPosition;
            public Quaternion defaultRotation;

            public Vector3 aimingPosition;
            public Quaternion aimingRotation;

            public static CurrentPositioning defaultSettings
            {
                get
                {
                    return new CurrentPositioning
                    {
                   
                        defaultPosition = Vector3.zero,
                        defaultRotation = Quaternion.identity,

                        aimingPosition = Vector3.zero,
                        aimingRotation = Quaternion.identity,
                    };
                }
            }
        }

        [SettingsGroup]
        public CurrentPositioning currentPositioning = CurrentPositioning.defaultSettings;

        [HideInInspector]
        public Vector3[] startPos = new Vector3[2];
        [HideInInspector]
        public Quaternion[] startRot = new Quaternion[2];

        public enum PositioningPresets
        {
            PistolPreset,
            RiflePreset,
            RPGPreset,
            Custom
        }
        //Pistol Preset
        private readonly Vector3 defaultPositionPistol = new Vector3(0, -.7f, .2f);
        private readonly Quaternion defaultRotationPistol = new Quaternion(.45f, -.47f, .25f, 1);

        private readonly Vector3 aimingPositionPistol = new Vector3(.2f, -.15f, .55f);
        private readonly Quaternion aimingRotationPistol = new Quaternion(0, 0, 0, 1);

        //Rifle Preset
        private readonly Vector3 defaultPositionRifle = new Vector3(0, -.45f, .2f);
        private readonly Quaternion defaultRotationRifle = new Quaternion(.35f, -.7f, .25f, 1);

        private readonly Vector3 aimingPositionRifle = new Vector3(.2f, -.15f, .35f);
        private readonly Quaternion aimingRotationRifle = new Quaternion(0, 0, 0, 1);

        //RPG Preset
        private readonly Vector3 defaultPositionRpg = new Vector3(.1f, -.05f, .25f);
        private readonly Quaternion defaultRotationRpg = new Quaternion(0, 0, 0, 1);

        private readonly Vector3 aimingPositionRpg = new Vector3(.1f, -.05f, .50f);
        private readonly Quaternion aimingRotationRpg = new Quaternion(0, 0, 0, 1);

    
        public enum ShootingMode
        {
            Raycast,
            Projectile
        }
        public enum UseLeftHand
        {
            No,
            Yes
        }
        [HideInInspector]
        public float currentRecoil;
        private AudioSource audioS;
        [Header("Audio")]  
        public AudioClip pickUpAudio;
        public AudioClip shotAudio;
        public AudioClip noAmmoAudio;
        public AudioClip reloadAudio;
        public AudioClip aimAudio;
        public AudioClip switchAudio;
        [Header("Extras")]
        public Sprite icon;
        public Sprite centerCross;

        private void Start()
        {
        
            if (!Application.isPlaying) { return; }

            if (useLeftHand == UseLeftHand.Yes)
            {
                usingLeftHand = true;
            }
            animator = transform.GetChild(0).GetComponent<Animator>();
            leftHand = animator.transform.Find("LeftHand");
            rightHand = animator.transform.Find("RightHand");

            currentAmmo = maxInClipBullets;
            mesh = animator.transform.Find("Mesh").gameObject;
            barrel = animator.transform.Find("Barrel");
            rb = GetComponent<Rigidbody>();
            bc = GetComponent<BoxCollider>();
            audioS = GetComponent<AudioSource>();

            startPos[0] = leftHand.localPosition;
            startPos[1] = rightHand.localPosition;

            startRot[0] = leftHand.localRotation;
            startRot[1] = rightHand.localRotation;
        }

        private void SetDefaultPositioning(Vector3 defaultPos, Quaternion defaultRot, Vector3 aimPos, Quaternion aimRot)
        {

            currentPositioning.defaultPosition = defaultPos;
            currentPositioning.defaultRotation = defaultRot;

            currentPositioning.aimingPosition = aimPos;
            currentPositioning.aimingRotation = aimRot;
        }

        private void OnDrawGizmos()
        {
            if (!previewHands) { return; }
            if (!animator)
            {
                animator = transform.GetChild(0).GetComponent<Animator>();
            }
            else
            {
                if (!leftHand)
                {
                    leftHand = animator.transform.Find("LeftHand");
                }
                if (!rightHand)
                {
                    rightHand = animator.transform.Find("RightHand");
                }
            }

            if (leftHand && rightHand)
            {
                if (!leftHandMesh)
                {
                    GameObject _lH = Resources.Load("Editor/Mesh/LeftHand") as GameObject;
                    leftHandMesh = _lH.GetComponent<MeshFilter>().sharedMesh;
                }
                if (!rightHandMesh)
                {
                    GameObject _rh = Resources.Load("Editor/Mesh/RightHand") as GameObject;
                    rightHandMesh = _rh.GetComponent<MeshFilter>().sharedMesh;
                }
                if (leftHandMesh && rightHandMesh)
                {
                    Gizmos.DrawMesh(leftHandMesh, leftHand.position, leftHand.rotation);
                    Gizmos.DrawMesh(rightHandMesh, rightHand.position, rightHand.rotation);
                }
            }
        
        }

        private void Update()
        {
            if (!updatePreset) return;
            switch (positioningPreset)
            {
                case PositioningPresets.PistolPreset:
                    SetDefaultPositioning(defaultPositionPistol, defaultRotationPistol, aimingPositionPistol, aimingRotationPistol);
                    break;
                case PositioningPresets.RiflePreset:
                    SetDefaultPositioning(defaultPositionRifle, defaultRotationRifle, aimingPositionRifle, aimingRotationRifle);
                    break;
                case PositioningPresets.RPGPreset:
                    SetDefaultPositioning(defaultPositionRpg, defaultRotationRpg, aimingPositionRpg, aimingRotationRpg);
                    break;
                case PositioningPresets.Custom:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            updatePreset = false;
            print("Preset Updated");
        }

        private void FixedUpdate()
        {
       
            if (!Application.isPlaying) { return; }
            currentRecoil = raysPerShot == 1 ? Mathf.Lerp(currentRecoil, 0, .02f) : Mathf.Lerp(currentRecoil, .025f, .03f);

            if (!pB) return;
            if (pB.currentWeapon != this) return;
            
            if (useLeftHand == WeaponBase.UseLeftHand.No)
            {
                usingLeftHand = (pB.aim) ? true : false;
            }
            var transform1 = transform;
            var position = transform1.position;
            var forward = transform1.forward;
            var size = bc.size;
            isColliding = Physics.Linecast(position - forward * (size.z * 2), position + forward * size.z, LayerMask.GetMask("Default"));
        }
        
        public void Shoot()
        {
            if (isColliding) { return; }
            if (currentAmmo > 0 && !reloadProgress && !shootProgress)
            {
                StartCoroutine(ShootProgress());
                return;
            }
            if (currentAmmo == 0 && !reloadProgress && !shootProgress)
            {
                if (!audioS.isPlaying)
                {
                    audioS.PlayOneShot(noAmmoAudio);
                }
                Reload();
            }
        }

        private IEnumerator ShootProgress()
        {

            shootProgress = true;
            if (!pB.aim)
            {
                yield return new WaitForSeconds(.25f);
            }
            if (!pB.aim)
            {
                shootProgress = false;
                StopCoroutine(ShootProgress());
            }
            else
            {
                animator.Rebind();
                animator.Play("Shoot");

                pB.recoil = UnityEngine.Random.Range(recoil, recoil * 2);

                audioS.PlayOneShot(shotAudio);
                currentAmmo--;

                if (shootingMode == ShootingMode.Projectile)
                {
                    ProjectileShoot();
                }
                else
                {
                    RaycastShoot();
                }
                if (currentRecoil < recoil) {
                    currentRecoil += 0.02f; 
                }
                yield return new WaitForSeconds(fireRate);

                shootProgress = false;

                leftHand.localPosition = startPos[0];
                leftHand.localRotation = startRot[0];

                rightHand.localPosition = startPos[1];
                rightHand.localRotation = startRot[1];
            }
        }

        private void RaycastShoot()
        {
            for (var i = 0; i < raysPerShot; i++)
            {
                var _recoil = UnityEngine.Random.insideUnitSphere * currentRecoil;
            
                if(i > 0)
                {
                    _recoil *= i;
                }

                RaycastHit hit, centerHit;

                // TODO: Remove Camera Reference, this needs to be done differently.
                bool centerHitted = Physics.Raycast(CameraCore.Instance.cameraObject.transform.position, CameraCore.Instance.cameraObject.transform.forward + _recoil, out centerHit);

                if (centerHitted)
                {
                    if (Physics.Raycast(barrel.position, centerHit.point - barrel.transform.position, out hit))
                    {
                        if (!hit.transform)
                        {
                            //GameManager.Instance.centerCrossBlock.SetActive(false);
                            return;
                        }

                        // if (hit.transform != centerHit.transform)
                        // {
                        //     GameManager.Instance.centerCrossBlock.transform.position += .02f * hit.point;
                        //     GameManager.Instance.centerCrossBlock.transform.localScale = new Vector3(.1f, 1, .1f);
                        //     // GameManager.Instance.centerCrossBlock.transform.parent = hit.transform;
                        //     GameManager.Instance.centerCrossBlock.SetActive(true);
                        // }
                        // else
                        // {
                        //     GameManager.Instance.centerCrossBlock.SetActive(false);
                        // }
                        
                        //var hRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                        //var hPrefab = (GameObject)Resources.Load("Prefabs/Particles/BulletHole");
                        //var bulletHole = Instantiate(hPrefab, hit.point, hRotation);
                        //bulletHole.transform.parent = hit.transform;
                    
                        Debug.Log("We have a Transform: "+hit.transform.gameObject.name);
                    
                        HandleHit(hit);
                    }
                    else
                    {
                        
                    }

                }
            }
        }

        private void HandleHit(RaycastHit h)
        {
            // your code
            if (h.transform.CompareTag("Npc"))
            {
                //var meshCollider = h.transform.GetComponent<MeshCollider>();
                //if (!meshCollider) return;
                var parent = h.transform.root;
                var humanoidNpc = parent.GetComponent<HumanoidCore>();
                if (!humanoidNpc) return;
                var humanoidNpcHealth = parent.GetComponent<HumanoidHealth>();
                if (humanoidNpcHealth)
                    humanoidNpcHealth.Damage(damage);
            }
            else
            {
                ShotVisuals(h);
            }
        }

        private void ShotVisuals(RaycastHit h)
        {
            var hTag = h.transform.tag;
            if (hTag == "" || hTag == "Weapon" || hTag == "Player" || hTag == "Npc") return;
            
            var hRotation = Quaternion.FromToRotation(Vector3.up, h.normal);
            var hPosition = h.point;
            var rScale = Random.Range(.1f, .2f);
            var hPrefab = (GameObject)Resources.Load("Prefabs/Particles/BulletHole");
            var bulletHole = Instantiate(hPrefab, hPosition, hRotation);
            bulletHole.transform.localPosition += .02f * h.normal;
            bulletHole.transform.localScale = new Vector3(rScale, 1, rScale);
            bulletHole.transform.parent = h.transform;
        }

        // TODO: Remove Camera Reference, this needs to be done differently.
        private void ProjectileShoot()
        {
            RaycastHit hit;
            Physics.Raycast(CameraCore.Instance.cameraObject.transform.position, CameraCore.Instance.cameraObject.transform.forward, out hit);

            var _projectile = Instantiate(projectile, barrel.position, barrel.rotation) as GameObject;
            if (hit.transform == null) { return; }

            if (pB.aim)
            {
                _projectile.transform.LookAt(hit.point);
            }
        }
        
        public void Reload()
        {
            if (currentAmmo < maxInClipBullets && reloadBullets > 0 && !reloadProgress)
            {
                StartCoroutine(ReloadProgress());
            }
        }

        private IEnumerator ReloadProgress()
        {
            int toRefill = maxInClipBullets - currentAmmo;

            shootProgress = false;
            reloadProgress = true;
            animator.Play("Reload");
            audioS.PlayOneShot(reloadAudio);

            yield return new WaitForSeconds(reloadTime);

            if (toRefill <= reloadBullets)
            {
                reloadBullets -= toRefill;
                currentAmmo += toRefill;
            }
            else{
                currentAmmo += reloadBullets;
                reloadBullets = 0;
            }

            reloadProgress = false;
            leftHand.localPosition = startPos[0];
            leftHand.localRotation = startRot[0];

            rightHand.localPosition = startPos[1];
            rightHand.localRotation = startRot[1];
        }
        
        public void AimAudio()
        {
            audioS.PlayOneShot(aimAudio);
        }
        
        public void PutInInventory()
        {
            Destroy(rb);
            bc.enabled = false;
            transform.localPosition = inventoryPos;
            transform.localRotation = inventoryRot;
        }
        
        public void RemoveFromInventory()
        {
            gameObject.AddComponent<Rigidbody>();
            bc.enabled = true;

        }
        
        public void ToggleRenderer(bool value)
        {
            mesh.SetActive(value);
            switch (value)
            {
                case true:
                    audioS.PlayOneShot(switchAudio);
                    break;
                case false:
                    transform.localPosition = inventoryPos;
                    transform.localRotation = inventoryRot;
                    break;
            }
        }
        
        public void MoveTo(Transform reference)
        {
            Vector3 _offset = currentPositioning.defaultPosition;
            Quaternion _toRot = Quaternion.identity;

            if (pB.aim || reloadProgress)
            {
                if (reloadProgress)
                {
                    usingLeftHand = true;
                }
                if (pB.halfSwitchingWeapons)
                {
                    _offset = currentPositioning.aimingPosition;
                    _toRot = currentPositioning.aimingRotation;
                }
            }
            else
            {
                if (!pB.crouch)
                {
                    if (pB.grounded)
                    {
                        _offset = currentPositioning.defaultPosition;
                    }
                    else
                    {
                        _offset = currentPositioning.defaultPosition;
                        _offset.y += 0.3f;
                        _offset.z += 0.1f;
                    }
                }
                else
                {
                    _offset.z -= 0.1f;
                }
                _toRot = currentPositioning.defaultRotation;

            }
            if (pB.switchingWeapons && !pB.halfSwitchingWeapons)
            {
                _offset = inventoryPos;
                _toRot = inventoryRot;
            }

            _offset.z += pB.bellyOffset;
            transform.localPosition = Vector3.Slerp(transform.localPosition, _offset, 6 * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, _toRot, 8 * Time.deltaTime);

        }
    }
}

