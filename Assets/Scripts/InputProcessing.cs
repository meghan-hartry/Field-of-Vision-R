using System.Collections;
using UnityEngine;

namespace FieldofVision
{
    internal class InputProcessing : MonoBehaviour
    {
        internal bool KeyPressed = false;

        internal float KeyPressedTime = 0;

        internal MainExecution Main { get; set; }

        internal IEnumerator WaitForInput()
        {
            Debug.Log("Wait for input started.");
            // handle messages
            while (!Main.Shutdown)
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
