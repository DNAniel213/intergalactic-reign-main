using UnityEngine;

namespace Humanoid_Basics.Player
{
    public class HumanoidEquipment : MonoBehaviour
    {
        [Header("Humanoid Core")]
        public HumanoidCore humanoidCore;
        
        // Current Weapon in Weapon Slot
        public int weaponSlot = 0;
        
        private void Start()
        {
            humanoidCore = GetComponent<HumanoidCore>();
        }
        
        // 
        public void UseEquipment(int itemSlot)
        {
            // Set our Humanoid to Load the weapon attached to the body
            weaponSlot = itemSlot;
            humanoidCore.SwitchWeapon(itemSlot);
        }
        
    }
}
