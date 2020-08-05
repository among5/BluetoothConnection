using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Support.Wearable.Activity;
using Android.Widget;
using System;
using System.Collections.Generic;

namespace blueTest
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

    private int counter;
    private int current;
    private int sensorChangecounter;

    private byte[] dataPack; 


    private float[] sensorData;
    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);
      SetContentView(Resource.Layout.activity_main);

      SetAmbientEnabled();

      bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
      messageHandler = new Handler();

      sensor_manager = (SensorManager)GetSystemService(Context.SensorService);
      sensor = sensor_manager.GetDefaultSensor(SensorType.Accelerometer);
      gyro = sensor_manager.GetDefaultSensor(SensorType.Gyroscope);
      vibrator = (Vibrator)GetSystemService(VibratorService);

      package = new byte[39];

      for(int i=0; i<package.Length; i++)
      {
        package[i] = 0;
      }
      package[0] = Convert.ToByte('Z');
      package[1] = Convert.ToByte('A');
      package[2] = Convert.ToByte('P');

      counter = -1;
      sensorChangecounter = 0;
      current = DateTime.Now.Second;
      sensorData = new float[4];

      dataPack = new byte[390];
      if (sensor_manager.GetDefaultSensor(SensorType.Accelerometer) != null)
      {
        vibrator.Vibrate(500);
      }
   
      StartBluetooth();
    }

    public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
    {  
    }

    public void OnSensorChanged(SensorEvent e)
    {
      counter++;
      e.Values.CopyTo(sensorData, 0);
      //Updates package of bytes based on which sensor updated and sends over new data
      // through bluetooth
      // If byte[] is ever less than length 4, sets extra values to 0
      switch (e.Sensor.Type)
      {
        case SensorType.Accelerometer:
         
          byte[] accelX = BitConverter.GetBytes(sensorData[0]);
          byte[] accelY = BitConverter.GetBytes(sensorData[1]);
          byte[] accelZ = BitConverter.GetBytes(sensorData[2]);
          for (int i = 3; i < 15; i++)
          {
            if (i < 7)
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
              package[i] = accelX[i - 3];
            }
            else if (i < 11)
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


              package[i] = accelY[i - 7];
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
              package[i] = accelZ[i - 11];
            }
          }
          package.CopyTo(dataPack, counter * 39);
          if(counter == 9)
          {
            SendMessage(dataPack);
            counter = -1;
          }
       
          break;

        case SensorType.Gyroscope:
          e.Values.CopyTo(sensorData, 0);
          byte[] gyrX = BitConverter.GetBytes(sensorData[0]);
          byte[] gyrY = BitConverter.GetBytes(sensorData[1]);
          byte[] gyrZ = BitConverter.GetBytes(sensorData[2]);
          for (int i = 15; i < 27; i++)
          {
            if (i < 19)
            {
              if (gyrX.Length < 4)
              {
                byte[] temp = new byte[4];
                for (int j = 0; j < 4; j++)
                {
                  if (j < gyrX.Length)
                  {
                    temp[j] = gyrX[j];
                  }
                  else
                  {
                    temp[j] = 0;
                  }
                }
                gyrX = temp;
              }
              package[i] = gyrX[i - 15];
            }
            else if (i < 23)
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
              package[i] = gyrY[i - 19];
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
              package[i] = gyrZ[i - 23];
            }
          }
          package.CopyTo(dataPack, counter * 39);
          if (counter == 9)
          {
            SendMessage(dataPack);
            counter = -1;
          }
          break;
        default:
          throw new Exception("Unhandled Sensor type");
      }
    }

    protected override void OnResume()
    {
      base.OnResume();
      sensor_manager.RegisterListener(this, sensor, SensorDelay.Game);
      sensor_manager.RegisterListener(this, gyro, SensorDelay.Game);
    }

    protected override void OnPause()
    {
      base.OnPause();
      sensor_manager.UnregisterListener(this);
    }

    protected override void OnStart()
    {
      base.OnStart();
      if (messageChatService is null)
      {
        //On start, restart the bluetooth connection
        SetupTransfer();
      }
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
          Toast.MakeText(Application.Context, "Device already enabled " + "\n" + "Scanning for remote Bluetooth devices..", ToastLength.Short).Show();
        }

        DiscoverDevices();
       // MakeDiscoverable();
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
      //Scan for remote Bluetooth devices
      if (bluetoothAdapter.StartDiscovery())
      {
        Toast.MakeText(Application.Context, "Discovering other devices...", ToastLength.Short).Show();
        
      }
      else
      {
        Toast.MakeText(Application.Context, "Discovery failed to start", ToastLength.Short).Show();
      }
    }

    protected override void OnStop()
    {
      base.OnStop();
     // SetupTransfer();
      //this.UnregisterReceiver(BroadcastReceiver);
    }

    //Creates new instance of SendData and starts AcceptThread to accept new connections
    private void SetupTransfer()
    {
      messageChatService = new SendData(messageHandler);
      messageChatService.Start();
    }

    private void SendMessage(byte[] message)
    {
      counter++;
      int curSec = DateTime.Now.Second;
     // Console.WriteLine(message.ToString() + " " + DateTime.Now.ToString());
      
      if(curSec != current)
      {
      //  Console.WriteLine(counter + DateTime.Now.ToString());
       // Console.WriteLine(sensorChangecounter + "*");
        sensorChangecounter = 0;
        current = curSec;
        counter = 0;
      }
      
      if (messageChatService.GetState() != StateEnum.Connected)
      {
        Toast.MakeText(Application.Context, "NOT CONNECTED", ToastLength.Short).Show();
        return;
      }

      if(message.Length != 390)
      {
        throw new ArgumentException("Length != 390");
      }   

      messageChatService.Write(message);
    }
  }
}

