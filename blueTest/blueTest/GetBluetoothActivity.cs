using Android.App;
using Android.Content;

namespace blueTest
{
  internal class GetBluetoothActivity : Java.Lang.Object, IDialogInterfaceOnClickListener
  {
    private MainActivity mainActivity;
    public GetBluetoothActivity(MainActivity activity)
    {
      mainActivity = activity;
    }
    public void OnClick(IDialogInterface dialog, int which)
    {
      Intent intent = new Intent(Application.Context, typeof(BluetoothActivity));
      mainActivity.StartActivity(intent);

    }
  }
}
