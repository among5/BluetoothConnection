using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

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