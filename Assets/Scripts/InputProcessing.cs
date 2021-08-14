using System.Collections;
using UnityEngine;

namespace FieldofVision
{
    public class InputProcessing
    {
        internal static bool Exited = false;
        internal IEnumerator WaitForInput()
        {
            // handle messages
            while (!MainExecution.Shutdown)
            {
                // Only record input once for each presentation.
                if (Input.anyKeyDown)
                {
                    // Record seen and response time in ms.
                    //var keyPressed = true;
                    //var keyPressedTime = (float)(Time.time - keyPressedTime);
                    Debug.Log("Key pressed at time: " + Time.time);
                    //if (keyPressedTime <= 0) Debug.LogError("Error, key pressed time: " + keyPressedTime);
                }
                yield return null; // wait until next frame
            }
            Exited = true;
            Debug.Log("Shutting down Input Processing.");
        }
    }
}
