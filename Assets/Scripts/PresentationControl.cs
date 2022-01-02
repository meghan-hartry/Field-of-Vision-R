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

        internal MainExecution Main { get; set; }

        /// <summary>
        /// Helper method for PresentCoroutine.
        /// </summary>
        /// <inheritdoc cref="Present"/>
        internal void Present(Stimulus stimulus)
        {
            // Cast to StaticStimulus (can't use pattern matching with Unity).
            var staticStimulus = stimulus as StaticStimulus;
            if (staticStimulus == null) { throw new NotImplementedException(); }
            Debug.Log("Starting presentation.");
            Main.ExecuteOnMainThread.Enqueue(() => { StartCoroutine(this.PresentCoroutine(staticStimulus)); });
        }

        /// <summary>
        /// Presents a static stimulus for the given duration and wait for response.
        /// Response is added to the test's Responses list.
        /// </summary>
        private IEnumerator PresentCoroutine(StaticStimulus stimulus)
        {
            this.PresentDone = false;

            // Reset keyPressed bool.
            Main.InputProcessor.KeyPressed = false;

            //Debug.Log("Stimuli count: " + this.Tests[this.CurrentTest].Stimuli.Count);

            StimulusObj = GameObject.Find("Stimulus");
            if (StimulusObj == null) 
            {
                Debug.LogError("Could not find Stimulus GameObject.");
                yield break;
            }

            Debug.Log("Eye: " + stimulus.Eye);
            ActiveCamera = GameObject.Find("Active Camera");
            InactiveCamera = GameObject.Find("Inactive Camera");
            SetEye(stimulus.Eye);

            Debug.Log("Stimulus level: " + stimulus.Level);
            //this.SetLevel(stimulus.Level);
            this.SetPosition(stimulus.X, stimulus.Y);
            StimulusObj.GetComponent<Renderer>().enabled = true;

            PresentationStartTime = Time.time;
            Debug.Log("Presentation start time: " + PresentationStartTime.ToString());

            //yield for Duration of stimulus presentation. Convert ms to s.
            yield return new WaitForSeconds((float)stimulus.Duration / 1000);

            StimulusObj.GetComponent<Renderer>().enabled = false;

            // How long to wait in ms after stimulus is presented for a response.
            var wait = (stimulus.ResponseWindow - stimulus.Duration) / 1000;

            if (wait > 0)
            {
                //yield for Response Window after stimulus presentation.
                yield return new WaitForSeconds((float)wait);
            }

            var responseTime = (int) ((Main.InputProcessor.KeyPressedTime - PresentationStartTime) * 1000);
            var response = Main.InputProcessor.KeyPressed? new Response(true, responseTime) : new Response(false);

            this.Responses.Add(response);

            Debug.Log("Responses count: " + this.Responses.Count);
            Debug.Log("Response: " + this.Responses[this.Responses.Count - 1].Seen);
            Debug.Log("Response time: " + this.Responses[this.Responses.Count - 1].Time);

            Main.MessageProcessor.WaitForResponse = false;
            this.PresentDone = true;
        }

        private void SetLevel(double cd)
        {
            var level = this.Device.ToAlpha(cd);
            Debug.Log("Stimulus alpha: " + level);
            var renderer = StimulusObj.GetComponent<Renderer>();
            var color = renderer.material.color;
            color.a = (float)level;
            renderer.material.color = color;
        }

        private void SetPosition(double x, double y)
        {
            var vector = this.Device.ToVector(x, y);
            this.StimulusObj.transform.position = new Vector3(vector[0], vector[1], 0);
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
