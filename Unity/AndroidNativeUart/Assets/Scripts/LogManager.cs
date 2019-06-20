using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogManager : MonoBehaviour
{
    public enum MessageType{ Message, Warning, Error, Send, Recived }

    [SerializeField] private Text msgLog;

    public void AddMessage(MessageType type, string msg)
    {
        string format = "";
        switch (type)
        {
            case MessageType.Message:
                format = "<color=blue>{0} 输出:{1} </color>";
                break;
            case MessageType.Warning:
                format = "<color=orange>{0} 警告:{1} </color>";
                break;
            case MessageType.Error:
                format = "<color=red>{0} 错误:{1} </color>";
                break;
            case MessageType.Send:
                format = "<color=green>{0} 发送:{1} </color>";
                break;
            case MessageType.Recived:
                format = "<color=teal>{0} 接收:{1} </color>";
                break;
        }
        var msgToWrite = string.Format(format, DateTime.Now, msg) + "\n";
        msgLog.text += msgToWrite;
    }

    public void ClearMssage()
    {
        msgLog.text = "";
    }

    public static void ExtractMessage(string orginMsg, out MessageType type, out string extractMsg)
    {
        var colonIndex = orginMsg.IndexOf(":");
        var prefix = orginMsg.Substring(0, colonIndex);
        type = (MessageType)Enum.Parse(typeof(MessageType), prefix);
        extractMsg = orginMsg.Substring(colonIndex, orginMsg.Length);
    }
}
