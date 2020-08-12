using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Support.Wearable.Activity;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Timers;

namespace InertialSensor.Watch
{
  [Activity(Label = "@string/app_name", MainLauncher = true)]
  public class MainActivity : WearableActivity, ISensorEventListener
  {
    private const int ENABLE_BT_REQUEST_CODE = 1;
    private const int DISCOVERABLE_DURATION = 3000;
    private const int DISCOVERABLE_BT_REQUEST_CODE = 2;

    private BluetoothAdapter bluetoothAdapter;
    private Handler messageHandler;
    private SendData messageChatService;

    private SensorManager sensor_manager;
    private Sensor sensor;
    private Vibrator vibrator;
    private Sensor gyro;
    private byte[] package;
    private byte[] header;

    private byte[] Magnetometer;
    private int counter;
    private int current;

    private Timer dispatcherTimer;
    private Timer beginBluetooth;
    private List<Byte[]> AccelBuffer;
    private List<Byte[]> GyrBuffer;
    private byte[] dataPack;

    private readonly object bufferLock = new object();

    private float[] sensorData;

    private Button button;
    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);
      SetContentView(Resource.Layout.activity_main); 

      bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
      messageHandler = new Handler();

      sensor_manager = (SensorManager)GetSystemService(Context.SensorService);
      sensor = sensor_manager.GetDefaultSensor(SensorType.Accelerometer);
      gyro = sensor_manager.GetDefaultSensor(SensorType.Gyroscope);
      vibrator = (Vibrator)GetSystemService(VibratorService);

      package = new byte[39];
      header = new byte[3];
      header[0] = Convert.ToByte('Z');
      header[1] = Convert.ToByte('A');
      header[2] = Convert.ToByte('P');
      Magnetometer = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
      AccelBuffer = new List<Byte[]>();
      GyrBuffer = new List<Byte[]>();
      counter = 0;
      current = DateTime.Now.Second;
      sensorData = new float[4];
      dataPack = new byte[390];
      if (sensor_manager.GetDefaultSensor(SensorType.Accelerometer) != null)
      {
        vibrator.Vibrate(500);
      }
 
      button = FindViewById<Button>(Resource.Id.button1);
      dispatcherTimer = new Timer(20);
      dispatcherTimer.Enabled = true;

      beginBluetooth = new Timer(1000);
      beginBluetooth.Enabled = true;
      beginBluetooth.Elapsed += beginBluetooth_Tick;
      SetAmbientEnabled();
      
    }

    public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
    {  
    }

    private void beginBluetooth_Tick(object sender, object e)
    {
      beginBluetooth.Enabled = false;
      StartBluetooth();
    }

      private void dispatcherTimer_Tick(object sender, object e)
    {
      List<Byte[]> tempAccel = new List<Byte[]>();
      List<Byte[]> tempGyr = new List<Byte[]>();
      if (messageChatService != null)
      {
        int length;
        var pack = new List<Byte>();
        lock (bufferLock)
        {
          if (AccelBuffer.Count < GyrBuffer.Count)
          {
            length = AccelBuffer.Count;
          }
          else
          {
            length = GyrBuffer.Count;
          }
          tempAccel.AddRange(AccelBuffer);
          tempGyr.AddRange(GyrBuffer);
          AccelBuffer.Clear();
          GyrBuffer.Clear();
        }
          pack.AddRange(header);
          int t = BitConverter.GetBytes(length).Length;
          pack.AddRange(BitConverter.GetBytes(length));
          for (int i = 0; i < length; i++)
          {
            pack.AddRange(tempAccel[i]);
            pack.AddRange(tempGyr[i]);
            pack.AddRange(Magnetometer);
          }

        SendMessage(pack.ToArray());
      }
    }

    public void OnSensorChanged(SensorEvent e)
    {
      try
      {
        e.Values.CopyTo(sensorData, 0);
        //Updates package of bytes based on which sensor updated and sends over new data
        // through bluetooth
        // If byte[] is ever less than length 4, sets extra values to 0
        switch (e.Sensor.Type)
        {
          case SensorType.Accelerometer:
            byte[] AccelData = new byte[12];
            byte[] accelX = BitConverter.GetBytes(sensorData[0]);
            byte[] accelY = BitConverter.GetBytes(sensorData[1]);
            byte[] accelZ = BitConverter.GetBytes(sensorData[2]);
            for (int i = 0; i < 12; i++)
            {
              if (i < 4)
              {
                if (accelX.Length < 4)
                {
                  byte[] temp = new byte[4];
                  for (int j = 0; j < 4; j++)
                  {
                    if (j < accelX.Length)
                    {
                      temp[i] = accelX[j];
                    }
                    else
                    {
                      temp[j] = 0;
                    }
                  }
                  accelX = temp;
                }
                AccelData[i] = accelX[i];
              }
              else if (i < 8)
              {
                if (accelY.Length < 4)
                {
                  byte[] temp = new byte[4];
                  for (int j = 0; j < 4; j++)
                  {
                    if (j < accelY.Length)
                    {
                      temp[j] = accelY[j];
                    }
                    else
                    {
                      temp[j] = 0;
                    }
                  }
                  accelY = temp;
                }


                AccelData[i] = accelY[i - 4];
              }
              else
              {
                if (accelZ.Length < 4)
                {
                  byte[] temp = new byte[4];
                  for (int j = 0; j < 4; j++)
                  {
                    if (j < accelZ.Length)
                    {
                      temp[j] = accelZ[j];
                    }
                    else
                    {
                      temp[j] = 0;
                    }
                  }
                  accelZ = temp;
                }
                AccelData[i] = accelZ[i - 8];
              }
            }
            lock (bufferLock)
            {
              AccelBuffer.Add(AccelData);
            }
            break;

          case SensorType.Gyroscope:
            e.Values.CopyTo(sensorData, 0);
            byte[] GyrData = new byte[12];
            byte[] gyrX = BitConverter.GetBytes(sensorData[0]);
            byte[] gyrY = BitConverter.GetBytes(sensorData[1]);
            byte[] gyrZ = BitConverter.GetBytes(sensorData[2]);
            for (int i = 0; i < 12; i++)
            {
              if (i < 4)
              {
                if (gyrX.Length < 4)
                {
                  byte[] temp = new byte[4];
                  for (int j = 0; j < 4; j++)
                  {
                    if (j < gyrX.Length)
                    {
                      temp[i] = gyrX[j];
                    }
                    else
                    {
                      temp[j] = 0;
                    }
                  }
                  gyrX = temp;
                }
                GyrData[i] = gyrX[i];
              }
              else if (i < 8)
              {
                if (gyrY.Length < 4)
                {
                  byte[] temp = new byte[4];
                  for (int j = 0; j < 4; j++)
                  {
                    if (j < gyrY.Length)
                    {
                      temp[j] = gyrY[j];
                    }
                    else
                    {
                      temp[j] = 0;
                    }
                  }
                  gyrY = temp;
                }


                GyrData[i] = gyrY[i - 4];
              }
              else
              {
                if (gyrZ.Length < 4)
                {
                  byte[] temp = new byte[4];
                  for (int j = 0; j < 4; j++)
                  {
                    if (j < gyrZ.Length)
                    {
                      temp[j] = gyrZ[j];
                    }
                    else
                    {
                      temp[j] = 0;
                    }
                  }
                  gyrZ = temp;
                }
                GyrData[i] = gyrZ[i - 8];
              }
            }
            lock (bufferLock) { GyrBuffer.Add(GyrData); }
            break;
          default:
            throw new Exception("Unhandled Sensor type");
        }
      }
      catch
      {
        throw;
      }
    }

    protected override void OnResume()
    {
      base.OnResume();
    }

    protected override void OnPause()
    {

        base.OnPause();

    }

    protected override void OnStart()
    {
      base.OnStart();
      
      if (messageChatService is null && beginBluetooth.Enabled == false)
      {
        //On start, restart the bluetooth connection
        SetupTransfer();
      }

      sensor_manager.RegisterListener(this, sensor, SensorDelay.Game);
      sensor_manager.RegisterListener(this, gyro, SensorDelay.Game);
      dispatcherTimer = new Timer(20);
      dispatcherTimer.Enabled = true;
    }

    protected override void OnStop()
    {
      base.OnStop();
      dispatcherTimer.Stop();

      sensor_manager.UnregisterListener(this);
      // SetupTransfer();
      //this.UnregisterReceiver(BroadcastReceiver);
    }
    protected override void OnActivityResult(int requestCode, Result result, Intent data)
    {
      base.OnActivityResult(requestCode, result, data);
      if (requestCode == ENABLE_BT_REQUEST_CODE)
      {
        if (result == Result.Ok)
        {
          Toast.MakeText(Application.Context, "Bluetooth enabled", ToastLength.Short).Show();
        }
        else
        {
          Toast.MakeText(Application.Context, "Bluetooth not enabled", ToastLength.Short).Show();
        }
      }
      else if (requestCode == DISCOVERABLE_BT_REQUEST_CODE)
      {
        if (result.Equals(DISCOVERABLE_DURATION))
        {
          Toast.MakeText(Application.Context, "Device now discoverable for " + DISCOVERABLE_DURATION + " seconds", ToastLength.Short).Show();
        }
        else
        {
          Toast.MakeText(Application.Context, "Failed to enable discoverability on device.", ToastLength.Short).Show();
        }
      }
    }


    //Makes device discoverable and enables bluetooth connection on device to PC
    protected void StartBluetooth()
    {
      if (bluetoothAdapter == null)
      {
        Toast.MakeText(Application.Context, "Device doesn't support bluetooth", ToastLength.Short).Show();
      }
      else
      {
        if (!bluetoothAdapter.IsEnabled)
        {
          Intent enableBluetoothIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
          StartActivityForResult(enableBluetoothIntent, ENABLE_BT_REQUEST_CODE);
        }
        else
        {
          DiscoverDevices();
          MakeDiscoverable();
          button.SetBackgroundColor(Color.BlueViolet);
          button.Text = "Ready to Connect";
          SetupTransfer();
          dispatcherTimer.Enabled = true;
          dispatcherTimer.Elapsed += dispatcherTimer_Tick;
        }


      }
    }
    protected void MakeDiscoverable()
    {
      Intent discoverableIntent = new Intent(BluetoothAdapter.ActionRequestDiscoverable);
      discoverableIntent.PutExtra(BluetoothAdapter.ExtraDiscoverableDuration, DISCOVERABLE_DURATION);
      StartActivityForResult(discoverableIntent, DISCOVERABLE_BT_REQUEST_CODE);
    }

    protected void DiscoverDevices()
    {
      bluetoothAdapter.StartDiscovery();
      //Scan for remote Bluetooth devices
      //if (bluetoothAdapter.StartDiscovery())
      //{
      //  Toast.MakeText(Application.Context, "Discovering other devices...", ToastLength.Short).Show();
        
      //}
      //else
      //{
      //  Toast.MakeText(Application.Context, "Discovery failed to start", ToastLength.Short).Show();
      //}
    }

    //Creates new instance of SendData and starts AcceptThread to accept new connections
    private void SetupTransfer()
    {
      messageChatService = new SendData(messageHandler);
      messageChatService.Start();
      button.SetBackgroundColor(Color.ForestGreen);
      button.Text = "Connected";
    }

    private void SendMessage(byte[] message)
    {    
      if (messageChatService.GetState() != StateEnum.Connected)
      {
        button.SetBackgroundColor(Color.BlueViolet);
        button.Text = "Ready to Connect";
      }
      else
      {
        button.SetBackgroundColor(Color.ForestGreen);
        button.Text = "Connected";
      }
 

      messageChatService.Write(message);
    }
  }
}

