using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Assets.Scripts;
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

        internal MessageProcessing MessageProcessor = new MessageProcessing();

        internal static bool Shutdown { get; set; } = false;

        private readonly Queue<Action> ExecuteOnMainThread = new Queue<Action>();

        private Task SocketServerTask;

        private static MainExecution instance;

        internal static MainExecution MainInstance
        {
            get
            {
                if (instance) return instance;

                instance = FindObjectOfType<MainExecution>();

                if (instance) return instance;

                return instance = new GameObject(nameof(MainExecution)).AddComponent<MainExecution>();
            }
        }

        public ErrorOccurredEvent ErrorOccurred;

        /// <summary>
        /// Popup GameObject in Unity.
        /// </summary>
        private GameObject Popup;

        /// <summary>
        /// PopupMessage GameObject in Unity.
        /// </summary>
        private GameObject PopupMessage;

        /// <summary>
        /// Start is called before the first frame update.
        /// Test is instantiated and started.
        /// </summary>
        void Start()
        {
            try
            {
                Application.runInBackground = true;

                ErrorOccurred = new ErrorOccurredEvent();
                ErrorOccurred.AddListener(OnErrorOccurredHelper);

                PresentationControl = gameObject.AddComponent<PresentationControl>();
                InputProcessor = gameObject.AddComponent<InputProcessing>();

                SocketServerTask = Task.Run(TCPServer.StartListening);
                StartCoroutine(InputProcessor.WaitForInput());
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
                ErrorOccurred.Invoke(e.Message);
            }
        }

        /// <summary>
        /// Update is called once per frame.
        /// </summary>
        void Update()
        {
            lock (ExecuteOnMainThread) 
            {
                while (ExecuteOnMainThread.Count > 0)
                {
                    var action = ExecuteOnMainThread.Dequeue();
                    action?.Invoke();
                }
            }
        }

        //called when an instance awakes in the game
        void Awake()
        {
            if (instance && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this; //set our static reference to our newly initialized instance
            DontDestroyOnLoad(gameObject);
        }

        void OnApplicationQuit()
        {
            RunShutdown();
        }

        internal void DoInMainThread(Action action)
        {
            lock (ExecuteOnMainThread)
            {
                ExecuteOnMainThread.Enqueue(action);
            }
        }

        internal void RunShutdown()
        {
            Debug.Log("Shutting down...");
            Shutdown = true;

            TCPServer.Disconnect();

#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnErrorOccurredHelper(string message)
        {
            ExecuteOnMainThread.Enqueue(() => { StartCoroutine(OnErrorOccurred(message)); });
        }

        private IEnumerator OnErrorOccurred(string message)
        {
            InputProcessor.KeyPressed = false;

            Popup = GameObject.Find("Popup");
            if (Popup == null)
            {
                ErrorOccurred.Invoke("Could not find Popup GameObject.");
                yield break; 
            }

            PopupMessage = GameObject.Find("Message");
            if (PopupMessage == null)
            {
                ErrorOccurred.Invoke("Could not find PopupMessage GameObject.");
                yield break;
            }

            // Update message
            PopupMessage.GetComponent<UnityEngine.UI.Text>().text = message + " Press any key to continue.";

            // show popup
            Popup.GetComponent<Canvas>().enabled = true;

            Debug.Log("Unity Event called with argument: " + message);

            yield return WaitForInput();

            // hide popup
            Popup.GetComponent<Canvas>().enabled = false;
        }

        private IEnumerator WaitForInput() 
        {
            while (!InputProcessor.KeyPressed)
            {
                yield return null;
            }
        }

        private IEnumerator WaitForInputWithTimeout(float timeout)
        {
            float startTime = Time.time;
            float elapsed = 0;

            while (!InputProcessor.KeyPressed && elapsed < timeout)
            {
                elapsed = Time.time - startTime;

                yield return null;
            }
        }
    }
}