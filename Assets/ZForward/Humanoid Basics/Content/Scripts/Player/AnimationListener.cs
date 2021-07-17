using UnityEngine;

namespace Humanoid_Basics.Player
{
    public class AnimationListener : MonoBehaviour {

        private TransformPathMaker pathMaker;
        private HumanoidCore humanoidCore;
        private static readonly int IsAttacking = Animator.StringToHash("CanAttack");
        private static readonly int CanAttackCombo = Animator.StringToHash("CanAttackCombo");
        private static readonly int CanAttackComboFinish = Animator.StringToHash("CanAttackComboFinish");

        private void Start()
        {
            pathMaker = GetComponent<TransformPathMaker>();
            humanoidCore = transform.parent.GetComponent<HumanoidCore>();
        }
        public void PlayPathMaker()
        {
            pathMaker.Play();
        }
        public void ResetPathMaker()
        {
            pathMaker.Reset();
        }
        public void NextClimbState()
        {
            pathMaker.NextState();
        }
        public void Jump()
        {
            humanoidCore.Jump();
        }

        public void ResetAttackState()
        {
            humanoidCore.isAttacking = false;
            humanoidCore.playerAnimator.SetBool(IsAttacking, false);
            humanoidCore.playerAnimator.SetBool(CanAttackCombo, false);
            humanoidCore.playerAnimator.SetBool(CanAttackComboFinish, false);
        }
    }
}
