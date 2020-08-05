using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App4
{
  class Constants
  {
    // The Chat Server's custom service Uuid: 34B1CF4D-1069-4AD6-89B6-E161D79BE4D8
    public static readonly Guid RfcommChatServiceUuid = Guid.Parse("c88ae110-c0e0-11ea-b3de-0242ac130004");

    // The Id of the Service Name SDP attribute
    public const UInt16 SdpServiceNameAttributeId = 0x100;

    // The SDP Type of the Service Name SDP attribute.
    // The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
    //    -  the Attribute Type size in the least significant 3 bits,
    //    -  the SDP Attribute Type value in the most significant 5 bits.
    public const byte SdpServiceNameAttributeType = (4 << 3) | 5;

    // The value of the Service Name SDP attribute
    public const string SdpServiceName = "Bluetooth Rfcomm Chat Service";

    public const int ChartWidth = 999;

    public const int ChartHeight = 600;
  }
}
