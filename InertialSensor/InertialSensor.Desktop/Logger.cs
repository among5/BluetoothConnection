using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace InertialSensor.Desktop
{
  class Logger
  {
    StorageFile sampleFile;
    public Logger(string fileName)
    {
      StorageFolder path = (ApplicationData.Current.TemporaryFolder);
      sampleFile = path.CreateFileAsync(fileName,
         Windows.Storage.CreationCollisionOption.OpenIfExists).GetAwaiter().GetResult();
      FileIO.WriteTextAsync(sampleFile, "Swift as a shadow").GetAwaiter().GetResult();
    }

    public async void logData(string data)
    {
         using (StreamWriter writer = File.AppendText(sampleFile.Path))
      {
          writer.WriteLine(data);
        }
    }

    public String getPath()
    {
      return sampleFile.Path;
    }

  }
}
