using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using InertialSensor.Common;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InertialSensor.Desktop
{
  /// <summary>
  /// An empty page that can be used on its own or navigated to within a Frame.
  /// </summary>
  public sealed partial class MainPage : Page
  {
    private DeviceWatcher deviceWatcher;
    private StreamSocket chatSocket;
    private DataWriter chatWriter;
    private RfcommDeviceService chatService;
    private BluetoothDevice bluetoothDevice;

    private const float DataStrokeThickness = 1;
    private readonly List<XYZ> Acceleration = new List<XYZ>();
    private readonly List<XYZ> accelerationTemp = new List<XYZ>();

    private readonly ChartRenderer _chartRenderer;

    private Boolean saveToFile;
    private List<double> buffer;
    private readonly object bufferLock = new object();

    private byte[] storeInputData = new byte[BluetoothConstants.BluetoothPackage];
    private Logger logger;
    private Stopwatch st;

    public MainPage()
    {
      this.InitializeComponent();
      App.Current.Suspending += App_Suspending;
      ResultCollection = new ObservableCollection<RfcommChatDeviceDisplay>();
      _chartRenderer = new ChartRenderer();
      Acceleration.Add(new XYZ(0.0, 0.0, 0.0));
      // logger = new Logger("dataLog.txt");
      //st = new Stopwatch();
      canvas.TargetElapsedTime = TimeSpan.FromMilliseconds(100);
      DataContext = this;
    }

    public ObservableCollection<RfcommChatDeviceDisplay> ResultCollection
    {
      get;
      private set;
    }

    private void StopWatcher()
    {
      if (null != deviceWatcher)
      {
        if (deviceWatcher.Status == DeviceWatcherStatus.Started ||
             deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted)
        {
          deviceWatcher.Stop();
        }
        deviceWatcher = null;
      }
    }

    void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
    {
      // Make sure we clean up resources on suspend.
      Disconnect("App Suspension disconnects");
    }

    /// <summary>
    /// When the user presses the run button, query for all nearby unpaired devices
    /// Note that in this case, the other device must be running the Rfcomm Chat Server before being paired.
    /// </summary>
    /// <param name="sender">Instance that triggered the event.</param>
    /// <param name="e">Event data describing the conditions that led to the event.</param>
    private void RunButton_Click(object sender, RoutedEventArgs e)
    {
      if (deviceWatcher == null)
      {
        SetDeviceWatcherUI();
        StartUnpairedDeviceWatcher();
        ConnectButton.IsEnabled = true;
      }
      else
      {
        ResetMainUI();
      }
    }

    private void SetDeviceWatcherUI()
    {
      // Disable the button while we do async operations so the user can't Run twice.
      RunButton.Content = "Stop";
      NotifyUser("Device watcher started");
      resultsListView.Visibility = Visibility.Visible;
      resultsListView.IsEnabled = true;
    }

    private void ResetMainUI()
    {
      RunButton.Content = "Start";
      RunButton.IsEnabled = true;
      ConnectButton.Visibility = Visibility.Visible;
      resultsListView.Visibility = Visibility.Visible;
      resultsListView.IsEnabled = true;
      ConnectButton.IsEnabled = false;
      // Re-set device specific UX
      ChatBox.Visibility = Visibility.Collapsed;
      StopWatcher();
    }

    private void StartUnpairedDeviceWatcher()
    {
      // Request additional properties
      string[] requestedProperties = new string[] { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected" };

      deviceWatcher = DeviceInformation.CreateWatcher("(System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\")",
                                                      requestedProperties,
                                                      DeviceInformationKind.AssociationEndpoint);

      // Hook up handlers for the watcher events before starting the watcher
      deviceWatcher.Added += new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
      {
        // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
        {
          // Make sure device name isn't blank
          if (deviceInfo.Name != "")
          {
            ResultCollection.Add(new RfcommChatDeviceDisplay(deviceInfo));
            NotifyUser(string.Format("{0} devices found.", ResultCollection.Count));
          }

        });
      });

      deviceWatcher.Updated += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
      {
        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
        {
          foreach (RfcommChatDeviceDisplay rfcommInfoDisp in ResultCollection)
          {
            if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
            {
              rfcommInfoDisp.Update(deviceInfoUpdate);
              break;
            }
          }
        });
      });

      deviceWatcher.EnumerationCompleted += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
      {
        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
        {
          NotifyUser(String.Format("{0} devices found. Enumeration completed. Watching for updates...", ResultCollection.Count));
        });
      });

      deviceWatcher.Removed += new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
      {
        // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
        {
          // Find the corresponding DeviceInformation in the collection and remove it
          foreach (RfcommChatDeviceDisplay rfcommInfoDisp in ResultCollection)
          {
            if (rfcommInfoDisp.Id == deviceInfoUpdate.Id)
            {
              ResultCollection.Remove(rfcommInfoDisp);
              break;
            }
          }
        });
      });

      deviceWatcher.Stopped += new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
      {
        await this.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
        {
          ResultCollection.Clear();
        });
      });

      deviceWatcher.Start();
    }

    /// <summary>
    /// Invoked once the user has selected the device to connect to.
    /// Once the user has selected the device,
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
      // Make sure user has selected a device first
      if (resultsListView.SelectedItem != null)
      {
        NotifyUser("Connecting to remote device. Please hold...");
      }
      else
      {
        NotifyUser("Please select an item to connect to");
        return;
      }

      RfcommChatDeviceDisplay deviceInfoDisp = resultsListView.SelectedItem as RfcommChatDeviceDisplay;

      // Perform device access checks before trying to get the device.
      // First, we check if consent has been explicitly denied by the user.
      DeviceAccessStatus accessStatus = DeviceAccessInformation.CreateFromId(deviceInfoDisp.Id).CurrentStatus;
      if (accessStatus == DeviceAccessStatus.DeniedByUser)
      {
        return;
      }

      // If not, try to get the Bluetooth device
      try
      {
        bluetoothDevice = await BluetoothDevice.FromIdAsync(deviceInfoDisp.Id);
      }
      catch
      {
        ResetMainUI();
        return;
      }

      // If we were unable to get a valid Bluetooth device object,
      // it's most likely because the user has specified that all unpaired devices
      // should not be interacted with.
      if (bluetoothDevice == null)
      {
        NotifyUser("Bluetooth Device returned null. Access Status = " + accessStatus.ToString());
      }

      // This should return a list of uncached Bluetooth services (so if the server was not active when paired, it will still be detected by this call
      var rfcommServices = await bluetoothDevice.GetRfcommServicesForIdAsync(
          RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid), BluetoothCacheMode.Uncached);

      if (rfcommServices.Services.Count > 0)
      {
        chatService = rfcommServices.Services[0];
      }
      else
      {
        NotifyUser("Could not discover the chat service on the remote device");
        ResetMainUI();
        return;
      }

      // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service
      var attributes = await chatService.GetSdpRawAttributesAsync();
      if (!attributes.ContainsKey(Constants.SdpServiceNameAttributeId))
      {
        NotifyUser("The Chat service is not advertising the Service Name attribute (attribute id=0x100). " + "Please verify that you are running the BluetoothRfcommChat server.");
        ResetMainUI();
        return;
      }
      var attributeReader = DataReader.FromBuffer(attributes[Constants.SdpServiceNameAttributeId]);
      var attributeType = attributeReader.ReadByte();
      if (attributeType != Constants.SdpServiceNameAttributeType)
      {
        NotifyUser("The Chat service is using an unexpected format for the Service Name attribute. " + "Please verify that you are running the BluetoothRfcommChat server.");
        ResetMainUI();
        return;
      }
      var serviceNameLength = attributeReader.ReadByte();

      // The Service Name attribute requires UTF-8 encoding.
      attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;

      StopWatcher();

      lock (this)
      {
        chatSocket = new StreamSocket();
      }
      try
      {
        await chatSocket.ConnectAsync(chatService.ConnectionHostName, chatService.ConnectionServiceName);
        SetChatUI(attributeReader.ReadString(serviceNameLength), bluetoothDevice.Name);

        if (checkBox.IsChecked == true)
        {
          saveToFile = true;
         
        }
        else
        {
          saveToFile = false;
        }

        chatWriter = new DataWriter(chatSocket.OutputStream);
        Thread thread = new Thread(() =>
        {
          DataReader chatReader = new DataReader(chatSocket.InputStream);
          ReceiveStringLoop(chatReader);
        });
        thread.Start();
      }
      catch (Exception ex) when ((uint)ex.HResult == 0x80070490) // ERROR_ELEMENT_NOT_FOUND
      {
        NotifyUser("Please verify that you are running the BluetoothRfcommChat server.");
        ResetMainUI();
      }
      catch (Exception ex) when ((uint)ex.HResult == 0x80072740) // WSAEADDRINUSE
      {
        NotifyUser("Please verify that there is no other RFCOMM connection to the same device.");
        ResetMainUI();
      }
    }

    /// <summary>
    ///  If you believe the Bluetooth device will eventually be paired with Windows,
    ///  you might want to pre-emptively get consent to access the device.
    ///  An explicit call to RequestAccessAsync() prompts the user for consent.
    ///  If this is not done, a device that's working before being paired,
    ///  will no longer work after being paired.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void RequestAccessButton_Click(object sender, RoutedEventArgs e)
    {
      // Make sure user has given consent to access device
      DeviceAccessStatus accessStatus = await bluetoothDevice.RequestAccessAsync();
      if (accessStatus != DeviceAccessStatus.Allowed)
      {
        NotifyUser("Access to the device is denied because the application was not granted access");
      }
      else
      {
        NotifyUser("Access granted, you are free to pair devices");
      }
    }

    private void NotifyUser(String text)
    {
      NotificationBox.Visibility = Visibility.Visible;
      Notification.Text = text;
    }
    private async void ReceiveStringLoop(DataReader chatReader)
    {
      try
      {
        while (true)
        {
          //st.Restart();
          uint actualStringLength = await chatReader.LoadAsync(BluetoothConstants.BluetoothPackage);
          if (actualStringLength < BluetoothConstants.BluetoothPackage)
          {
            Disconnect("Remote device terminated connection - make sure only one instance of server is running on remote device");
            return;
          }
          chatReader.ReadBytes(storeInputData);

         // string store = "";
          var bufferTemp = new List<double>();
          var temp = new List<XYZ>();
          for(int j =0; j< storeInputData.Length; j += BluetoothConstants.SingleDataPoint) {
            for (int i = 0; i < 3; i++)
            {
              // float curFloat = BitConverter.ToSingle(storeInputData, i * 4 + 3);
              // store = store + curFloat.ToString() + " ";
              bufferTemp.Add(BitConverter.ToSingle(storeInputData, i * 4 + 3));
            }
            temp.Add(new XYZ(bufferTemp[0], bufferTemp[1], bufferTemp[2]));
          }
          

          // Lock to copy over the data
          lock (bufferLock)
          {
           
            accelerationTemp.AddRange(temp);            
          }
/*
          if (saveToFile)
          {
            logger.logData(counter + " " + store + " " + DateTime.Now.ToString());
          }
          */
          //st.Stop();
          //Debug.WriteLine(st.ElapsedTicks);
          //SensorText.Text = store;
        }
      }
      catch (Exception ex)
      {
        lock (this)
        {
          if (chatSocket == null)
          {
            // Do not print anything here -  the user closed the socket.
            if ((uint)ex.HResult == 0x80072745)
              this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { NotifyUser("Disconnect triggered by remote device"); }).GetAwaiter().GetResult();
            else if ((uint)ex.HResult == 0x800703E3)
              this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { NotifyUser("The I/O operation has been aborted because of either a thread exit or an application request."); }).GetAwaiter().GetResult();
          }
          else
          {
            this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { Disconnect("Read stream failed with error: " + ex.Message); }).GetAwaiter().GetResult();
          }
        }
      } 
    }

   
    private void Canvas_UpdateData(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
    {
     // st.Restart();
      if (accelerationTemp.Count != 0)
      {
        lock (bufferLock)
        {
          Acceleration.AddRange(accelerationTemp);
          accelerationTemp.Clear();
        }
         /* 
        st.Stop();
        counter++;
        average = (average + st.ElapsedTicks);
        Debug.WriteLine(average/counter + " " + st.ElapsedTicks);*/  
      }
    }

    private void Canvas_OnDraw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
    {
      // st.Restart();
      canvas.IsFixedTimeStep = false;
      int width = Constants.ChartWidth;
      if (Acceleration.Count > width)
      {
        Acceleration.RemoveRange(0, Acceleration.Count - width);
      }

      _chartRenderer.RenderData(canvas, args, Colors.Black, DataStrokeThickness, Acceleration);
      _chartRenderer.RenderAxes(canvas, args);
      // st.Stop();
      // Debug.WriteLine(st.ElapsedTicks + "!");
    }

    private void DisconnectButton_Click(object sender, RoutedEventArgs e)
    {
      Disconnect("Disconnected");
    }

    /// <summary>
    /// Cleans up the socket and DataWriter and reset the UI
    /// </summary>
    /// <param name="disconnectReason"></param>
    private void Disconnect(string disconnectReason)
    {
      if (chatWriter != null)
      {
        chatWriter.DetachStream();
        chatWriter = null;
      }

      if (chatService != null)
      {
        chatService.Dispose();
        chatService = null;
      }

      lock (this)
      {
        if (chatSocket != null)
        {
          chatSocket.Dispose();
          chatSocket = null;
        }
      }

      NotifyUser(disconnectReason);
      Acceleration.Clear();
      Acceleration.Add(new XYZ(0.0, 0.0, 0.0));
      ResetMainUI();
    }

    private void SetChatUI(string serviceName, string deviceName)
    {
      NotifyUser("Connected");
      DeviceName.Text = "Connected to: " + deviceName;
      RunButton.IsEnabled = false;
      ConnectButton.Visibility = Visibility.Collapsed;
      resultsListView.IsEnabled = false;
      resultsListView.Visibility = Visibility.Collapsed;
      ChatBox.Visibility = Visibility.Visible;
      NotificationBox.Visibility = Visibility.Collapsed;
    }

    private void ResultsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      UpdatePairingButtons();
    }

    private void UpdatePairingButtons()
    {
      RfcommChatDeviceDisplay deviceDisp = (RfcommChatDeviceDisplay)resultsListView.SelectedItem;
      if (null != deviceDisp)
      {
        ConnectButton.IsEnabled = true;
      }
      else
      {
        ConnectButton.IsEnabled = false;
      }
    }

    private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
    {
    }

    private void Notification_SelectionChanged(object sender, RoutedEventArgs e)
    {

    }
  }

  public class RfcommChatDeviceDisplay : INotifyPropertyChanged
  {
    private DeviceInformation deviceInfo;

    public RfcommChatDeviceDisplay(DeviceInformation deviceInfoIn)
    {
      deviceInfo = deviceInfoIn;
      UpdateGlyphBitmapImage();
    }

    public DeviceInformation DeviceInformation
    {
      get
      {
        return deviceInfo;
      }

      private set
      {
        deviceInfo = value;
      }
    }

    public string Id
    {
      get
      {
        return deviceInfo.Id;
      }
    }

    public string Name
    {
      get
      {
        return deviceInfo.Name;
      }
    }

    public BitmapImage GlyphBitmapImage
    {
      get;
      private set;
    }

    public void Update(DeviceInformationUpdate deviceInfoUpdate)
    {
      deviceInfo.Update(deviceInfoUpdate);
      UpdateGlyphBitmapImage();
    }

    private async void UpdateGlyphBitmapImage()
    {
      DeviceThumbnail deviceThumbnail = await deviceInfo.GetGlyphThumbnailAsync();
      BitmapImage glyphBitmapImage = new BitmapImage();
      await glyphBitmapImage.SetSourceAsync(deviceThumbnail);
      GlyphBitmapImage = glyphBitmapImage;
      OnPropertyChanged("GlyphBitmapImage");
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string name)
    {
      PropertyChangedEventHandler handler = PropertyChanged;
      if (handler != null)
      {
        handler(this, new PropertyChangedEventArgs(name));
      }
    }
  }
}

