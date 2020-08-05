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
      
    /*
      var stream = await sampleFile.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite);
      using (var outputStream = stream.GetOutputStreamAt(0))
      {
        using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
        {
          dataWriter.WriteString(data);
          await dataWriter.StoreAsync();
          await outputStream.FlushAsync();
        }
      }
      stream.Dispose(); // Or use the stream variable (see previous code snippet) with a using statement as well.
      */
     // await FileIO.WriteTextAsync(sampleFile, data);
    }

  }
}
