using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Wearable;
using Android.Widget;
using Java.Util;

namespace blueTest
{

  public class SendData
  {

    private BluetoothAdapter mAdapter { get; }
    private Handler mHandler { get; }
    private AcceptThread mSecureAcceptThread;
    private AcceptThread mInsecureAcceptThread;
    private ConnectThread mConnectThread;
    private ConnectedThread mConnectedThread;

    private UUID uuid;

    private int mState;
    private int mNewState;


    public const int STATE_NONE = 0;       // we're doing nothing
    public const int STATE_LISTEN = 1;     // now listening for incoming connections
    public const int STATE_CONNECTING = 2; // now initiating an outgoing connection
    public const int STATE_CONNECTED = 3;  // now connected to a remote device


    public SendData(Context context, Handler handler)
    {
      mAdapter = BluetoothAdapter.DefaultAdapter;
      mState = STATE_NONE;
      mNewState = mState;
      mHandler = handler;
      uuid = UUID.FromString("gjhk");
     
    }



    public int getState()
    {
      return mState;
    }

    public void start()
    {
      if (mConnectThread != null)
      {
        mConnectThread.cancel();
        mConnectThread = null;
      }

      // Cancel any thread currently running a connection
      if (mConnectedThread != null)
      {
        mConnectedThread.cancel();
        mConnectedThread = null;
      }

      // Start the thread to listen on a BluetoothServerSocket
      if (mSecureAcceptThread == null)
      {
        mSecureAcceptThread = new AcceptThread(true, this);
        mSecureAcceptThread.run();
      }
      if (mInsecureAcceptThread == null)
      {
        mInsecureAcceptThread = new AcceptThread(false, this);


        mInsecureAcceptThread.run();
      }




    }

    
    public void connect(BluetoothDevice device, Boolean secure)
    {
      if (mState == STATE_CONNECTING)
      {
        if (mConnectThread != null)
        {
          mConnectThread.cancel();
          mConnectThread = null;
        }
      }

      // Cancel any thread currently running a connection
      if (mConnectedThread != null)
      {
        mConnectedThread.cancel();
        mConnectedThread = null;
      }

      // Start the thread to connect with the given device
      mConnectThread = new ConnectThread(device, true, this);
      mConnectThread.run();
    }

    public void connected(BluetoothSocket socket, BluetoothDevice
            device, String socketType)
    {

      // Cancel the thread that completed the connection
      if (mConnectThread != null)
      {
        mConnectThread.cancel();
        mConnectThread = null;
      }

      // Cancel any thread currently running a connection
      if (mConnectedThread != null)
      {
        mConnectedThread.cancel();
        mConnectedThread = null;
      }

      // Cancel the accept thread because we only want to connect to one device
      if (mSecureAcceptThread != null)
      {
        mSecureAcceptThread.cancel();
        mSecureAcceptThread = null;
      }
      if (mInsecureAcceptThread != null)
      {
        mInsecureAcceptThread.cancel();
        mInsecureAcceptThread = null;
      }

      // Start the thread to manage the connection and perform transmissions
      mConnectedThread = new ConnectedThread(socket, socketType, this);
      mConnectedThread.run();

      // Send the name of the connected device back to the UI Activity
      Message msg = mHandler.ObtainMessage(Constants.MESSAGE_DEVICE_NAME);
      Bundle bundle = new Bundle();
      bundle.PutString(Constants.DEVICE_NAME, device.Name);
      msg.Data = bundle;
      mHandler.SendMessage(msg);

    }




    public void stop()
    {

      if (mConnectThread != null)
      {
        mConnectThread.cancel();
        mConnectThread = null;
      }

      if (mConnectedThread != null)
      {
        mConnectedThread.cancel();
        mConnectedThread = null;
      }

      if (mSecureAcceptThread != null)
      {
        mSecureAcceptThread.cancel();
        mSecureAcceptThread = null;
      }

      if (mInsecureAcceptThread != null)
      {
        mInsecureAcceptThread.cancel();
        mInsecureAcceptThread = null;
      }
      mState = STATE_NONE;

    }

    public void write(byte[] message)
    {
      ConnectedThread r;

 
        if (mState != STATE_CONNECTED) { return; }
        r = mConnectedThread;
        r.write(message);
    }

    private void connectionFailed()
    {
      Message msg = mHandler.ObtainMessage(Constants.MESSAGE_TOAST);
      Bundle bundle = new Bundle();
      bundle.PutString(Constants.TOAST, "Unable to connect");
      msg.Data = bundle;
      mHandler.SendMessage(msg);

      mState = STATE_NONE;

      this.start();
    }
    private void connectionLost()
    {
      Message msg = mHandler.ObtainMessage(Constants.MESSAGE_TOAST);
      Bundle bundle = new Bundle();
      bundle.PutString(Constants.TOAST, "Device connection lost");
      msg.Data = bundle;
      mHandler.SendMessage(msg);
      mState = STATE_NONE;

      this.start();
    }



    internal class AcceptThread
    {
      private readonly BluetoothServerSocket mmServerSocket;
      private String mSocketType;
      private SendData sendData;

      public AcceptThread(Boolean secure, SendData sd)
      {
        BluetoothServerSocket temp = null;
        mSocketType = secure ? "Secure" : "Insecure";
        sendData = sd;
        try
        {
          if (secure)
          {

            temp = sendData.mAdapter.ListenUsingRfcommWithServiceRecord("Bluetooth Chat Service", sendData.uuid);
          }
          else
          {
            temp = sendData.mAdapter.ListenUsingInsecureRfcommWithServiceRecord("Insecure Bluetooth Chat Service", sendData.uuid);
          }
        }
        catch (IOException e)
        {
          Console.WriteLine(e.StackTrace);
        }
        mmServerSocket = temp;
        sendData.mState = STATE_LISTEN;

      }

      public void run()
      {
        BluetoothSocket socket = null;
        while (sendData.mState != STATE_CONNECTING)
        {
          try
          {
            socket = mmServerSocket.Accept();
          }
          catch (IOException e)
          {
            Console.WriteLine(e.StackTrace);
            break;
          }

          if (socket != null)
          {
            switch (sendData.mState)
            {
              case STATE_LISTEN:
              case STATE_CONNECTING:
                sendData.connected(socket, socket.RemoteDevice, mSocketType);
                break;
              case STATE_NONE:
              case STATE_CONNECTED:
                try
                {
                  socket.Close();
                }
                catch (IOException e)
                {
                  Console.Write(e.StackTrace);
                }
                break;
            }
          }
        }
      }


      public void cancel()
      {
        try
        {
          mmServerSocket.Close();
        }
        catch (IOException e)
        {
          Console.WriteLine(e.StackTrace);
        }
      }
    }




    internal class ConnectThread
    {
      private readonly BluetoothSocket mmSocket;
      private readonly BluetoothDevice mmDevice;
      private String mSocketType;
      private SendData sd;

      public ConnectThread(BluetoothDevice device, Boolean secure, SendData sd)
      {
        mmDevice = device;
        BluetoothSocket tmp = null;
        mSocketType = secure ? "Secure" : "Insecure";
        this.sd = sd;
        // Get a BluetoothSocket for a connection with the
        // given BluetoothDevice
        try
        {
          if (secure)
          {
            tmp = device.CreateRfcommSocketToServiceRecord(
                    sd.uuid);
          }
          else
          {
            tmp = device.CreateInsecureRfcommSocketToServiceRecord(
                    sd.uuid);
          }
        }
        catch (IOException e)
        {
          Console.WriteLine(e.StackTrace);
        }
        mmSocket = tmp;
        sd.mState = STATE_CONNECTING;
      }


      public void run()
      {
        sd.mAdapter.CancelDiscovery();
        try
        {
          mmSocket.Connect();
        }
        catch (IOException e)
        {
          try
          {
            mmSocket.Close();
          }
          catch (IOException except) {
            Console.WriteLine(except.StackTrace);
          }
          sd.connectionFailed();
          return;
        }
        sd.connected(mmSocket, mmDevice, mSocketType);
      }
      public void cancel()
      {
        try
        {
          this.mmSocket.Close();
        }
        catch (IOException e)
        {
          Console.WriteLine(e.StackTrace);
        }
      }

    }



    internal class ConnectedThread
    {
      private readonly BluetoothSocket mmSocket;
      private readonly Stream mmInStream;
      private readonly Stream mmOutStream;
      private readonly SendData sd;
      public ConnectedThread(BluetoothSocket socket, String socketType, SendData sendData)
      {
        mmSocket = socket;
        Stream tempIn = null;
        Stream tempOut = null;


        try
        {
          tempIn = socket.InputStream;
          tempOut = socket.OutputStream;

        }catch (IOException e)
        {
          Console.WriteLine(e.StackTrace);
        }

        mmInStream = tempIn;
        mmOutStream = tempOut;
        sd.mState = STATE_CONNECTED;
      }


      public void run()
      {
        byte[] buffer = new byte[1024];
        int bytes;

        while(sd.mState == STATE_CONNECTED)
        {
          try
          {
            bytes = mmInStream.Read(buffer);

            sd.mHandler.ObtainMessage(Constants.MESSAGE_READ, bytes, -1, buffer).SendToTarget();
          }catch (IOException e)
          {
            Console.Write(e.StackTrace);
            sd.connectionLost();
            break;
          }
        }
      }


      public void write(byte[] buffer)
      {
        try
        {
           mmOutStream.Write(buffer);

          sd.mHandler.ObtainMessage(Constants.MESSAGE_WRITE, -1, -1, buffer).SendToTarget();
        }catch (IOException e)
        {
          Console.WriteLine(e.StackTrace);
        }
      }

      public void cancel()
      {
        try
        {
          mmSocket.Close();
        }catch (IOException e)
        {
          Console.WriteLine(e.StackTrace);
        }
      }

    }
   






  }








}


