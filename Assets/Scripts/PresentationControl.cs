using Assets.Scripts.OPI_Definitions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace FieldofVision
{
    internal class PresentationControl : MonoBehaviour
    {
        /// <summary>
        /// List of the response received for each stimulus presentation.
        /// </summary>
        internal List<Response> Responses = new List<Response>();

        /// <summary>
        /// Stimulus GameObject in Unity.
        /// </summary>
        private GameObject StimulusObj;

        /// <summary>
        /// Active Camera GameObject in Unity.
        /// </summary>
        private GameObject ActiveCamera;

        /// <summary>
        /// Inactive Camera GameObject in Unity.
        /// </summary>
        private GameObject InactiveCamera;

        /// <summary>
        /// Start time of a stimulus presentation.
        /// </summary>
        private float PresentationStartTime;

        private MainExecution Main => MainExecution.MainInstance;

        /// <summary>
        /// Helper method for PresentCoroutine.
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
                Main.ErrorOccurred.Invoke(e.Message);
            }
            finally 
            {
                Interlocked.Decrement(ref Main.ActionsExecuting);
            }
        }

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
                    Main.ErrorOccurred.Invoke("Could not find Fixation GameObject.");
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
                Main.ErrorOccurred.Invoke(e.Message);
            }
            finally
            {
                Interlocked.Decrement(ref Main.ActionsExecuting);
            }
        }

        /// <summary>
        /// Presents a static stimulus for the given duration and wait for response.
        /// Response is added to the test's Responses list.
        /// </summary>
        private IEnumerator PresentCoroutine(StaticStimulus stimulus)
        {
            StimulusObj = GameObject.Find("Stimulus");
            if (StimulusObj == null)
            {
                Main.ErrorOccurred.Invoke("Could not find Stimulus GameObject.");
                yield break;
            }

            var fixationObj = GameObject.Find("Fixation");
            if (fixationObj == null)
            {
                Main.ErrorOccurred.Invoke("Could not find Fixation GameObject.");
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

            // Set visible
            StimulusObj.GetComponent<Renderer>().enabled = true;

            PresentationStartTime = Time.time;
            Debug.Log("Presentation start time: " + PresentationStartTime.ToString());

            // Wait for Duration of stimulus presentation. Convert ms to s.
            yield return new WaitForSeconds((float)stimulus.Duration / 1000);

            // Set invisible
            StimulusObj.GetComponent<Renderer>().enabled = false;

            // How long to wait in ms after stimulus is presented for a response.
            var wait = stimulus.ResponseWindow - stimulus.Duration;

            if (wait > 0)
            {
                // Wait for Response Window after stimulus presentation in seconds.
                yield return WaitForResponseCoroutine((float)wait / 1000);
            }
            Debug.Log("Presentation completed.");
        }

        private IEnumerator WaitForResponseCoroutine(float timeoutMs)
        {
            // Reset keyPressed bool.
            Main.InputProcessor.KeyPressed = false;

            var timeoutSeconds = (float)timeoutMs / 1000;
            var previousCount = Responses.Count;
            float startTime = Time.time;
            float elapsed = 0;

            while (!Main.InputProcessor.KeyPressed && elapsed < timeoutSeconds)
            {
                elapsed = Time.time - startTime;

                yield return null;
            }

            var responseTime = (int)((Main.InputProcessor.KeyPressedTime - PresentationStartTime) * 1000);
            var response = Main.InputProcessor.KeyPressed ? new Response(true, responseTime) : new Response(false);

            Responses.Add(response);

            Debug.Log("Responses count: " + Responses.Count);
            Debug.Log("Response: " + Responses[^1].Seen);
            Debug.Log("Response time: " + Responses[^1].Time);

            // Send Response
            var lastResponse = Main.PresentationControl.Responses[previousCount];

            // Convert the string data to byte data using ASCII encoding.  
            List<byte> byteList = new List<byte>();
            byteList.AddRange(BitConverter.GetBytes(lastResponse.Seen));
            byteList.AddRange(BitConverter.GetBytes(lastResponse.Time));

            var byteData = byteList.ToArray();

            Main.Server.Write(byteData);
            Debug.Log("Found response.");
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
    }
}
