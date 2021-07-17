using System;
using Humanoid_Basics.Npc;
using UnityEngine;

namespace Humanoid_Basics.Player
{
    public class HumanoidHealth : MonoBehaviour
    {
        [Header("Humanoid Core")]
        public HumanoidCore humanoidCore;

        [Header("Settings")]
        public int health = 100;
        public int maxHealth = 100;
        public bool isDead = false;
        
        // UI Event
        public event Action<int> OnHealthChange;
        
        private void Start()
        {
            humanoidCore = GetComponent<HumanoidCore>();
        }
        
        ////////////////////////
        ////// Public API //////
        ////////////////////////
        
        // Get Humanoid Death Status
        public bool IsDead()
        {
            return isDead;
        }
        
        // Set Humanoid Health
        public void SetHealth(int hp)
        {
            health = hp;
        }
        
        // Get Humanoid Health
        public int GetHealth()
        {
            return health;
        }
        
        // Heal Humanoid
        public void Heal(int hp)
        {
            if (isDead) return;
            health += hp;
            if (health > maxHealth) health = maxHealth;
            
            if (humanoidCore.humanoidType == HumanoidCore.Type.Player)
                OnHealthChange?.Invoke(health);
        }
        
        // Damage Humanoid
        public void Damage(int hp)
        {
            if (isDead) return;
            health -= hp;
            if (health <= 0) Kill();
            if (humanoidCore.humanoidType == HumanoidCore.Type.Player)
                OnHealthChange?.Invoke(health);
        }

        // Revive a Dead Humanoid
        public void Revive()
        {
            if (!isDead) return;
            SetHealth(maxHealth);
            humanoidCore.Start();
            humanoidCore.ToggleRagdoll();
            if (humanoidCore.humanoidType == HumanoidCore.Type.Player)
                OnHealthChange?.Invoke(health);
        }

        // Kill Humanoid
        public void Kill()
        {
            isDead = true;
            SetHealth(0);
            
            // Set our Humanoid Core to stop processing
            humanoidCore.humanoidStatus = HumanoidCore.Status.Dead;

            if (humanoidCore.humanoidType == HumanoidCore.Type.Npc)
            {
                var npcAi = GetComponent<NpcBehaviour>();
                npcAi.aiType = NpcBehaviour.AITarget.Idle;
                npcAi.SetAgentDestination(null, true);
                humanoidCore.SetXAxis(0);
                humanoidCore.SetYAxis(0);
                npcAi.agent.isStopped = true;
                npcAi.agent.updatePosition = false;
            }
            
            // Lets toggle ragdoll affect
            if (!humanoidCore.ragdollHelper.ragdolled)
            {
                humanoidCore.ToggleRagdoll();
            }
            
            if (humanoidCore.humanoidType == HumanoidCore.Type.Player)
                OnHealthChange?.Invoke(health);
        }
        
    }
}
