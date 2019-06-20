package jp.co.satoshi.uart_plugin;

import android.app.PendingIntent;
import android.content.Context;
import android.content.Intent;
import android.hardware.usb.UsbConstants;
import android.hardware.usb.UsbDevice;
import android.hardware.usb.UsbDeviceConnection;
import android.hardware.usb.UsbEndpoint;
import android.hardware.usb.UsbInterface;
import android.hardware.usb.UsbManager;

import android.app.Activity;
import android.serialport.SerialPort;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStream;
import java.io.LineNumberReader;
import java.io.OutputStream;
import java.util.Iterator;
import java.util.Vector;
import java.util.concurrent.Semaphore;

/**
 * Created by LIFE_MAC34 on 2017/08/08.
 */

public class NativeUart extends Activity{

    public static String GAME_OBJECT = "NativeUart";
    public static String CALLBACK_METHOD = "UartCallbackState";
    public static String RECEIVED_METHOD = "UartMessageReceived";
    public static String DEVICELIST_METHOD = "UartCallbackDeviceList";

    private static SerialPort serialPort;
    private static InputStream mFileInputStream;
    private static OutputStream mFileOutputStream;
    private static Semaphore semaphore = new Semaphore(1);
    private static boolean reading = false;

    static public void initialize() {

        // イニシャライズ
        UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Message:Uart initialize...");

        // USBデバイスの検索
        updateDviceList();

        UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Message:Uart initialized");
    }

    static public void open(String devPath, int baud){
        UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Message:Serial port opening...");
        try {
            serialPort = new SerialPort(devPath, baud);
            mFileInputStream = serialPort.getInputStream();
            mFileOutputStream = serialPort.getOutputStream();
            semaphore.acquire();
            reading = true;
            semaphore.release();
            read();
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Message:Serial port opened");
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "open success");
        } catch (Throwable e) {
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Error:Open serial port failed because " + e.getMessage());
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "open failed");
        }
    }

    static public void close() {
        try {
            serialPort.close();
            serialPort = null;
            reading = false;
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Message:Serial port closed");
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "close success");
        }catch (Throwable e) {
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Error:Close serial port failed because " + e.getMessage());
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "close failed");
        }

    }


    static public void updateDviceList() {
        try {
            Vector<Driver> drivers = new Vector<>();
            LineNumberReader r = new LineNumberReader(new FileReader("/proc/tty/drivers"));
            String l;
            while ((l = r.readLine()) != null) {
                String drivername = l.substring(0, 0x15).trim();
                String[] w = l.split(" +");
                if ((w.length >= 5) && (w[w.length - 1].equals("serial"))) {
                    Log.d(Driver.TAG, "Found new driver " + drivername + " on " + w[w.length - 4]);
                    drivers.add(new Driver(drivername, w[w.length - 4]));
                }
            }
            r.close();

            Vector<String> devices = new Vector<>();
            Iterator<Driver> itdriv;
            itdriv = drivers.iterator();
            while (itdriv.hasNext()) {
                Driver driver = itdriv.next();
                Iterator<File> itdev = driver.getDevices().iterator();
                while (itdev.hasNext()) {
                    String device = itdev.next().getAbsolutePath();
                    devices.add(device);
                }
            }

            StringBuilder b = new StringBuilder();
            for (int i = 0; i < devices.size(); i++) {
                b.append(devices.get(i));
                if (i < devices.size() - 1)
                    b.append("|");
            }
            UnityPlayer.UnitySendMessage(GAME_OBJECT, DEVICELIST_METHOD, b.toString());

        }catch (IOException e) {
            e.printStackTrace();
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Error:Update device list failed because " + e.getMessage());
        }
    }

    static public void read(){

        new Thread(new Runnable(){
            public void run(){
                try{
                    semaphore.acquire();
                    UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD,"Message:Start read message");
                    semaphore.release();
                    while (reading) {
                        final int size = 128;
                        final byte[] buffer = new byte[size];
                        final StringBuilder sb = new StringBuilder();

                        int length = mFileInputStream.read(buffer);

                        if (length > 0) {

                            for (int i = 0; i < length; i++) {
                                sb.append(buffer[i]);
                            }
                            UnityPlayer.UnitySendMessage(GAME_OBJECT, RECEIVED_METHOD, String.valueOf(sb));
                        }
                        Thread.sleep(1);
                    }
                }
                catch (InterruptedException | IOException e) {
                    e.printStackTrace();
                    UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Error:Read message failed because " + e.getMessage());
                }
            }
        }).start();
    }

    static public void send(String str) {
        try {
            mFileOutputStream.write(str.getBytes());
        } catch (IOException e) {
            e.printStackTrace();
            UnityPlayer.UnitySendMessage(GAME_OBJECT, CALLBACK_METHOD, "Error:Send message failed because " + e.getMessage());
        }
    }

}
