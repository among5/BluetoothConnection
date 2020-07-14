using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Support.Wearable.Activity;
using Android.Widget;
using System.Text;

namespace blueTest
{
  [Activity(Label = "@string/app_name", MainLauncher = true)]
  public class MainActivity : WearableActivity
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

    protected override void OnStart()
    {
      base.OnStart();
      if (mChatService is null)
      {
        SetupTransfer();
      }

      BroadcastReceiver = new SampleReceiver(adapter);
      //  this.RegisterReceiver(BroadcastReceiver);
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
      //this.UnregisterReceiver(BroadcastReceiver);
    }

    private void SetupTransfer()
    {
      mChatService = new SendData(mHandler);
      mChatService.Start();
      string message = "Testing";

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
      for (int i = 0; i < 50; i++)
      {
        mChatService.Write(send);
      }
    }
  }





}

