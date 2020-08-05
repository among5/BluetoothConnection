using System;

namespace InertialSensor.Watch
{
  class Constants
  {
    public const int MESSAGE_STATE_CHANGE = 1;
    public const int MESSAGE_READ = 2;
    public const int MESSAGE_WRITE = 3;
    public const int MESSAGE_DEVICE_NAME = 4;
    public const int MESSAGE_TOAST = 5;

    // Key names received from the BluetoothChatService Handler
    public const string DEVICE_NAME = "device_name";
    public const string TOAST = "toast";
  }
}
