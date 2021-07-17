/*
 * HumanoidPickup.cs - Basement Media
 * @version: 1.0.0
*/

using System;
using Humanoid_Basics.Core;
using Humanoid_Basics.Core.Scriptables;
using Humanoid_Basics.Weapon;
using UnityEngine;

namespace Humanoid_Basics.Player
{
    [RequireComponent(typeof(HumanoidCore))]
    [RequireComponent(typeof(HumanoidHealth))]
    [RequireComponent(typeof(HumanoidInventory))]
    public class HumanoidPickup: MonoBehaviour
    {
        [Header("Humanoid Core")]
        public HumanoidCore humanoidCore;
        public HumanoidHealth humanoidHealth;
        public HumanoidInventory humanoidInventory;

        [Header("Settings")] 
        public bool automaticPickup = true;
        
        private bool hasCollidedWith;
        [System.Serializable]
        public class CollidedWith
        {
            public GameObject gameObject;
            public bool isItem;
            public ItemPickup itemPickup;
            public WeaponBase weaponBase;
            public ParticleSystem particleSystem;
        }
        public CollidedWith collidedWith = new CollidedWith();
        
        private void Start()
        {
            humanoidCore = GetComponent<HumanoidCore>();
            humanoidHealth = GetComponent<HumanoidHealth>();
            humanoidInventory = GetComponent<HumanoidInventory>();
        }

        private void Update()
        {
            if (automaticPickup) PickUpItem();
        }

        public void PickUpItem()
        {
            if (collidedWith == null) return;
            
            // Check if we have collision or if Item
            if (!hasCollidedWith || !collidedWith.isItem) return;
            
            // Check if we have room
            if (humanoidInventory.WeaponCount() >= humanoidInventory.weaponLimit) return;
            
            //var item = collidedWith.gameObject.GetComponent<ItemPickup>();
            var itemPickup = collidedWith.itemPickup;
            switch (itemPickup.type)
            {
                case ItemPickup.Type.InstantUseItem:
                    UseInstantItem(itemPickup);
                    break;
                case ItemPickup.Type.WeaponItem:
                    humanoidInventory.AddWeapon(collidedWith.weaponBase);
                    break;
                case ItemPickup.Type.InventoryItem:
                    humanoidInventory.AddItem(itemPickup.inventoryItem);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Play pick up audio
            if (itemPickup.pickupSound)
            {
                humanoidCore.audioSource.PlayOneShot(itemPickup.pickupSound);
            }

            // Reset & Remove Item
            itemPickup.boxCollider.enabled = false;
            collidedWith.particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            itemPickup.enabled = false;
            ResetCollision();
        }

        private void UseInstantItem(ItemPickup item)
        {
            switch (item.inventoryItem.itemType)
            {
                case ItemType.Health:
                    humanoidHealth.Heal(item.inventoryItem.healthBoost);
                    break;
                case ItemType.Armor:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            // Play pick up audio
            if (item.inventoryItem.itemSounds.useSound)
            {
                humanoidCore.audioSource.PlayOneShot(item.inventoryItem.itemSounds.useSound, item.inventoryItem.itemSounds.soundVolume);
            }
            Destroy(item.gameObject);
        }

        private void ProcessCollision(Collision other)
        {
            if (!other.gameObject.CompareTag("Item")) return;
            
            hasCollidedWith = true;
            
            // Lets check for an item
            var itemPickup = other.gameObject.GetComponent<ItemPickup>();
            if (itemPickup)
            {
                collidedWith.gameObject = other.gameObject;
                collidedWith.isItem = true;
                collidedWith.itemPickup = itemPickup;
                collidedWith.particleSystem = other.gameObject.GetComponent<ParticleSystem>();;
                collidedWith.weaponBase = itemPickup.type == ItemPickup.Type.WeaponItem ? collidedWith.gameObject.GetComponent<WeaponBase>() : null;
            }
            else
            {
                ResetCollision();
            }
        }
        
        private void ResetCollision()
        {
            hasCollidedWith = false;
            collidedWith.gameObject = null;
            collidedWith.isItem = false;
            collidedWith.itemPickup = null;
            collidedWith.particleSystem = null;
            collidedWith.weaponBase = null;
        }

        private void OnCollisionStay(Collision col)
        {
            ProcessCollision(col);
        }

        private void OnCollisionEnter(Collision other)
        {
            //ProcessCollision(other);
        }

        private void OnCollisionExit()
        {
            ResetCollision();
        }
    }
}