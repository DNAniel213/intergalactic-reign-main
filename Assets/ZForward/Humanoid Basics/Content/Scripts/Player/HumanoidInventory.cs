/*
 * HumanoidInventory.cs - Basement Media
 * @version: 1.0.0
*/

using System.Collections.Generic;
using Humanoid_Basics.Core.Scriptables;
using Humanoid_Basics.Weapon;
using UnityEngine;
using UnityEngine.Serialization;

namespace Humanoid_Basics.Player
{
    public class HumanoidInventory : MonoBehaviour
    {
        [Header("Humanoid Core")]
        public HumanoidCore humanoidCore;

        [FormerlySerializedAs("bagLimit")] [Header("Settings")]
        public int weaponLimit = 4;
        public int itemLimit = 20;
        
        // Holds Weapons
        public List<WeaponBase> weapons = new List<WeaponBase>();

        // Holds Players Inventory (Key Cards, Keys, Progression Items)
        public List<InventoryDatabase.Item> items = new List<InventoryDatabase.Item>();

        // Start is called before the first frame update
        private void Start()
        {
            humanoidCore = GetComponent<HumanoidCore>();
        }

        public void AddItem(InventoryDatabase.Item item)
        {
            items.Add(item);
        }

        public void AddWeapon(WeaponBase weapon)
        {
            var weaponSlotId = HasWeapon(weapon);
            if (weaponSlotId >= 0)
            {
                weapons[weaponSlotId].reloadBullets += weapon.reloadBullets + weapon.currentAmmo;
                Destroy(weapon.gameObject); 
            }
            else
            {
                weapon.transform.parent = humanoidCore.aimHelper;
                weapon.pB = humanoidCore;
                weapon.PutInInventory();
                weapon.ToggleRenderer(false);
                weapons.Add(weapon);
            }
        }

        public int WeaponCount()
        {
            return weapons.Count;
        }
        
        public int ItemCount()
        {
            return items.Count;
        }

        private int HasWeapon(WeaponBase weapon)
        {
            for(var i = 0; i < weapons.Count; i++)
            {
                if (weapons[i].weapon != weapon.weapon) continue;
                return i;
            }

            return -1;
        }
    }
}
