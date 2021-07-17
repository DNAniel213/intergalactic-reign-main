using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Humanoid_Basics.Core.Scriptables;
using Humanoid_Basics.Player;
using Humanoid_Basics.Weapon;
using UnityEngine;
using UnityEngine.Serialization;

namespace Humanoid_Basics.Core
{
    public class ItemPickup : MonoBehaviour
    {
        public enum Type { InstantUseItem, WeaponItem, InventoryItem }
        public enum Axis {X, Y, Z};
        private enum Direction {Up, Down};
        
        [Header("Core")]
        public Type type;
        public int inventoryId;
        public int weaponId;
        public float distance;
        public bool automaticPickup;
        
        [Header("Item Mesh")]
        public MeshRenderer meshRenderer;
        
        [HideInInspector]
        public InventoryDatabase.Item inventoryItem;
        
        // messages
        public string pickupMessage = "You picked up an item.";
        public float pickupMessageCooldown = 10f;
        
        // sounds
        public AudioClip pickupSound;
        
        // private
        public BoxCollider boxCollider;
        public ParticleSystem particleEffect;
        public WeaponBase weaponBase;
        private AudioSource audioSource;
        
        // Collision
        [System.Serializable]
        public class CollidedObject
        {
            public bool hasCollided;
            public GameObject gameObject;
            public HumanoidCore humanoidCore;
            public HumanoidHealth humanoidHealth;
            public HumanoidInventory humanoidInventory;
        }
        public CollidedObject collidedObject = new CollidedObject();
        
        [Header("Float Settings")]
        public bool itemFloat = true;
        public float floatLerpSpeed = 0.5f;
        private Vector3 top, bottom;
        private float floatPercent = 0.0f;
        private Direction direction;
        
        [Header("Rotation Settings")]
        public bool itemRotate = true;
        public float rotationSpeed = 25f;
        public Vector3 rotationAxis = Vector3.down;

        private void Start()
        {
            inventoryItem = GameManager.Instance.inventoryDatabase.itemDatabase[inventoryId];
            audioSource = GetComponent<AudioSource>();
            boxCollider = GetComponent<BoxCollider>();
            particleEffect = GetComponent<ParticleSystem>();

            var position = transform.position;
            top = new Vector3(position.x, position.y + distance, position.z);
            bottom = new Vector3(position.x, position.y - distance, position.z);

            if (type == Type.WeaponItem)
            {
                weaponBase = GetComponent<WeaponBase>();
            }
        }

        private void Update () {
            if (itemFloat) ApplyFloatingEffect();
            if (itemRotate) ApplyRotationEffect();
            if (automaticPickup) PickUpItem();
        }

        private void LateUpdate()
        {
            
        }

        private void ApplyFloatingEffect()
        {
            if (floatPercent < 1)
            {
                switch (direction)
                {
                    case Direction.Up:
                        floatPercent += Time.deltaTime * floatLerpSpeed;
                        transform.position = Vector3.Lerp(top, bottom, floatPercent);
                        break;
                    case Direction.Down:
                        floatPercent += Time.deltaTime * floatLerpSpeed;
                        transform.position = Vector3.Lerp(bottom, top, floatPercent);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (!(floatPercent >= 1)) return;
            floatPercent = 0.0f;
            direction = direction == Direction.Up ? Direction.Down : Direction.Up;
        }

        private void PickUpItem()
        {
            if (!collidedObject.hasCollided) return;
            
            var itemUsed = false;
            switch (type)
            {
                case Type.InstantUseItem:
                    switch (inventoryItem.itemType)
                    {
                        case ItemType.Health:
                            collidedObject.humanoidHealth.Heal(inventoryItem.healthBoost);
                            itemUsed = true;
                            break;
                        case ItemType.Armor:
                            itemUsed = true;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                    break;
                case Type.WeaponItem:
                    if (collidedObject.humanoidCore.humanoidInventory.WeaponCount() >= collidedObject.humanoidInventory.weaponLimit) return;
                    collidedObject.humanoidInventory.AddWeapon(weaponBase);
                    itemUsed = true;
                    break;
                case Type.InventoryItem:
                    if (collidedObject.humanoidCore.humanoidInventory.ItemCount() >= collidedObject.humanoidInventory.itemLimit) return;
                    collidedObject.humanoidInventory.AddItem(inventoryItem);
                    itemUsed = true;
                    break;
                default:
                    itemUsed = false;
                    break;
            }

            // Play pick up audio
            if (itemUsed)
            {
                if (pickupSound)
                {
                    audioSource.PlayOneShot(pickupSound);
                }
                
                // Add Alert Message to Buffer
                if (pickupMessage.Length > 0)
                {
                    GameManager.Instance.alertMessage.AddAlert(pickupMessage);
                }
                
                // Reset & Remove Item
                if (meshRenderer) meshRenderer.enabled = false;
                boxCollider.enabled = false;
                particleEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                enabled = false;
                
                ResetCollision();
            }

        }

        private void ApplyRotationEffect()
        {
            transform.Rotate(rotationAxis, Time.deltaTime * rotationSpeed);
        }
        
        private void ProcessCollision(Collider other)
        {
            if (!other.gameObject.CompareTag("Player") && !other.gameObject.CompareTag("Bone"))
            {
                return;
            }

            var playerGameObject = other.transform.root.gameObject;
            if (collidedObject.hasCollided && collidedObject.gameObject == playerGameObject) return;  
            
            var humanoidCore = playerGameObject.GetComponent<HumanoidCore>();
            var humanoidHealth = playerGameObject.GetComponent<HumanoidHealth>();
            var humanoidInventory = playerGameObject.GetComponent<HumanoidInventory>();
            if (!humanoidInventory || !humanoidCore || !humanoidHealth) return;
            collidedObject.hasCollided = true;
            collidedObject.gameObject = playerGameObject;
            collidedObject.humanoidCore = humanoidCore;
            collidedObject.humanoidHealth = humanoidHealth;
            collidedObject.humanoidInventory = humanoidInventory;
            
            
            // Check that its not an Instant Use Item
            if (type == Type.InstantUseItem) return;
            
            // Weapons Full UI
            if (collidedObject.hasCollided && collidedObject.humanoidCore.humanoidInventory.WeaponCount() >=
                collidedObject.humanoidInventory.weaponLimit)
            {
                GameManager.Instance.alertMessage.AddAlert("You have no more room for weapons...");
                StartCoroutine(PickupMessageTimer());
            }

            // Items Full UI
            if (collidedObject.hasCollided && collidedObject.humanoidCore.humanoidInventory.ItemCount() >=
                collidedObject.humanoidInventory.itemLimit)
            {
                GameManager.Instance.alertMessage.AddAlert("You have no more room for items...");
                StartCoroutine(PickupMessageTimer());
            }

        }
        
        private void ResetCollision()
        {
            collidedObject.hasCollided = false;
            collidedObject.gameObject = null;
            collidedObject.humanoidCore = null;
        }

        private IEnumerator PickupMessageTimer()
        {
            yield return new WaitForSeconds(pickupMessageCooldown);
            ResetCollision();
        }

        private void OnTriggerStay(Collider col)
        {
            //ProcessCollision(col);
        }

        private void OnTriggerEnter(Collider col)
        {
            ProcessCollision(col);
        }

        private void OnTriggerExit(Collider col)
        {
            //ResetCollision();
        }
    }
}
