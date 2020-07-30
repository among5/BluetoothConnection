using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
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
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Imaging;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace App4
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
    private Queue<DataPoint> valueList;
    private Queue<DataPoint> gyrValueList;
    private Stopwatch stopwatch;


    private const float DataStrokeThickness = 1;

    private readonly List<double> _dataAccelX = new List<double>();
    private readonly List<double> _dataAccelY = new List<double>();
    private readonly List<double> _dataAccelZ = new List<double>();
    private readonly List<double> _dataGyrX = new List<double>();
    private readonly List<double> _dataGyrY = new List<double>();
    private readonly List<double> _dataGyrZ = new List<double>();
    private int shift => (int)ShiftSlider.Value;

    private readonly ChartRenderer _chartRenderer;

    private DispatcherTimer dispatcherTimer;

    private double[] values;

    private int counter;

    public MainPage()
    {
      this.InitializeComponent();
      App.Current.Suspending += App_Suspending;
      counter = 0;
      ResultCollection = new ObservableCollection<RfcommChatDeviceDisplay>();
      valueList = new Queue<DataPoint>();
      gyrValueList = new Queue<DataPoint>();
     
      stopwatch = new Stopwatch();
      stopwatch.Start();
      DispatcherTimerSetup();

      _chartRenderer = new ChartRenderer();
      values = new double[9];
      DataContext = this;
      counter = 0;
     

    }

    public void DispatcherTimerSetup()
    {
      dispatcherTimer = new DispatcherTimer();
      dispatcherTimer.Tick += dispatchedTimer_Tick;

      dispatcherTimer.Interval = TimeSpan.FromMilliseconds(20);
      dispatcherTimer.Start();
    }

    public void dispatchedTimer_Tick(object sender, object e)
    {
      canvas.Invalidate();
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
        if ((deviceWatcher.Status == DeviceWatcherStatus.Started||
             deviceWatcher.Status == DeviceWatcherStatus.EnumerationCompleted))
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

      // Re-set device specific UX
      ChatBox.Visibility = Visibility.Collapsed;
      RequestAccessButton.Visibility = Visibility.Collapsed;
      stopwatch.Stop();
      stopwatch.Reset();
     // if (ConversationList.Items != null) ConversationList.Items.Clear();

     // valueList.Clear();

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
            NotifyUser(String.Format("{0} devices found.", ResultCollection.Count));
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
      /*
      if (resultsListView.SelectedItem != null)
      {
        rootPage.NotifyUser("Connecting to remote device. Please wait...", NotifyType.StatusMessage);
      }
      else
      {
        rootPage.NotifyUser("Please select an item to connect to", NotifyType.ErrorMessage);
        return;
      }
      */
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
      catch (Exception ex)
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
        NotifyUser("The Chat service is not advertising the Service Name attribute (attribute id=0x100). " +"Please verify that you are running the BluetoothRfcommChat server.");
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
        chatWriter = new DataWriter(chatSocket.OutputStream);

        DataReader chatReader = new DataReader(chatSocket.InputStream);
        ReceiveStringLoop(chatReader);
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
    private void SendButton_Click(object sender, RoutedEventArgs e)
    {
      SendMessage();
    }

    public void KeyboardKey_Pressed(object sender, KeyRoutedEventArgs e)
    {
      if (e.Key == Windows.System.VirtualKey.Enter)
      {
        SendMessage();
      }
    }

    /// <summary>
    /// Takes the contents of the MessageTextBox and writes it to the outgoing chatWriter
    /// </summary>
    private async void SendMessage()
    {
      try
      {
        if (MessageTextBox.Text.Length != 0)
        {
          chatWriter.WriteUInt32((uint)MessageTextBox.Text.Length);
          chatWriter.WriteString(MessageTextBox.Text);

        //  ConversationList.Items.Add("Sent: " + MessageTextBox.Text);
          MessageTextBox.Text = "";
          await chatWriter.StoreAsync();

        }
      }
      catch (Exception ex) when ((uint)ex.HResult == 0x80072745)
      {
        // The remote device has disconnected the connection
      NotifyUser("Remote side disconnect: " + ex.HResult.ToString() + " - " + ex.Message);
      }
    }

    private void NotifyUser(String text)
    {
      NotificationBox.Visibility = Visibility.Visible;
      Notification.Text = text;
    }
    private async void ReceiveStringLoop(DataReader chatReader)
    {

       
 
        // Debug.WriteLine("Dequeued!");
      
     
      try
      {
        uint size = await chatReader.LoadAsync(sizeof(uint));
        if (size < sizeof(uint))
        {
          Disconnect("Remote device terminated connection - make sure only one instance of server is running on remote device");
          return;
        }

        // uint stringLength = chatReader.ReadUInt32();
        uint stringLength = 39;
        uint actualStringLength = await chatReader.LoadAsync(stringLength);

        if (actualStringLength != stringLength)
        {
          // The underlying socket was closed before we were able to read the whole data
          return;
        }

        byte[] temp = new byte[39];
        chatReader.ReadBytes(temp);
        string store = "";
        //Stores time
        double AccelX = 0.0;
        //Stores X value of Accleration
        double AccelY = 0.0;
        double GyrX = 0.0;
        double GyrY = 0.0;
        for (int i = 0; i < 9; i++)
        {
          float curFloat = BitConverter.ToSingle(temp, i * 4 + 3);
          store = store + curFloat.ToString() + " ";
          values[i] = (Convert.ToDouble(BitConverter.ToSingle(temp, i * 4 + 3)));
          /*
            if (i == 0)
            {
              AccelY = Convert.ToDouble(BitConverter.ToSingle(temp, i * 4 + 3));
              AccelX = stopwatch.ElapsedMilliseconds;
              valueList.Enqueue(new DataPoint() { X = AccelX, Y = AccelY });
            }
            
            else if (i == 1)
            {
              AccelY = Convert.ToDouble(BitConverter.ToSingle(temp, i * 4 + 3));
              valueList.Add(new DataPoint() { X = AccelX, Y = AccelY });
            }
            
            else if (i == 3)
            {
              GyrY = Convert.ToDouble(BitConverter.ToSingle(temp, i * 4 + 3));
              GyrX = stopwatch.ElapsedMilliseconds;
              gyrValueList.Enqueue(new DataPoint() { X = GyrX, Y = GyrY });
            }
           
            else if (i == 4)
            {
              GyrY = Convert.ToDouble(BitConverter.ToSingle(temp, i * 4 + 3));
              gyrValueList.Add(new DataPoint() { X = GyrX, Y = GyrY });
            }
           
          */
        }

        
        
        // ConversationList.Items.Add("Accelerometer: " + text.Substring(3,4) + ", " + text.Substring(7,4) + ", " + text.Substring(11,4) + " Gyroscope: " + text.Substring(15,4) + ", " + text.Substring(19,4) + ", " + text.Substring(23,4));
        // SensorText.Text = text;

        SensorText.Text = store;
       // Debug.WriteLine(store);
        ReceiveStringLoop(chatReader);
      }
      catch (Exception ex)
      {
        lock (this)
        {
          if (chatSocket == null)
          {
            // Do not print anything here -  the user closed the socket.
            //  if ((uint)ex.HResult == 0x80072745)
            //     rootPage.NotifyUser("Disconnect triggered by remote device", NotifyType.StatusMessage);
            //  else if ((uint)ex.HResult == 0x800703E3)
            //     rootPage.NotifyUser("The I/O operation has been aborted because of either a thread exit or an application request.", NotifyType.StatusMessage);
          }
          else
          {
            Disconnect("Read stream failed with error: " + ex.Message);
          }
        }
      }

    }

    private void Canvas_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
    {
      _dataAccelX.Add(values[0]);
      _dataAccelY.Add(values[1]);
      _dataAccelZ.Add(values[2]);
      args.DrawingSession.Clear(Colors.FloralWhite);
      _chartRenderer.RenderData(canvas, args, Colors.Black, DataStrokeThickness, _dataAccelX, shift);
      _chartRenderer.RenderData(canvas, args, Colors.DarkGreen, DataStrokeThickness, _dataAccelY, shift);
      _chartRenderer.RenderData(canvas, args, Colors.Blue, DataStrokeThickness, _dataAccelZ, shift);
     // _chartRenderer.RenderMovingAverage(canvas, args, Colors.DeepSkyBlue, DataStrokeThickness, 50, _dataAccelX);
      _chartRenderer.RenderAxes(canvas, args);
      /*
      if (values != null)
      {
        _data.Add(values[0]);
      }
      else
      {
        _data.Add(0.0);
      }
      */

      if (_dataAccelX.Count > (int)canvas.ActualWidth)
      {
        _dataAccelX.RemoveRange(0, _dataAccelX.Count - (int)canvas.ActualWidth);
        _dataAccelY.RemoveRange(0, _dataAccelY.Count - (int)canvas.ActualWidth);
        _dataAccelZ.RemoveRange(0, _dataAccelZ.Count - (int)canvas.ActualWidth);
      }

     

      
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

      // rootPage.NotifyUser(disconnectReason, NotifyType.StatusMessage);
      ResetMainUI();
    }

    private void SetChatUI(string serviceName, string deviceName)
    {
      // rootPage.NotifyUser("Connected", NotifyType.StatusMessage);
      ServiceName.Text = "Service Name: " + serviceName;
      DeviceName.Text = "Connected to: " + deviceName;
      RunButton.IsEnabled = false;
      ConnectButton.Visibility = Visibility.Collapsed;
      RequestAccessButton.Visibility = Visibility.Visible;
      resultsListView.IsEnabled = false;
      resultsListView.Visibility = Visibility.Collapsed;
      ChatBox.Visibility = Visibility.Visible;
      NotificationBox.Visibility = Visibility.Collapsed;
      stopwatch.Start();


     

     
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

