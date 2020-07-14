using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Hardware;
using Android.Support.Wearable.Activity;
using Android.Widget;
using Android.Runtime;
using System.Text;
using System;

namespace blueTest
{
  [Activity(Label = "@string/app_name", MainLauncher = true)]
  public class MainActivity : WearableActivity, ISensorEventListener
  {
    private const int ENABLE_BT_REQUEST_CODE = 1;
    private const int DISCOVERABLE_DURATION = 3000;
    private const int DISCOVERABLE_BT_REQUEST_CODE = 2;

    private TextView textView;
    private TextView textView2;
    private ListView listView;

    private BluetoothAdapter bluetoothAdapter;
    private ToggleButton toggleButton;
    private ArrayAdapter adapter;
    private SampleReceiver BroadcastReceiver;
    private Handler mHandler;
    private SendData mChatService = null;

    private SensorManager sensor_manager;
    private Sensor sensor;
    private TextView accelerometerData;
    private Vibrator vibrator;
    private Sensor gyro;

    private byte[] package;
    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);
      SetContentView(Resource.Layout.activity_main);

      textView = FindViewById<TextView>(Resource.Id.textView);
      SetAmbientEnabled();

      toggleButton = FindViewById<ToggleButton>(Resource.Id.toggleButton);

      listView = FindViewById<ListView>(Resource.Id.foundDevices);
      bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
      BroadcastReceiver = new SampleReceiver(adapter);
      mHandler = new Handler();
      textView2 = FindViewById<TextView>(Resource.Id.moose);

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

      if (sensor_manager.GetDefaultSensor(SensorType.Accelerometer) != null)
      {
        textView.Text = "ACCELEROMETER DETECTED";
        vibrator.Vibrate(500);
      }


      if (!bluetoothAdapter.IsEnabled)
      {
        Intent enableBlueToothIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
        StartActivityForResult(enableBlueToothIntent, ENABLE_BT_REQUEST_CODE);
      }

      toggleButton.Click += delegate
      {
        if (bluetoothAdapter is null)
        {
          Toast.MakeText(Application.Context, "Device doesn't support bluetooth", ToastLength.Short).Show();
          toggleButton.Checked = false;
        }
        else
        {
          if (toggleButton.Checked)
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
            MakeDiscoverable();
          }
          else
          {
            bluetoothAdapter.Disable();
            adapter.Clear();
            Toast.MakeText(Application.Context, "Device now disabled", ToastLength.Short).Show();
          }
        }
      };
    }



    public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
    {

    }


    protected override void OnResume()
    {
      base.OnResume();
      sensor_manager.RegisterListener(this, sensor, SensorDelay.Normal);
      sensor_manager.RegisterListener(this, gyro, SensorDelay.Normal);
    }

    protected override void OnPause()
    {
      base.OnPause();
      sensor_manager.UnregisterListener(this);
    }

    public void OnSensorChanged(SensorEvent e)
    {
     
      switch (e.Sensor.Type)
      {
        case SensorType.Accelerometer:
          byte[] accelX = Encoding.ASCII.GetBytes(e.Values[0].ToString());
          byte[] accelY = Encoding.ASCII.GetBytes(e.Values[1].ToString());
          byte[] accelZ = Encoding.ASCII.GetBytes(e.Values[2].ToString());
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
            }else if (i < 11)
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
          SendMessage(package);
          break;

        case SensorType.Gyroscope:
          byte[] gyrX = Encoding.ASCII.GetBytes(e.Values[0].ToString());
          byte[] gyrY = Encoding.ASCII.GetBytes(e.Values[1].ToString());
          byte[] gyrZ = Encoding.ASCII.GetBytes(e.Values[2].ToString());
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
              package[i] = gyrX[i-15];
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
          SendMessage(package);
          break;
        default:
          throw new Exception("Unhandled Sensor type");
      }
    }





    protected override void OnStart()
    {
      base.OnStart();
      if (mChatService is null)
      {
        SetupTransfer();
      }

      BroadcastReceiver = new SampleReceiver(adapter);
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
      SetupTransfer();
      //this.UnregisterReceiver(BroadcastReceiver);
    }

    private void SetupTransfer()
    {
      mChatService = new SendData(mHandler);
      mChatService.Start();
      string message = "TestingTestingTestingTestingTestingTEST";

      SendMessage(message);
    }

    private void SendMessage(string message)
    {
      if (mChatService.GetState() != StateEnum.Connected)
      {
        Toast.MakeText(Application.Context, "NOT CONNECTED", ToastLength.Short).Show();
        return;
      }
      byte[] send = Encoding.ASCII.GetBytes(message);
      Array.Resize(ref send, 39);
      //for (int i = 0; i < 50; i++)
      
        mChatService.Write(send);
      
    }

    private void SendMessage(byte[] message)
    {
      if (mChatService.GetState() != StateEnum.Connected)
      {
        Toast.MakeText(Application.Context, "NOT CONNECTED", ToastLength.Short).Show();
        return;
      }
      if(message.Length != 39)
      {
        Array.Resize(ref message, 39);
      }   

      mChatService.Write(message);
    }
  }





}

