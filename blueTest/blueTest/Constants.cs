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
  class Constants
  {
    public static readonly int MESSAGE_STATE_CHANGE = 1;
    public static readonly int MESSAGE_READ = 2;
    public static readonly int MESSAGE_WRITE = 3;
    public static readonly int MESSAGE_DEVICE_NAME = 4;
    public static readonly int MESSAGE_TOAST = 5;

    // Key names received from the BluetoothChatService Handler
    public static readonly String DEVICE_NAME = "device_name";
    public static readonly String TOAST = "toast";
  }
}
