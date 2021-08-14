using System.Collections.Concurrent;
using UnityEngine;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections;

namespace FieldofVision
{
    /// <summary>
    /// This class defines the OPI implementation on Unity engine.
    /// </summary>
    public class MainExecution : MonoBehaviour
    {
        internal static bool Shutdown { get; private set; } = false;

        internal static ConcurrentQueue<string> Messages { get; private set; }
        internal static Queue<Action> ExecuteOnMainThread = new Queue<Action>();

        internal static PresentationControl PresentationControl { get; private set; }

        private static Task SocketServerTask;
        private static MainExecution instance;

        /// <summary>
        /// Start is called before the first frame update.
        /// Test is instantiated and started.
        /// </summary>
        void Start()
        {
            try
            {
                Application.runInBackground = true;
                Messages = new ConcurrentQueue<string>();

                PresentationControl = gameObject.AddComponent<PresentationControl>();

                SocketServerTask = Task.Run(new SocketServer().StartListening);
                StartCoroutine(new MessageProcessing().BeginProcessing());
                StartCoroutine(new InputProcessing().WaitForInput());
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

        void OnDisable()
        {
            RunShutdownHelper();
        }

        //called when an instance awakes in the game
        void Awake()
        {
            instance = this; //set our static reference to our newly initialized instance
        }

        private static IEnumerator RunShutdown() 
        {
            Shutdown = true;
            if(SocketServer.Listener != null)
                   SocketServer.Listener.Close();

            if (!SocketServerTask.IsCompleted)
                SocketServerTask.Wait();

            while (!InputProcessing.Exited)
                yield return new WaitForSeconds(1f);

            while (!MessageProcessing.Exited)
                yield return new WaitForSeconds(1f);

            #if UNITY_EDITOR
                // Application.Quit() does not work in the editor so
                // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        public static void RunShutdownHelper() 
        {
            ExecuteOnMainThread.Enqueue(() => { instance.StartCoroutine(RunShutdown()); });
        }
    }
}