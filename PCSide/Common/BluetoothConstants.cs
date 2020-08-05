using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InertialSensor.Common
{
  public class BluetoothConstants
  {
    public const int NumDataPoints = 10;
    public const int SingleDataPoint = 39;
    public const int BluetoothPackage = NumDataPoints * SingleDataPoint;
  }
}
