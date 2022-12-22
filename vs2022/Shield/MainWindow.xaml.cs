using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Shield
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class ConfigSchema
        {
            public class App
            {
                public string fileName { get; set; } = "";
                public string arguments { get; set; } = "";
                public string workingDirectory { get; set; } = "";
            }
            public App app { get; set; } = new App();
            public int watchInterval { get; set; } = 20;
            public int clickTrigger { get; set; } = 10;
        }
        private ConfigSchema? config_ = null;

        private long lastTimestamp_ = 0;
        private int clickCounter_ = 0;
        private int clickTrigger_ = 10;
        private bool watching_ = true;
        private int watchInterval_ = 20;
        private int watchTimer_ = 0;
        private Process? processor_ = null;

        /// <summary>
        /// 启动的时间戳，单位秒
        /// </summary>
        private readonly long startupTimestamp_ = 0;

        public MainWindow()
        {
            startupTimestamp_ = (DateTime.UtcNow.Ticks - 621355968000000000) / (10 * 1000 * 1000);

            InitializeComponent();
            if (Width > Height)
            {
                imgH.Visibility = Visibility.Visible;
                imgV.Visibility = Visibility.Collapsed;
            }
            else
            {
                imgH.Visibility = Visibility.Collapsed;
                imgV.Visibility = Visibility.Visible;
            }

            readConfig();

            Task.Run(() =>
            {
                watchTimer_ = watchInterval_;
                string text = "";
                while (true)
                {
                    Thread.Sleep(1000);
                    if (null != processor_)
                        continue;
                    if (!watching_)
                        continue;
                    watchTimer_ -= 1;
                    if (watchTimer_ <= 0)
                    {
                        text = "";
                        watchTimer_ = watchInterval_;
                        execute();
                    }
                    else
                    {
                        text = watchTimer_.ToString();
                    }
                    Dispatcher.Invoke(() =>
                    {
                        textTimer.Text = text;
                    });
                }
            });
        }

        private void OnOnClicked(object sender, RoutedEventArgs e)
        {
            clickCheck(() =>
            {
                btnOn.Visibility = Visibility.Collapsed;
                btnOff.Visibility = Visibility.Visible;
                watching_ = false;
                textTimer.Visibility = Visibility.Collapsed;
            });
        }

        private void OnOffClicked(object sender, RoutedEventArgs e)
        {
            clickCheck(() =>
            {
                btnOn.Visibility = Visibility.Visible;
                btnOff.Visibility = Visibility.Collapsed;
                watching_ = true;
                textTimer.Visibility = Visibility.Visible;
                watchTimer_ = watchInterval_;
                textTimer.Text = watchTimer_.ToString();
            });
        }

        private void clickCheck(Action _onPass)
        {
            long ticks = (DateTime.UtcNow.Ticks - 621355968000000000) / (10 * 1000);
            Console.WriteLine(ticks.ToString());
            if (ticks - lastTimestamp_ < 500)
            {
                clickCounter_++;
            }
            else
            {
                clickCounter_ = 0;
            }
            lastTimestamp_ = ticks;

            if (clickCounter_ > clickTrigger_)
            {
                clickCounter_ = 0;
                lastTimestamp_ = 0;
                _onPass();
            }
        }

        private void execute()
        {
            if (null == config_)
                return;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = config_.app.fileName;
                psi.Arguments = config_.app.arguments;
                psi.WorkingDirectory = config_.app.workingDirectory;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                processor_ = Process.Start(psi);
                if (null != processor_)
                {
                    processor_.EnableRaisingEvents = true;
                    processor_.Exited += new EventHandler(handleProcExited);
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void handleProcExited(object? _sender, EventArgs _e)
        {
            processor_ = null;
        }

        private void readConfig()
        {
            if (!File.Exists("./config.json"))
            {
                return;
            }

            try
            {
                string content = File.ReadAllText("./config.json");
                config_ = JsonSerializer.Deserialize<ConfigSchema>(content);
                if (null != config_)
                {
                    watchInterval_ = config_.watchInterval;
                    clickTrigger_ = config_.clickTrigger;
                }
            }
            catch (Exception ex)
            {

            }
        }

        private void onImageClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            long ticks = (DateTime.UtcNow.Ticks - 621355968000000000) / (10 * 1000 * 1000);
            // 仅允许启动后1分钟内使用隐藏点击退出
            if (ticks - startupTimestamp_ > 60)
            {
                return;
            }

            clickCheck(() =>
            {
                Application.Current.Shutdown();
            });
        }
    }
}
