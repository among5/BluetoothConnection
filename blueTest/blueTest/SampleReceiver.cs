using Android.Bluetooth;
using Android.Content;
using Android.Widget;

namespace blueTest
{
  [BroadcastReceiver(Enabled = true, Exported = false)]
  public class SampleReceiver : BroadcastReceiver
  {
    private readonly ArrayAdapter adapter;

    public SampleReceiver()
    { }

    public SampleReceiver(ArrayAdapter givenAdapter)
    {
      this.adapter = givenAdapter;
    } 

    public override void OnReceive(Context context, Intent intent)
    {
      var action = intent.Action;
      //When remote bluetooth device is found
      if (BluetoothDevice.ActionFound.Equals(action))
      {
        //Get bluetooth device from Intent
        BluetoothDevice device = (BluetoothDevice) intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
        //Add name and address to an array adapter to show in ListView
        adapter.Add(device.Name + "\n" + device.Address);
      }
    }
  }
}
