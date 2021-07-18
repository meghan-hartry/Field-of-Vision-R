using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using OPI_.NET;
using OPI_.NET.StimulusTypes;
using UnityEngine;

namespace FieldofVision
{
    /// <summary>
    /// This class defines the OPI implementation on Unity engine.
    /// </summary>
    public class UnityOPI : MonoBehaviour, IOPI
    {
        System.Threading.Thread SocketThread;

        /// <summary>
        /// Reference to the Stimulus Prefab. A Prefab is drug into this field in the Inspector.
        /// </summary>
        public GameObject stimulusPrefab;

        /// <summary>
        /// Holds an instantiated stimulus GameObject during presentation.
        /// </summary>
        public GameObject stimulusObject;

        /// <summary>
        /// Status of presentation. Used for flow control.
        /// </summary>
        private bool presentDone;

        /// <summary>
        /// True if key press is detected.
        /// </summary>
        private bool keyPressed;

        /// <summary>
        /// Time after presentation that key press is detected.
        /// </summary>
        private float keyPressedTime;

        ///<inheritdoc />
        public IDevice Device { get; set; }

        /// <summary>
        /// Device as UnityDevice type. Use this for extended functionality.
        /// </summary>
        public IUnityDevice UnityDevice { get; set; }

        ///<inheritdoc />
        public string DeviceType { get; set; }

        ///<inheritdoc />
        public Eye Eye { get; set; }

        public List<Response> Responses = new List<Response>();

        private System.Random random = new System.Random();

        /// <summary>
        /// Index of current test being stepped.
        /// </summary>
        private int CurrentTest { get; set; } = 0;
        
        #region Flow Methods
        /// <summary>
        /// Start is called before the first frame update.
        /// Test is instantiated and started.
        /// </summary>
        void Start()
        {
            Application.runInBackground = true;

            this.Device = new ViveProEye();
            this.DeviceType = "VIVE";

            // Cast to IUnityDevice (can't use pattern matching with Unity).
            this.UnityDevice = this.Device as IUnityDevice;
            if (this.UnityDevice == null) { throw new NotImplementedException(); }

            SocketThread = new Thread(SocketServer.StartListening);
            SocketThread.IsBackground = true;
            SocketThread.Start();
        }

        /// <summary>
        /// Update is called once per frame.
        /// Listen for key presses.
        /// </summary>
        void Update()
        {
            // Only record input once for each presentation.
            if (!this.keyPressed && Input.anyKeyDown)
            {
                // Record seen and response time in ms.
                this.keyPressed = true;
                this.keyPressedTime = (Time.time - this.keyPressedTime);
                Debug.Log("Record key pressed time: " + this.keyPressedTime);
                if (this.keyPressedTime <= 0) Debug.LogError("Error, key pressed time: " + this.keyPressedTime);
            }
        }

        void OnDisable()
        {
            //stop thread
            if (SocketThread != null)
            {
                SocketThread.Abort();
            }

            //SocketServer.StopListening();
        }

        #endregion

        #region Present Methods
        /// <summary>
        /// Helper method for PresentCoroutine.
        /// </summary>
        /// <inheritdoc cref="Present"/>
        public Response Present(Stimulus stimulus)
        {
            // Cast to StaticStimulus (can't use pattern matching with Unity).
            var staticStimulus = stimulus as StaticStimulus;
            if (staticStimulus == null) { throw new NotImplementedException(); }

            StartCoroutine(this.PresentCoroutine(staticStimulus));

            return null;
        }

        /// <summary>
        /// Presents a static stimulus for the given duration and wait for response.
        /// Response is added to the test's Responses list.
        /// </summary>
        private IEnumerator PresentCoroutine(StaticStimulus stimulus)
        {
            this.presentDone = false;

            // Reset keyPressed bool.
            this.keyPressed = false;

            //Debug.Log("Stimuli count: " + this.Tests[this.CurrentTest].Stimuli.Count);

            this.stimulusObject = Instantiate(stimulusPrefab);
            this.stimulusObject.SetActive(false);
            Debug.Log("Stimulus level: " + stimulus.Level);
            this.SetLevel(stimulus.Level);
            this.SetPosition(stimulus.X, stimulus.Y);
            this.stimulusObject.SetActive(true);

            // Record presentation start time.
            this.keyPressedTime = Time.time;
            Debug.Log("Record presentation start time: " + this.keyPressedTime);

            //yield for Duration of stimulus presentation. Convert ms to s.
            yield return new WaitForSeconds((float)stimulus.Duration / 1000);

            Destroy(this.stimulusObject);

            // How long to wait in ms after stimulus is presented for a response.
            var wait = (stimulus.ResponseWindow - stimulus.Duration) / 1000;

            if (wait > 0)
            {
                //yield for Response Window after stimulus presentation.
                yield return new WaitForSeconds((float)wait);
            }

            var response = new Response(false);
            if (this.keyPressed == true)
            {
                response = new Response(true, this.keyPressedTime);
            }

            this.Responses.Add(response);

            Debug.Log("Responses count: " + this.Responses.Count);
            Debug.Log("Response: " + this.Responses[this.Responses.Count - 1].Seen);
            Debug.Log("Response time: " + this.Responses[this.Responses.Count - 1].Time);

            this.presentDone = true;
        }
        #endregion

        #region Stimulus Methods
        ///<inheritdoc />
        public void SetLevel(double cd)
        {
            var level = this.UnityDevice.ToAlpha(cd);
            Debug.Log("Stimulus alpha: " + level);
            var renderer = stimulusObject.GetComponent<Renderer>();
            var color = renderer.material.color;
            color.a = (float)level;
            renderer.material.color = color;
        }

        ///<inheritdoc />
        public void SetPosition(double x, double y)
        {
            var vector = this.UnityDevice.ToVector(x, y);
            this.stimulusObject.transform.position = new Vector3(vector[0], vector[1], 0);
        }
        #endregion
    }
}