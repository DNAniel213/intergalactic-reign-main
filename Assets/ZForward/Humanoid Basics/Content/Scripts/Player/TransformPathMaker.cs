using UnityEngine;

namespace Humanoid_Basics.Player
{
    public class TransformPathMaker : MonoBehaviour {
        public bool play;
        public Transform reference;
        private Rigidbody rb;
        public int state;
        public Vector3[] points;
        public float[] pointsTime;
        public Vector3 correctPosition;
        public Quaternion correctRotation;

        private void Start () {
            rb = GetComponent<Rigidbody>();   
        }

        private void Update () {

            if (play)
            {
                MoveTo();
            }
        }
        public void NextState()
        {
            if (state < points.Length)
            {
                state++;
                if (state < points.Length)
                {
                    CorrectPosition();
                }
                else { Reset(); }
                return;
            }
        }
        void MoveTo()
        {
            if (state < points.Length)
            {
                transform.position = Vector3.Lerp(transform.position, correctPosition, pointsTime[state] * Time.deltaTime);
            }
            
            //transform.rotation = Quaternion.Lerp(transform.rotation, correctRotation, pointsTime[state] * Time.deltaTime);
        }
        public void Reset()
        {
            if (rb)
                rb.isKinematic = false;
            play = false;
            state = 0;
        }
        public void Play()
        {
            if (play == false)
            {
                CorrectPosition();
                rb.isKinematic = true;
                play = true;
            }
        }
        void CorrectPosition()
        {
        
            Vector3 x = reference.right * points[state].x;
            Vector3 y = new Vector3(0, points[state].y,0);
            Vector3 z = reference.forward * points[state].z;

            Vector3 toGo = reference.position + x + y + z;

            correctPosition = new Vector3(toGo.x, y.y, toGo.z);
        }
    }
}
