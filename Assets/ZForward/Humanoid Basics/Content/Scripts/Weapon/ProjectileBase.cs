using Humanoid_Basics.Player;
using UnityEngine;

namespace Humanoid_Basics.Weapon
{
    public class ProjectileBase : MonoBehaviour {

        public float force = 2000;
        public DoForce addForce = DoForce.AtStart;

        private Rigidbody rb;

        public int damage = 70;
        public float explosionForce = 1000;
        public float explosionRadius = 30;

        public GameObject particle;
        public GameObject trail;

        public enum DoForce
        {
            AtStart,
            InFixedUpdate
        }

        // Use this for initialization
        void Start()
        {

            rb = GetComponent<Rigidbody>();
            if (addForce == DoForce.AtStart)
            {
                rb.AddForce(transform.forward * force, ForceMode.Acceleration);
            }
        }
	
        // Update is called once per frame
        void FixedUpdate () {

            if (addForce == DoForce.InFixedUpdate)
            {
                rb.AddForce(transform.forward * force, ForceMode.Acceleration);
            }
        }
        void OnCollisionEnter()
        {
            // get all the colliders inside the radius
            const int maxColliders = 50;
            var hitColliders = new Collider[maxColliders];
            var numColliders = Physics.OverlapSphereNonAlloc(transform.position, explosionRadius, hitColliders);
            
            
            for (var i = 0; i < numColliders; i++)
            {
                var t = hitColliders[i];
                
                var r = t.GetComponent<Rigidbody>();
                if (!r) continue;
                
                // Check if we found a Humanoid if so, ragdoll...
                if (t.gameObject.layer == (LayerMask.NameToLayer("Humanoid")))
                {
                    var humanoidCore = r.transform.root.GetComponent<HumanoidCore>();
                    if (humanoidCore)
                    {
                        var humanoidHealth = humanoidCore.GetComponent<HumanoidHealth>();
                        if (humanoidHealth)
                        {
                            humanoidHealth.Damage(damage / (int) Vector3.Distance(transform.position,
                                humanoidCore.transform.position));
                        }

                        if (!humanoidCore.ragdollHelper.ragdolled)
                        {
                            humanoidCore.ToggleRagdoll();
                        }
                    }
                }
                
                r.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
            
            Instantiate(particle, transform.position, Quaternion.identity);
            if (trail)
            {
                trail.transform.parent = null;
            }
            Destroy(gameObject);
        }
    }
}