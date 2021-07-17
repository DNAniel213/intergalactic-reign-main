/*
 * Inventory.cs - Basement Media
 * @version: 1.0.0
*/
using UnityEngine;
using System.Collections.Generic;
using System;
using Humanoid_Basics.Core.Helpers;

namespace Humanoid_Basics.Core.Scriptables
{
    public enum ItemType { Health, Armor, Weapon }
    [CreateAssetMenu(fileName = "InventoryDatabase", menuName = "ScriptableObjects/InventoryDatabase", order = 1)]
    public class InventoryDatabase : ScriptableObject
    {
        public List<Item> itemDatabase = new List<Item> ();

        [Serializable]
        public class Item
        {
            [Header("Core")]
            public string name;
            public int id;
            [Multiline] public string description;
            public Sprite itemSprite;

            public ItemType itemType;
            [DrawIf("itemType", ItemType.Health)] public int healthBoost;

            [Serializable]
            public class Settings
            {
                public int maxItemCount;
            }
            public Settings itemSettings = new Settings();
            
            [Serializable]
            public class Sounds
            {
                public AudioClip useSound;
                [Range(0,1f)]
                public float soundVolume = 1f;
            }
            public Sounds itemSounds = new Sounds();
        }

        public void OnEnable()
        {
            Reseed();
        }

        private void OnValidate()
        {
            Reseed();
        }

        private void OnDisable()
        {
            Reseed();
        }

        public void Reseed()
        {
            foreach (var x in itemDatabase)
            {
                x.id = itemDatabase.IndexOf(x);
            }
        }
    }
}
