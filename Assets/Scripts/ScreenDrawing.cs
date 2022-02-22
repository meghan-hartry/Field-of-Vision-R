using Assets.Scripts.OPI_Definitions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FieldofVision
{
    [Serializable]
    internal class KeyPressedEvent : UnityEvent<float> { }

    internal class ScreenDrawing : MonoBehaviour
    {
        #region Events

        private KeyPressedEvent KeyPressedEvent = new KeyPressedEvent();

        #endregion

        #region Properties and Fields

        private readonly List<Response> Responses = new List<Response>();
        private GameObject StimulusObj;
        private GameObject ActiveCamera;
        private GameObject InactiveCamera;
        private float PresentationStartTime = 0;
        private int NumberOfResponses = 0;

        private MainExecution Main => MainExecution.MainInstance;

        #endregion

        #region MonoBehavior Override Methods

        void Update()
        {
            // Record user input.
            if (Input.anyKeyDown)
            {
                var KeyPressedTime = Time.time;
                Debug.Log("Key pressed at time: " + KeyPressedTime);
                KeyPressedEvent.Invoke(KeyPressedTime);
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Presents a static stimulus for the given duration and wait for response.
        /// Response is added to the test's Responses list.
        /// </summary>
        internal void Present(Stimulus stimulus)
        {
            try
            {
                // Cast to StaticStimulus (pattern matching not available in Unity).
                var staticStimulus = stimulus as StaticStimulus;
                if (staticStimulus == null) { throw new NotImplementedException(); }
                Debug.Log("Starting presentation.");
                Main.DoInMainThread(() => { StartCoroutine(PresentCoroutine(staticStimulus)); });
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally 
            {
                Main.DecrementActionsExecuting();
            }
        }

        /// <summary>
        /// Sets the background, fixation, and active eye parameters.
        /// </summary>
        internal void SetBackground(Color color, FixationPoint fixationPoint, Eye eye) 
        {
            try
            {
                // Set background color
                ActiveCamera = GameObject.Find("Active Camera");
                ActiveCamera.GetComponent<Camera>().backgroundColor = color;

                var fixationObj = GameObject.Find("Fixation");
                if (fixationObj == null)
                {
                    Debug.LogError("Could not find Fixation GameObject.");
                    return;
                }

                // Set fixation position
                var zPos = fixationObj.transform.localPosition.z;
                fixationObj.transform.localPosition = new Vector3(fixationPoint.X, fixationPoint.Y, zPos);

                // Set fixation size
                var zScale = fixationObj.transform.localScale.z;
                fixationObj.transform.localScale = new Vector3(fixationPoint.SizeX, fixationPoint.SizeY, zScale);

                // Set fixation alpha
                var renderer = fixationObj.GetComponent<Renderer>();
                var fixationColor = renderer.material.color;
                color.a = fixationPoint.Alpha;
                renderer.material.color = fixationColor;

                // Set fixation color
                var spriteRenderer = fixationObj.GetComponent<SpriteRenderer>();
                spriteRenderer.color = fixationPoint.Color;

                // Set eye
                SetEye(eye);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
            finally
            {
                Main.DecrementActionsExecuting();
            }
        }

        #endregion

        #region Private Methods

        private void OnKeyPressed(float time)
        {
            var responseTime = (int)((time - PresentationStartTime) * 1000);
            var response = new Response(true, responseTime);

            Responses.Add(response);
        }

        private IEnumerator PresentCoroutine(StaticStimulus stimulus)
        {
            StimulusObj = GameObject.Find("Stimulus");
            if (StimulusObj == null)
            {
                Debug.LogError("Could not find Stimulus GameObject.");
                yield break;
            }

            var fixationObj = GameObject.Find("Fixation");
            if (fixationObj == null)
            {
                Debug.LogError("Could not find Fixation GameObject.");
                yield break;
            }

            // Set eye
            SetEye(stimulus.Eye);

            // Set level
            Debug.Log("Stimulus level: " + stimulus.Level);
            SetLevel(stimulus.Level);

            // Set position.
            var zPos = StimulusObj.transform.localPosition.z;
            StimulusObj.transform.localPosition = new Vector3(stimulus.X, stimulus.Y, zPos);

            // Set color
            var spriteRenderer = StimulusObj.GetComponent<SpriteRenderer>();
            spriteRenderer.color = stimulus.Color;

            // Set size
            var zScale = StimulusObj.transform.localScale.z;
            StimulusObj.transform.localScale = new Vector3(stimulus.Size, stimulus.Size, zScale);

            // Get how long to wait after stimulus is presented for a response.
            float secondsToWait = (stimulus.ResponseWindow - stimulus.Duration)/1000;
            if (secondsToWait < 0) secondsToWait = 0;

            // Record information
            NumberOfResponses = Responses.Count;
            PresentationStartTime = Time.time;

            // Show stimulus
            StimulusObj.GetComponent<Renderer>().enabled = true;

            // Listen for key presses
            KeyPressedEvent.AddListener(OnKeyPressed);

            // Wait for duration of stimulus presentation. Convert ms to s.
            yield return new WaitForSeconds((float)stimulus.Duration / 1000);

            // Hide stimulus
            StimulusObj.GetComponent<Renderer>().enabled = false;

            // Wait for the Response Window after stimulus presentation in seconds.
            float startTime = Time.time;
            while (Responses.Count == NumberOfResponses && Time.time - startTime < secondsToWait)
            {
                yield return null;
            }
            KeyPressedEvent.RemoveListener(OnKeyPressed);

            // Not seen
            if (Responses.Count == NumberOfResponses)
            {
                Responses.Add(new Response(false));
            }

            // Send first response added in the response window.
            var lastResponse = Main.Draw.Responses[NumberOfResponses]; // NumberOfResponses + 1 because of indexing

            // Convert the string data to byte data using ASCII encoding.  
            List<byte> byteData = new List<byte>();
            byteData.AddRange(BitConverter.GetBytes(lastResponse.Seen));
            byteData.AddRange(BitConverter.GetBytes(lastResponse.Time));

            Main.Server.Write(byteData.ToArray());
            Debug.Log("Presentation completed.");
        }

        private void SetLevel(float percent)
        {
            if (percent > 100) 
            {
                Debug.Log("Stimulus level was out of range: " + percent + "%. Reset to 100% (opaque)");
                percent = 100;
            }
            else if (percent < 0)
            {
                Debug.Log("Stimulus level was out of range: " + percent + "%. Reset to 0% (transparent)");
                percent = 0;
            }

            Debug.Log("Stimulus alpha: " + percent);
            var renderer = StimulusObj.GetComponent<Renderer>();
            var color = renderer.material.color;
            color.a = percent;
            renderer.material.color = color;
        }

        private void SetEye(Eye eye)
        {
            Debug.Log("Eye: " + eye);
            ActiveCamera = GameObject.Find("Active Camera");
            InactiveCamera = GameObject.Find("Inactive Camera");
            switch (eye) 
            {
                case Eye.Left:
                    ActiveCamera.GetComponent<Camera>().stereoTargetEye = StereoTargetEyeMask.Left;
                    InactiveCamera.GetComponent<Camera>().stereoTargetEye = StereoTargetEyeMask.Right;
                    break;
                case Eye.Right:
                    ActiveCamera.GetComponent<Camera>().stereoTargetEye = StereoTargetEyeMask.Right;
                    InactiveCamera.GetComponent<Camera>().stereoTargetEye = StereoTargetEyeMask.Left;
                    break;
                case Eye.Both:
                default:
                    ActiveCamera.GetComponent<Camera>().stereoTargetEye = StereoTargetEyeMask.Both;
                    InactiveCamera.GetComponent<Camera>().stereoTargetEye = StereoTargetEyeMask.None;
                    break;
            }
        }

        #endregion
    }
}
