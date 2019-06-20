using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class UartTest : MonoBehaviour
{
    [SerializeField] private Dropdown deviceList;
    [SerializeField] private InputField baudrateInput;
    [SerializeField] private Button switchPort;
    [SerializeField] private InputField command;
    [SerializeField] private Button send;
    [SerializeField] private Button clear;
    [SerializeField] private Button forbidden;
    [SerializeField] private GameObject tip;

    [SerializeField] private LogManager logManager;

	NativeUart nu;

    string devicePath = "";
    int baudrate = 9600;
    bool isForbiddenReceive = false;

    void Awake()
    {
        try
        {
            nu = NativeUart.Instance;

            deviceList.ClearOptions();
            deviceList.onValueChanged.AddListener(index => {
                devicePath = deviceList.options[index].text;
            });

            baudrateInput.onValueChanged.AddListener(value=> {
                baudrate = int.Parse(value);
            });

            switchPort.onClick.AddListener(SwitchPort);

            send.onClick.AddListener(SendCommand);

            clear.onClick.AddListener(()=> {
                logManager.ClearMssage();
            });

            forbidden.onClick.AddListener(()=> {
                isForbiddenReceive = !isForbiddenReceive;
                forbidden.GetComponentInChildren<Text>().text = isForbiddenReceive ? "打开禁止自动更新日志" : "关闭禁止自动更新日志";
            });

            send.interactable = false;

            tip.SetActive(false);

            nu.OnUartState += SerialState;
            nu.OnUartDeviceList += SerialDeviceList;
            nu.OnUartMessageRead += SerialMessageReceived;

            nu.Init();
        }
        catch (Exception e)
        {
            logManager.AddMessage(LogManager.MessageType.Error, "Initialize serial port error: " + e.Message);
        }
	}

    private void SwitchPort()
    {
        switchPort.interactable = false;
        try
        {
            if (!nu.IsOpening)
            {
                nu.Open(devicePath, baudrate);
                nu.OnUartOpenChanged += OnUartOpenChanged;
            }
            else
            {
                nu.Close();
                nu.OnUartCloseChanged += OnUartCloseChanged;
            }
            
        }
        catch(Exception e)
        {
            logManager.AddMessage(LogManager.MessageType.Error, "Switch serial port error: " + e.Message);
        }
    }

    private void OnUartOpenChanged(bool value)
    {
        logManager.AddMessage(
            value ? LogManager.MessageType.Message : LogManager.MessageType.Error, 
            value ? "串口打开成功" : "串口打开失败");
        switchPort.interactable = true;
        send.interactable = value;
        switchPort.GetComponentInChildren<Text>().text = value ? "关闭串口" : "打开串口";
        tip.GetComponentInChildren<Text>().text = value ? "串口打开成功" : "串口打开失败";
        tip.SetActive(true);
        CancelInvoke();
        Invoke("DelayCloseTip", 3.0f);
        nu.OnUartOpenChanged -= OnUartOpenChanged;
    }

    private void OnUartCloseChanged(bool value)
    {
        logManager.AddMessage(
            value ? LogManager.MessageType.Message : LogManager.MessageType.Error, 
            value ? "串口关闭成功" : "串口关闭失败");
        switchPort.interactable = true;
        send.interactable = false;
        switchPort.GetComponentInChildren<Text>().text = "打开串口";
        tip.GetComponentInChildren<Text>().text = value ? "串口关闭成功" : "串口关闭失败";
        tip.SetActive(true);
        CancelInvoke();
        Invoke("DelayCloseTip", 3.0f);
        nu.OnUartOpenChanged -= OnUartOpenChanged;
    }

    void DelayCloseTip()
    {
        tip.SetActive(false);
    }

	public void SendCommand()
    {
        try
        {
            nu.Send(command.text);
            logManager.AddMessage(LogManager.MessageType.Send, command.text);
        }
        catch (Exception e)
        {
            logManager.AddMessage(LogManager.MessageType.Error, "Send message error: " + e.Message);
        }
	}

    private void OnDestroy()
    {
        try
        {
            nu.Close();
        }
        catch (Exception e)
        {
            logManager.AddMessage(LogManager.MessageType.Error, "Close serial port error: " + e.Message);
        }
    }

    public void SerialState(string msg)
    {
        LogManager.MessageType type;
        string extractMsg;
        LogManager.ExtractMessage(msg, out type, out extractMsg);
        logManager.AddMessage(type, extractMsg);
	}
	public void SerialDeviceList(string msg){
        string[] devices = msg.Split(new char[] { '|', ' ', '\r', '\n', '\t', '\0' });
        if (devices.Length == 0)
            devices = new string[] { "No serial Port"};

        deviceList.AddOptions(new List<string>(devices));
        devicePath = deviceList.options[0].text;
	}
	public void SerialMessageReceived(string msg)
    {
        if (isForbiddenReceive)
            return;

        logManager.AddMessage(LogManager.MessageType.Recived, msg);
	}

}
