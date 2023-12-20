using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO.Ports;

namespace examenProjectSem1_WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    StringBuilder _buffer = new StringBuilder();

    SerialPort _serialPort = new()
    {
        PortName = "COM3",
        BaudRate = 115200,
        Parity = Parity.None,
        StopBits = StopBits.One,
        DataBits = 8,
        Handshake = Handshake.None,
    };

    public MainWindow()
    {
        InitializeComponent();

        _serialPort.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
        _serialPort.Open();
    }

    void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
    {
        SerialPort sp = (SerialPort)sender;
        string indata = sp.ReadExisting();
        _buffer.Append(indata);

        if (_buffer.ToString().EndsWith('\0'))
        {
            string message = _buffer.ToString().Trim('\0');
            _buffer.Clear();
            
            if (message == "LEDSTATE HIGH")
            {
                Dispatcher.Invoke(() =>
                {
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/images/on.png"));
                    AddToCommandListView($"Received: {message}");
                });
            }

            if (message == "LEDSTATE LOW")
            {
                Dispatcher.Invoke(() =>
                {
                    image.Source = new BitmapImage(new Uri("pack://application:,,,/images/off.png"));
                    AddToCommandListView($"Received: {message}");
                });
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var nonClientAreaHeight = this.ActualHeight - this.RenderSize.Height;
        var desiredClientHeight = 300; // set desired client area height
        this.Height = desiredClientHeight + nonClientAreaHeight;
    }

    private void CommandTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (commandTextBox.Text.Length == 28) e.Handled = true;

        if (e.Key == Key.Enter && !string.IsNullOrEmpty(commandTextBox.Text.Trim()))
        {
            var command = commandTextBox.Text.Trim();
            if (commandTextBox.Text.Trim() == "LED ON" || commandTextBox.Text.Trim() == "LED OFF" || commandTextBox.Text.Trim() == "LED TOGGLE")
            {
                _serialPort.Write($"{commandTextBox.Text.Trim()}\0");
            }

            AddToCommandListView($"Send: {commandTextBox.Text.Trim()}");
            commandTextBox.Clear();
        }
    }

    private void AddToCommandListView(string text)
    {
        if (commandListView.Items.Count == 10)
        {
            commandListView.Items.RemoveAt(9);
        }

        commandListView.Items.Insert(0, text);
    }

    private void ShowUnknownCommandPopup()
    {
        Storyboard fadeInStoryboard = new()
        {
            Duration = new Duration(TimeSpan.FromMilliseconds(100)),
        };
        DoubleAnimation fadeInAnimation = new()
        {
            Duration = TimeSpan.FromMilliseconds(100),
            To = 1,
        };
        Storyboard fadeOutStoryboard = new()
        {
            Duration = new Duration(TimeSpan.FromMilliseconds(100))
        };
        DoubleAnimation fadeOutAnimation = new()
        {
            Duration = TimeSpan.FromMilliseconds(100),
            To = 0
        };
        Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath(OpacityProperty));
        fadeInStoryboard.Children.Add(fadeInAnimation);
        Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath(OpacityProperty));
        fadeInStoryboard.Children.Add(fadeOutAnimation);

        Popup commandNotFoundPopup = new()
        {
            PlacementTarget = commandTextBoxBorder,
            Placement = PlacementMode.Top,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = new TextBlock()
            {
                Foreground = Brushes.White,
                Text = "Unknown command",
            }
        };

        Storyboard.SetTarget(fadeInStoryboard, commandNotFoundPopup);
        fadeInStoryboard.Begin();

        /*Task.Delay(1000).ContinueWith(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                Storyboard.SetTarget(fadeOutStoryboard, commandNotFoundPopup);
                fadeOutStoryboard.Begin();
            });
        });*/
    }
}