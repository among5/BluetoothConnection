namespace blueTest
{
  public enum StateEnum
  {
    None = 0,   // we're doing nothing
    Listen,     // now listening for incoming connections
    Connecting, // now initiating an outgoing connection
    Connected,  // now connected to a remote device
  }
}
