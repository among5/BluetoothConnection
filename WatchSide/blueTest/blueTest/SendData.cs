using Android.Bluetooth;
using Android.OS;
using Java.Util;
using System;
using System.IO;
using System.Text;

namespace blueTest
{
  public class SendData
  {
    private BluetoothAdapter BluetoothAdapter { get; }
    private Handler MessageHandler { get; }

    private AcceptThread mSecureAcceptThread;
    private ConnectedThread mConnectedThread;
    private readonly UUID uuid;
    private StateEnum state;

    public SendData(Handler handler)
    {
      BluetoothAdapter = BluetoothAdapter.DefaultAdapter;
      state = StateEnum.None;
      MessageHandler = handler;
      uuid = UUID.FromString("c88ae110-c0e0-11ea-b3de-0242ac130004");
    }

    public StateEnum GetState() => state;

    public void Start()
    {
      StopRunningConnectThread();

      // Start the thread to listen on a BluetoothServerSocket
      if (mSecureAcceptThread is null)
      {
        mSecureAcceptThread = new AcceptThread(true, this);
        mSecureAcceptThread.Run();
      }

      state = StateEnum.Connected;
    }

    public void Connected(BluetoothSocket socket, BluetoothDevice device)
    {
      StopRunningConnectThread();

      // Cancel the accept thread because we only want to connect to one device
      if (mSecureAcceptThread != null)
      {
        mSecureAcceptThread.Cancel();
        mSecureAcceptThread = null;
      }

      // Start the thread to manage the connection and perform transmissions
      mConnectedThread = new ConnectedThread(socket, this);

      // Send the name of the connected device back to the UI Activity
      Message msg = MessageHandler.ObtainMessage(Constants.MESSAGE_DEVICE_NAME);
      Bundle bundle = new Bundle();
      bundle.PutString(Constants.DEVICE_NAME, device.Name);
      msg.Data = bundle;
      MessageHandler.SendMessage(msg);
    }

    public void Stop()
    {
      StopRunningConnectThread();

      if (mSecureAcceptThread != null)
      {
        mSecureAcceptThread.Cancel();
        mSecureAcceptThread = null;
      }

      state = StateEnum.None;
    }

    public void Write(byte[] message)
    {
      if (state != StateEnum.Connected)
      {
        return;
      }

      var r = mConnectedThread;
      r.Write(message);
    }

    private void StopRunningConnectThread()
    {
      // Cancel any thread currently running a connection
      if (mConnectedThread != null)
      {
        mConnectedThread.Cancel();
        mConnectedThread = null;
      }
    }

    private void ConnectionLost()
    {
      Message msg = MessageHandler.ObtainMessage(Constants.MESSAGE_TOAST);
      Bundle bundle = new Bundle();
      bundle.PutString(Constants.TOAST, "Device connection lost");
      msg.Data = bundle;
      MessageHandler.SendMessage(msg);
      state = StateEnum.None;
      this.Start();
    }

    private void Disconnect()
    {
      StopRunningConnectThread();

      if (mSecureAcceptThread != null)
      {
        mSecureAcceptThread.Cancel();
        mSecureAcceptThread = null;
      }

      this.Start();
    }

    internal class AcceptThread
    {
      private readonly BluetoothServerSocket mmServerSocket;
      private readonly SendData sendData;

      public AcceptThread(bool secure, SendData sd)
      {
        try
        {
          BluetoothServerSocket temp;
          sendData = sd;

          if (secure)
          {

            temp = sendData.BluetoothAdapter.ListenUsingRfcommWithServiceRecord("Bluetooth Chat Service", sendData.uuid);
          }
          else
          {
            temp = sendData.BluetoothAdapter.ListenUsingInsecureRfcommWithServiceRecord("Insecure Bluetooth Chat Service", sendData.uuid);
          }

          mmServerSocket = temp;
          sendData.state = StateEnum.Listen;
        }
        catch (IOException e)
        {
          Console.WriteLine(e.ToString() + "!!!");
          throw;
        }
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
            throw;
          }

          if (socket != null)
          {
            if (sendData.GetState() == StateEnum.Connecting)
            {
              sendData.Connected(socket, socket.RemoteDevice);

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
      public ConnectedThread(BluetoothSocket socket, SendData sendData)
      {
        mmSocket = socket;
        sd = sendData;

        try
        {
          mmInStream = socket.InputStream;
          mmOutStream = socket.OutputStream;
          sd.state = StateEnum.Connected;
        }
        catch (IOException e)
        {
          Console.WriteLine(e.ToString() + "!!!");
          throw;
        }
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

            sd.MessageHandler.ObtainMessage(Constants.MESSAGE_READ, bytes, -1, buffer).SendToTarget();
          }
          catch (IOException e)
          {
            Console.Write(e.ToString() + "!!!");
            sd.ConnectionLost();
            throw;
          }
        }
      }

      public void Write(byte[] buffer)
      {
        try
        {
          mmOutStream.Write(buffer);
          Console.WriteLine(Encoding.UTF8.GetString(buffer));
          sd.MessageHandler.ObtainMessage(Constants.MESSAGE_WRITE, -1, -1, buffer).SendToTarget();
        }
        catch (Exception e)
        {
          Console.WriteLine(e.ToString() + "!!!");
          sd.Disconnect();
          throw;
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
          throw;
        }
      }
    }
  }
}


