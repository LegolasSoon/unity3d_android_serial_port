using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NativeUart : MonoBehaviour
{
    public const string OpenedCallback = "Serial port opened";

	static NativeUart instance;

    public delegate void UartStateChangedHandler(bool value);
	public delegate void UartDataReceivedEventHandler(string message);
    public event UartStateChangedHandler OnUartOpenChanged;
    public event UartStateChangedHandler OnUartCloseChanged;
	public event UartDataReceivedEventHandler OnUartState;
	public event UartDataReceivedEventHandler OnUartDeviceList;
	public event UartDataReceivedEventHandler OnUartMessageRead;

	#if UNITY_ANDROID
	AndroidJavaClass nu;
	AndroidJavaObject context;
	AndroidJavaClass unityPlayer;
    public bool IsOpening { private set; get; }
	#endif

	NativeUart()
    {
#if UNITY_ANDROID
        nu = new AndroidJavaClass("jp.co.satoshi.uart_plugin.NativeUart");
        unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        context = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
#endif
        Debug.Log("Create NativeUart instance.");
    }

    private void OnDestroy()
    {
        Close();
        instance = null;
    }

    public static NativeUart Instance
    {
		get
        {
			if (instance == null) {
				GameObject obj = new GameObject ("NativeUart");
				instance = obj.AddComponent<NativeUart>();
			}
			return instance;
		}
	}

	public void Init()
    {
#if UNITY_ANDROID
        context.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            nu.CallStatic("initialize");
        }));
#endif
	}


	public void Open(string device, int boud)
    {
#if UNITY_ANDROID
        context.Call("runOnUiThread", new AndroidJavaRunnable(() =>
        {
            nu.CallStatic("open", device, boud);
        }));
#endif
	}

    public void Close()
    {
        nu.CallStatic("close");
    }

	public void Send(string msg)
    {
        if (!IsOpening) return;
#if UNITY_ANDROID
        nu.CallStatic("send", msg);
#endif
	}

	public void UartCallbackState(string msg)
    {
        FindObjectOfType<LogManager>().AddMessage(LogManager.MessageType.Message, msg);
        switch (msg)
        {
            case "open success":
                IsOpening = true;
                OnUartOpenChanged(true);
                break;
            case "open failed":
                IsOpening = false;
                OnUartOpenChanged(false);
                break;
            case "close success":
                IsOpening = false;
                OnUartCloseChanged(true);
                break;
            case "close failed":
                IsOpening = false;
                OnUartCloseChanged(false);
                break;
            default:
                OnUartState(msg);
                break;
        }
    }

	public void UartCallbackDeviceList(string msg)
    {
		OnUartDeviceList(msg);
	}

    public void UartMessageReceived(string msg)
    {
        OnUartMessageRead(msg);
    }
}
