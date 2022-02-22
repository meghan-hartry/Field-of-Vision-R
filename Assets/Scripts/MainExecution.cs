using UnityEngine;
using System;
using System.Threading;
using System.Collections.Concurrent;

namespace FieldofVision
{
    /// <summary>
    /// This class control the main execution of the OPI implementation on Unity engine.
    /// </summary>
    public class MainExecution : MonoBehaviour
    {
        #region Properties and Fields

        /// <summary>
        /// Reference to the class that controls screen drawing.
        /// </summary>
        internal ScreenDrawing Draw { get; private set; }

        /// <summary>
        /// Reference to the TCP Server class.
        /// </summary>
        internal TCPServer Server { get; private set; }

        internal static MainExecution MainInstance
        {
            get
            {
                if (Instance) return Instance;
                Instance = FindObjectOfType<MainExecution>();
                if (Instance) return Instance;
                return Instance = new GameObject(nameof(MainExecution)).AddComponent<MainExecution>();
            }
        }

        private readonly ConcurrentQueue<Action> ExecuteOnMainThread = new ConcurrentQueue<Action>();
        private static MainExecution Instance;
        private int ActionsExecuting = 0;

        #endregion

        #region MonoBehaviour Override Methods

        /// <summary>
        /// Start is called before the first frame update.
        /// Test is instantiated and started.
        /// </summary>
        void Start()
        {
            try
            {
                Application.runInBackground = true;

                // Create screen drawing class.
                Draw = gameObject.AddComponent<ScreenDrawing>();

                // Start Input Processing
                gameObject.AddComponent<InputProcessing>();

                // Create server and start listening for client connections.
                Server = new TCPServer();
                Server.StartListening();
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        /// <summary>
        /// Update is called once per frame.
        /// </summary>
        void Update()
        {
            lock (ExecuteOnMainThread) 
            {
                while (ExecuteOnMainThread.Count > 0 && ActionsExecuting < 1)
                {
                    Interlocked.Increment(ref ActionsExecuting);
                    ExecuteOnMainThread.TryDequeue(out var action);
                    action?.Invoke();
                }
            }
        }

        /// <summary>
        /// Called when an instance awakes in the game
        /// </summary>
        void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this; //set our static reference to our newly initialized instance
            DontDestroyOnLoad(gameObject);
        }

        void OnApplicationQuit()
        {
            Shutdown();
        }

        #endregion

        #region Internal Methods

        internal void Shutdown()
        {
            Server.Shutdown();

#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        internal void DoInMainThread(Action action)
        {
            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Enqueue(action);
            }
        }

        internal void DecrementActionsExecuting()
        {
            Interlocked.Decrement(ref ActionsExecuting);
        }

        #endregion
    }
}