using Assets.Scripts.OPI_Definitions;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FieldofVision
{
    internal class PresentationControl : MonoBehaviour
    {
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

        internal List<Response> Responses = new List<Response>();

        /// <summary>
        /// Status of presentation. Used for flow control.
        /// </summary>
        internal bool PresentDone = false;

        internal float PresentationStartTime;

        internal ViveProEye Device { get; private set; } = new ViveProEye();

        private MainExecution Main => MainExecution.MainInstance;

        /// <summary>
        /// Helper method for PresentCoroutine.
        /// </summary>
        /// <inheritdoc cref="Present"/>
        internal void Present(Stimulus stimulus)
        {
            try
            {
                // Cast to StaticStimulus (can't use pattern matching with Unity).
                StaticStimulus staticStimulus = stimulus as StaticStimulus;
                if (staticStimulus == null) { throw new NotImplementedException(); }
                Debug.Log("Starting presentation.");
                Main.DoInMainThread(() => { StartCoroutine(this.PresentCoroutine(staticStimulus)); });
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                Main.ErrorOccurred.Invoke(e.Message);
            }
        }

        internal void WaitForResponse(float timeout)
        {
            Main.DoInMainThread(() => { StartCoroutine(WaitForResponseCoroutine(timeout)); });
        }

        /// <summary>
        /// Presents a static stimulus for the given duration and wait for response.
        /// Response is added to the test's Responses list.
        /// </summary>
        private IEnumerator PresentCoroutine(StaticStimulus stimulus)
        {
            this.PresentDone = false;

            //Debug.Log("Stimuli count: " + this.Tests[this.CurrentTest].Stimuli.Count);

            StimulusObj = GameObject.Find("Stimulus");
            if (StimulusObj == null) 
            {
                Main.ErrorOccurred.Invoke("Could not find Stimulus GameObject.");
                yield break;
            }

            Debug.Log("Eye: " + stimulus.Eye);
            ActiveCamera = GameObject.Find("Active Camera");
            InactiveCamera = GameObject.Find("Inactive Camera");
            SetEye(stimulus.Eye);

            Debug.Log("Stimulus level: " + stimulus.Level);
            this.SetLevel(stimulus.Level);
            this.SetPosition(stimulus.X, stimulus.Y);
            StimulusObj.GetComponent<Renderer>().enabled = true;

            PresentationStartTime = Time.time;
            Debug.Log("Presentation start time: " + PresentationStartTime.ToString());

            // Wait for Duration of stimulus presentation. Convert ms to s.
            yield return new WaitForSeconds((float)stimulus.Duration / 1000);

            StimulusObj.GetComponent<Renderer>().enabled = false;

            // How long to wait in ms after stimulus is presented for a response.
            var wait = (stimulus.ResponseWindow - stimulus.Duration) / 1000;

            if (wait > 0)
            {
                // Wait for Response Window after stimulus presentation.
                yield return WaitForResponseCoroutine((float)wait);
            }

            this.PresentDone = true;
        }

        private IEnumerator WaitForResponseCoroutine(float timeout)
        {
            // Reset keyPressed bool.
            Main.InputProcessor.KeyPressed = false;

            var previousCount = Responses.Count;
            float startTime = Time.time;
            float elapsed = 0;

            while (!Main.InputProcessor.KeyPressed && elapsed < timeout)
            {
                elapsed = Time.time - startTime;

                yield return null;
            }

            var responseTime = (int)((Main.InputProcessor.KeyPressedTime - PresentationStartTime) * 1000);
            var response = Main.InputProcessor.KeyPressed ? new Response(true, responseTime) : new Response(false);

            this.Responses.Add(response);

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

            TCPServer.Write(byteData);
            Debug.Log("Found response.");
        }

        internal void SetBackground(Color color) 
        {
            ActiveCamera = GameObject.Find("Active Camera");
            ActiveCamera.GetComponent<Camera>().backgroundColor = color;
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

        private void SetPosition(float x, float y)
        {
            this.StimulusObj.transform.position = new Vector3(x, y, 0);
        }

        private void SetEye(Eye eye)
        {
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
