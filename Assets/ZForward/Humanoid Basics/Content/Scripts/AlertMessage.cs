using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Humanoid_Basics
{
    public class AlertMessage : MonoBehaviour
    {

        public float alertTime = 3;
        public float breakTime = 1;
        
        private List<string> messages;
        private bool showMessage;
        private bool processingBuffer;
        private GUIStyle fontStyle;

        private void Start()
        {
            messages = new List<string>();
            fontStyle = new GUIStyle {fontSize = 22, alignment = TextAnchor.UpperCenter};
            // fontStyle = GUI.skin.GetStyle("Label");
        }

        public void AddAlert(string message)
        {
            messages.Add(message);
        }

        // Update is called once per frame
        private void Update()
        {
            if (messages.Count > 0 && processingBuffer == false)
            {
                StartCoroutine(ProcessMessageBuffer());
            }
        }
        
        private IEnumerator ProcessMessageBuffer()
        {
            processingBuffer = true;
            showMessage = true;
            yield return new WaitForSeconds(alertTime);
            showMessage = false;
            messages.RemoveAt(0);
            yield return new WaitForSeconds(breakTime);
            processingBuffer = false;
        }

        private void OnGUI()
        {
            if (!showMessage) return;

            GUI.Label(new Rect(Screen.width / 2-200, Screen.height / 2-200, 400, 100), messages.First(), fontStyle);
        }
    }
}
