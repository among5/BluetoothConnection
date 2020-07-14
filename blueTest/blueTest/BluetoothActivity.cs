using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Support.Wearable.Activity;
using Android.Views;
using Android.Widget;

namespace blueTest
{
  public class BluetoothActivity : WearableActivity
  {
    private const int ENABLE_BT_REQUEST_CODE = 1;
    private const int DISCOVERABLE_DURATION = 300;
    private const int DISCOVERABLE_BT_REQUEST_CODE = 2;

    private TextView textView;

    private BluetoothAdapter bluetoothAdapter;
    private ToggleButton toggleButton;
    private ListView listView;
    private ArrayAdapter adapter;
    private SampleReceiver broadcastReceiver;

  
    protected override void OnCreate(Bundle bundle)
    {
      base.OnCreate(bundle);
      SetContentView(Resource.Layout.activity_main);

      textView = FindViewById<TextView>(Resource.Id.textView);
      SetAmbientEnabled();



      toggleButton = FindViewById<ToggleButton>(Resource.Id.toggleButton);

      listView = FindViewById<ListView>(Resource.Id.foundDevices);
      String[] animalList = { "Lion", "Tiger", "Moose" };
      adapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleListItem1, Resource.Id.textView, animalList);
      listView.Adapter = adapter;
      bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

      if (!bluetoothAdapter.IsEnabled)
      {
        Intent enableBlueToothIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
        StartActivityForResult(enableBlueToothIntent, ENABLE_BT_REQUEST_CODE);
      }

      toggleButton.Click += delegate
      {

        adapter.Clear();

        if (bluetoothAdapter == null)
        {
          Toast.MakeText(Application.Context, "Device doesn't support bluetooth", ToastLength.Short).Show();
          toggleButton.Checked = false;
        }
        else
        {
          if (toggleButton.Checked == true)
          {
            if (!bluetoothAdapter.IsEnabled)
            {
              Intent enableBluetoothIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
              StartActivityForResult(enableBluetoothIntent, ENABLE_BT_REQUEST_CODE);
            }
            else
            {

              Toast.MakeText(Application.Context, "Device already enabled " + "\n" + "Scanning for remote Bluetooth devices..", ToastLength.Short).Show();
              discoverDevices();
              makeDiscoverable();
            }
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


    //public void OnToggleClicked(View view)
    //{

    //  adapter.Clear();

    //  ToggleButton toggleButton = (ToggleButton)view;
    //  if (bluetoothAdapter == null)
    //  {
    //    Toast.MakeText(Application.Context, "Device doesn't support bluetooth", ToastLength.Short).Show();
    //    toggleButton.Checked = false;
    //  }
    //  else
    //  {
    //    if (toggleButton.Checked == true)
    //    {
    //      if (!bluetoothAdapter.IsEnabled)
    //      {
    //        Intent enableBluetoothIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
    //        StartActivityForResult(enableBluetoothIntent, ENABLE_BT_REQUEST_CODE);
    //      }
    //      else
    //      {

    //        Toast.MakeText(Application.Context, "Device already enabled " + "\n" + "Scanning for remote Bluetooth devices..", ToastLength.Short).Show();
    //        discoverDevices();
    //        makeDiscoverable();
    //      }
    //    }
    //    else
    //    {
    //      bluetoothAdapter.Disable();
    //      adapter.Clear();
    //      Toast.MakeText(Application.Context, "Device now disabled", ToastLength.Short).Show();
    //    }
    //  }
    //}


    public void onActivityResult(int requestCode, int resultCode, Intent data)
    {
      if (requestCode == ENABLE_BT_REQUEST_CODE)
      {
        if (resultCode == -1)
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
        if (resultCode == DISCOVERABLE_DURATION)
        {
          Toast.MakeText(Application.Context, "Device now discoverable for " + DISCOVERABLE_DURATION + " seconds", ToastLength.Short).Show();
        }
        else
        {
          Toast.MakeText(Application.Context, "Failed to enable discoverability on device.", ToastLength.Short).Show();
        }
      }
    }

    protected void makeDiscoverable()
    {
      Intent discoverableIntent = new Intent(BluetoothAdapter.ActionRequestDiscoverable);
      discoverableIntent.PutExtra(BluetoothAdapter.ExtraDiscoverableDuration, DISCOVERABLE_DURATION);
      StartActivityForResult(discoverableIntent, DISCOVERABLE_BT_REQUEST_CODE);
    }

    protected void discoverDevices()
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

    protected override void OnResume()
    {
      base.OnResume();
      IntentFilter filter = new IntentFilter(BluetoothDevice.ActionFound);
      this.RegisterReceiver(broadcastReceiver, filter);
    }

    protected override void OnPause()
    {
      base.OnPause();
      this.UnregisterReceiver(broadcastReceiver);
    }
  }
}
