using Android.Bluetooth;
using Android.OS;
using Java.Util;
using System;
using System.IO;

namespace blueTest
{
  public class SendData
  {
    private BluetoothAdapter bluetoothAdapter { get; }
    private Handler mHandler { get; }

    private AcceptThread mSecureAcceptThread;
    private AcceptThread mInsecureAcceptThread;
    private ConnectedThread mConnectedThread;

    private UUID uuid;
    private StateEnum state;

    public SendData(Handler handler)
    {
      bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
      state = StateEnum.None;
      mHandler = handler;
      uuid = UUID.FromString("c88ae110-c0e0-11ea-b3de-0242ac130004");
    }

    public StateEnum GetState()
    {
      return state;
    }

    public void Start()
    {
      // Cancel any thread currently running a connection
      if (mConnectedThread != null)
      {
        mConnectedThread.Cancel();
        mConnectedThread = null;
      }

      // Start the thread to listen on a BluetoothServerSocket
      if (mSecureAcceptThread is null)
      {
        mSecureAcceptThread = new AcceptThread(true, this);
        mSecureAcceptThread.Run();
      }

      state = StateEnum.Connected;
    }

    public void Connected(BluetoothSocket socket, BluetoothDevice
            device, string socketType)
    {
      // Cancel any thread currently running a connection
      if (mConnectedThread != null)
      {
        mConnectedThread.Cancel();
        mConnectedThread = null;
      }

      // Cancel the accept thread because we only want to connect to one device
      if (mSecureAcceptThread != null)
      {
        mSecureAcceptThread.Cancel();
        mSecureAcceptThread = null;
      }
      if (mInsecureAcceptThread != null)
      {
        mInsecureAcceptThread.Cancel();
        mInsecureAcceptThread = null;
      }

      // Start the thread to manage the connection and perform transmissions
      mConnectedThread = new ConnectedThread(socket, socketType, this);
      //mConnectedThread.run();

      // Send the name of the connected device back to the UI Activity
      Message msg = mHandler.ObtainMessage(Constants.MESSAGE_DEVICE_NAME);
      Bundle bundle = new Bundle();
      bundle.PutString(Constants.DEVICE_NAME, device.Name);
      msg.Data = bundle;
      mHandler.SendMessage(msg);
    }

    public void Stop()
    {
      if (mConnectedThread != null)
      {
        mConnectedThread.Cancel();
        mConnectedThread = null;
      }

      if (mSecureAcceptThread != null)
      {
        mSecureAcceptThread.Cancel();
        mSecureAcceptThread = null;
      }

      if (mInsecureAcceptThread != null)
      {
        mInsecureAcceptThread.Cancel();
        mInsecureAcceptThread = null;
      }

      state = StateEnum.None;
    }

    public void Write(byte[] message)
    {
      ConnectedThread r;
      if (state != StateEnum.Connected)
      {
        return;
      }

      r = mConnectedThread;
      r.Write(message);
    }

    private void ConnectionLost()
    {
      Message msg = mHandler.ObtainMessage(Constants.MESSAGE_TOAST);
      Bundle bundle = new Bundle();
      bundle.PutString(Constants.TOAST, "Device connection lost");
      msg.Data = bundle;
      mHandler.SendMessage(msg);
      state = StateEnum.None;
      this.Start();
    }

    internal class AcceptThread
    {
      private readonly BluetoothServerSocket mmServerSocket;
      private string mSocketType;
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

            temp = sendData.bluetoothAdapter.ListenUsingRfcommWithServiceRecord("Bluetooth Chat Service", sendData.uuid);
          }
          else
          {
            temp = sendData.bluetoothAdapter.ListenUsingInsecureRfcommWithServiceRecord("Insecure Bluetooth Chat Service", sendData.uuid);
          }
        }
        catch (IOException e)
        {
          Console.WriteLine(e.ToString() + "!!!");
        }

        mmServerSocket = temp;
        sendData.state = StateEnum.Listen;
      }

      public void Run()
      {
        BluetoothSocket socket;
        while (sendData.state != StateEnum.Connected)
        {
          try
          {
            socket = mmServerSocket.Accept();
            sendData.state = StateEnum.Connecting;
          }
          catch (IOException e)
          {
            Console.WriteLine(e.ToString() + "!!!");
            break;
          }

          if (socket != null)
          {
            if (sendData.GetState() == StateEnum.Connecting)
            {
              sendData.Connected(socket, socket.RemoteDevice, mSocketType);

            }
            else if (sendData.GetState() == StateEnum.Connected)
            {
              mmServerSocket.Close();
            }
          }
        }
      }


      public void Cancel()
      {
        try
        {
          mmServerSocket.Close();
        }
        catch (IOException e)
        {
          Console.WriteLine(e.ToString() + "!!!");
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
        sd = sendData;

        try
        {
          tempIn = socket.InputStream;
          tempOut = socket.OutputStream;
        }
        catch (IOException e)
        {
          Console.WriteLine(e.ToString() + "!!!");
        }

        mmInStream = tempIn;
        mmOutStream = tempOut;
        sd.state = StateEnum.Connected;
      }

      public void Run()
      {
        byte[] buffer = new byte[1024];
        int bytes;
        while (sd.state == StateEnum.Connected)
        {
          try
          {
            bytes = mmInStream.Read(buffer);

            sd.mHandler.ObtainMessage(Constants.MESSAGE_READ, bytes, -1, buffer).SendToTarget();
          }
          catch (IOException e)
          {
            Console.Write(e.ToString() + "!!!");
            sd.ConnectionLost();
            break;
          }
        }
      }

      public void Write(byte[] buffer)
      {
        try
        {
          mmOutStream.Write(buffer);
          Console.WriteLine(buffer.ToString());
          sd.mHandler.ObtainMessage(Constants.MESSAGE_WRITE, -1, -1, buffer).SendToTarget();
        }
        catch (IOException e)
        {
          Console.WriteLine(e.ToString() + "!!!");
        }
      }

      public void Cancel()
      {
        try
        {
          mmSocket.Close();
        }
        catch (IOException e)
        {
          Console.WriteLine(e.ToString() + "!!!");
        }
      }
    }
  }
}


