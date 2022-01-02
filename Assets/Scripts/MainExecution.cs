using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

namespace FieldofVision
{
    /// <summary>
    /// This class defines the OPI implementation on Unity engine.
    /// </summary>
    public class MainExecution : MonoBehaviour
    {        
        internal PresentationControl PresentationControl { get; private set; }

        internal InputProcessing InputProcessor { get; private set; }

        internal SocketServer SocketServer { get; private set; }

        internal MessageProcessing MessageProcessor { get; private set; }

        internal Task SocketServerTask;

        internal bool Shutdown { get; set; } = false;

        internal Queue<Action> ExecuteOnMainThread = new Queue<Action>();

        internal MainExecution MainInstance;

        /// <summary>
        /// Start is called before the first frame update.
        /// Test is instantiated and started.
        /// </summary>
        void Start()
        {
            try
            {
                Application.runInBackground = true;

                PresentationControl = gameObject.AddComponent<PresentationControl>();
                PresentationControl.Main = this;
                InputProcessor = gameObject.AddComponent<InputProcessing>();
                InputProcessor.Main = this;

                SocketServer = new SocketServer(this);
                MessageProcessor = new MessageProcessing(this);

                SocketServerTask = Task.Run(SocketServer.StartListening);
                StartCoroutine(InputProcessor.WaitForInput());
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        /// <summary>
        /// Update is called once per frame.
        /// </summary>
        void Update()
        {
            while (ExecuteOnMainThread.Count > 0)
            {
                ExecuteOnMainThread.Dequeue().Invoke();
            }
        }

        //called when an instance awakes in the game
        void Awake()
        {
            MainInstance = this; //set our static reference to our newly initialized instance
        }

        void OnApplicationQuit()
        {
            RunShutdown();
        }

        internal void RunShutdown()
        {
            Debug.Log("Shutdown = true");
            Shutdown = true;

            SocketServer.ForceShutdown();

            MainInstance.StopAllCoroutines();

#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}