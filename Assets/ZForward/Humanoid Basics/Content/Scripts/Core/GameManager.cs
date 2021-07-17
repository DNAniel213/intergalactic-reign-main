/*
 * GameManager.cs - Basement Media
 * @version: 1.0.0
*/

using Humanoid_Basics.Core.Helpers;
using Humanoid_Basics.Core.Scriptables;
using Humanoid_Basics.Player;
using Humanoid_Basics.Weapon;
using UnityEngine;
using UnityEngine.UI;

namespace Humanoid_Basics.Core
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Main")]
        public GameObject player;
        public PlayerController playerController;
        public HumanoidCore humanoidCore;
        public HumanoidHealth humanoidHealth;

        [Header("Databases")] 
        public InventoryDatabase inventoryDatabase;
        
        // Player UI
        [Header("Player UI")]
        public Text healthText;
        public GameObject weaponInfo;
        public Text weaponAmmo;
        public Image weaponIcon;
        public Image centerCross;
        public GameObject centerCrossBlock;
        private WeaponBase currentWeapon;
        
        [Header("General UI")]
        public AlertMessage alertMessage;

        [Header("Options")] public bool showFPS;
        
        
        // FPS
        private float deltaTime;
        
        private void Start()
        {
            humanoidCore.OnWeaponSwitch += UpdateWeaponUI;
            humanoidCore.OnWeaponShoot += UpdateAmmoUI;
            
            if (humanoidHealth)
                humanoidHealth.OnHealthChange += UpdateHealthUI;
        }

        private void Update()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            if (!currentWeapon || !humanoidCore.equippedWeapon) return;
            
            centerCross.enabled = humanoidCore.aim;
            weaponInfo.SetActive(true);
            var s = currentWeapon.currentRecoil * 4;
            centerCross.transform.localScale = new Vector3(s + 0.3f, s + 0.3f, 1);
        }

        private void UpdateHealthUI(int life)
        {
            healthText.text = life.ToString();
        }

        private void UpdateWeaponUI()
        {
            currentWeapon = humanoidCore.currentWeapon;
            if (!humanoidCore.equippedWeapon)
            {
                weaponInfo.SetActive(false);
                centerCross.enabled = false;
                return;
            }

            if (currentWeapon.centerCross)
            {
                centerCross.sprite = currentWeapon.centerCross;
            }

            weaponIcon.enabled = false;
            if (!currentWeapon.icon) return;
            weaponIcon.enabled = true;
            weaponIcon.sprite = currentWeapon.icon;

            UpdateAmmoUI();
        }

        private void UpdateAmmoUI()
        {
            weaponAmmo.text = currentWeapon.currentAmmo.ToString() + " / " + currentWeapon.reloadBullets.ToString();
        }

        private void OnGUI()
        {
            if (!showFPS) return;
            int w = Screen.width, h = Screen.height;
            GUIStyle style = new GUIStyle();
            Rect rect = new Rect(0, 0, w, h * 2 / 100);
            style.alignment = TextAnchor.UpperLeft;
            style.fontSize = h * 2 / 100;
            style.normal.textColor = new Color (0.0f, 0.0f, 0.5f, 1.0f);
            float msec = deltaTime * 1000.0f;
            float fps = 1.0f / deltaTime;
            string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
            GUI.Label(rect, text, style);
        }
    }
}
