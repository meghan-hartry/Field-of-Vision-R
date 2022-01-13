using System.Collections;
using UnityEngine;

namespace FieldofVision
{
    internal class InputProcessing : MonoBehaviour
    {
        internal bool KeyPressed = false;

        internal float KeyPressedTime = 0;

        internal bool exited = false;

        internal IEnumerator WaitForInput()
        {
            Debug.Log("Input processing started.");
            // handle messages
            while (!MainExecution.Shutdown)
            {
                // Only record input once for each presentation.
                if (Input.anyKeyDown)
                {
                    // Record seen and response time in ms.
                    KeyPressed = true;
                    KeyPressedTime = Time.time;
                    Debug.Log("Key pressed at time: " + KeyPressedTime);
                }
                yield return null; // wait until next frame
            }
        }
    }
}
