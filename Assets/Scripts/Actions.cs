using System;
using System.Linq;
using UnityEngine;

public class Actions
{
    internal void ProcessMessage(string message)
    {
        string[] commands = new string[] { "OPI_GET_RES", "OPI_IMAGE", "OPI_PRESENT", "OPI_SET_BGROUND", "OPI_BIN_FIXATION", "OPI_BIN_PRESENT", "OPI_MONO_BG_ADD", "OPI_MONO_SET_BG", "OPI_MONO_PRESENT", "OPI_SET_FOVY" };

        string currentCommand = commands.FirstOrDefault(s => message.Contains(s));

        if (string.IsNullOrEmpty(currentCommand)) 
        {
            Debug.LogError("OPI command not recognized. " + message);
            return;
        }

        switch (currentCommand)
        {
            case "OPI_GET_RES":
                break;
            case "OPI_IMAGE":
                break;
            case "OPI_PRESENT":
                break;
            case "OPI_SET_BGROUND":
                break;
            case "OPI_BIN_FIXATION":
                break;
            case "OPI_BIN_PRESENT":
                break;
            case "OPI_MONO_BG_ADD":
                break;
            case "OPI_MONO_SET_BG":
                break;
            case "OPI_MONO_PRESENT":
                break;
            case "OPI_SET_FOVY":
                break;
            default:
                Debug.LogError("OPI command not recognized. " + currentCommand);
                return;
        }
    }
}
