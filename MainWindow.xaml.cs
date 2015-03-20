using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;

namespace DigiHash
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {        
        private DataSource _dataSource;

        public MainWindow()
        {            
            InitializeComponent();
            this.Paragraph = new Paragraph() { Margin = new Thickness(0, 0, 0, 0) };
            this.OutputRichTextBox.Document.Blocks.Add(this.Paragraph);
            this.Output(MessageType.System, string.Format("{0} Version {1}\n", this.Title, Assembly.GetEntryAssembly().GetName().Version));

            this.PreferenceButton.Click += (sender, eventArgs) => this.ShowPreferenceDialog();
            this.ActionButton.Click += (sender, eventArgs) =>
                {
                    if (this._dataSource == null || this._dataSource.Algorithms == null)
                        MessageBox.Show("Please wait for retrieving data from server!", this.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    else if (this._dataSource.Preference == null || !this._dataSource.Preference.IsValid)
                        this.ShowPreferenceDialog();
                    else
                    {
                        if (this._dataSource.Started)
                            this.Stop();
                        else
                            this.Start();
                    }
                };
            this.Closing += (sender, eventArgs) =>
                {
                    if (this._dataSource != null)
                    {
                        this.Stop();
                        eventArgs.Cancel = this._dataSource.Miner != null;
                    }
                };
            this.Loaded += (sender, eventArgs) => 
                {
                    Task.Run(() =>
                    {
                        this.CPUs = this.Execute(MessageType.System, "Getting CPU info", true, () => Hardware.GetCPUs());
                        this.GPUs = this.Execute(MessageType.System, "Getting GPU info", true, () => Hardware.GetGPUs());

                        this.Execute(MessageType.Server, "Retrieving data from server", false, () =>
                            {
                                this.RetrieveData("- Algorithms", true, "algorithms/gets", null,
                                    algorithmJson =>
                                    {
                                        var algorithms = JsonConvert.DeserializeObject<Algorithm[]>(algorithmJson);

                                        var parameters = new List<KeyValuePair<string, string>>();
                                        parameters.Add(new KeyValuePair<string, string>("device", MinerDevice.GPU.ToString()));
                                        parameters.Add(new KeyValuePair<string, string>("name", this.GPUs.First().Model));
                                        this.RetrieveData("- GPU family list", true, "DeviceFamilies/gets", parameters.ToArray(),
                                            gpuSeriesJson =>
                                            {
                                                var gpuSeries = JsonConvert.DeserializeObject<string[]>(gpuSeriesJson);

                                                this.Dispatcher.BeginInvoke(new Action(() =>
                                                {
                                                    this.IniaitalizeData(algorithms, gpuSeries);
                                                }));
                                            });
                                    });


                            });

                    });
                }; 

        }

        private Paragraph Paragraph { get; set; }

        private void ShowPreferenceDialog()
        {
            if (this._dataSource != null)
            {
                var dialog = new PreferenceDialog();
                var perference = dialog.Show(this, this._dataSource);
                if (perference != null)
                {
                    this._dataSource.Preference = perference;
                    if (this._dataSource.Preference.IsValid)
                        this.SavePreference();
                }
            }
        }

        private void Stop()
        {
            if (this._dataSource != null && this._dataSource.Miner != null)
            {
                var result = MessageBox.Show("Are you sure you would like to stop mining?", "Terminal Mining", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.OK)
                    this.ForceStop();
            }
        }

        private void ForceStop()
        {            
            this.Execute(MessageType.System, "Stop", true, () =>
            {
                this._dataSource.Started = false;
                try
                {
                    this._dataSource.Miner.Kill();
                    this._dataSource.Miner.Close();
                }
                catch (Exception)
                {
                }
                
                this._dataSource.Miner = null;
            });
        }

        private bool SavePreference()
        {
            return this.Execute(MessageType.System, "Saving preference", true, () =>
            {
                var json = JsonConvert.SerializeObject(this._dataSource.Preference);
                File.WriteAllText(this.GetFullPath(Preference.FileName), json);

                return true;
            });
        }

        internal GPU[] GPUs { get; set; }
        internal CPU[] CPUs { get; set; }

        internal void RetrieveMinerSetting(Preference preference, Action<MinerConfig> success, Action<string> fail = null)
        {
            var spec = new
                {
                    algorithm = preference.Algorithm,
                    username = preference.Wallet,
                    platform = new
                    {
                        name = "WINDOWS_X" + (Environment.Is64BitOperatingSystem ? "64" : "32"),
                        version = string.Format("{0}.{1}", Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor)
                    },
                    cpu = this.CPUs.Select(current => new
                    {
                        name = current.Model,
                        clock = current.Clock,
                        manufacturer = current.Manufacturer,
                        number_of_cores = current.NumberOfCores,
                        number_of_logical_processors = current.NumberOfLogicalProcessors,
                    }).First(),
                    gpu = this.GPUs.Select(current => new { name = preference.GPUModel ?? current.Model, manufacturer = current.Manufacturer }).First(),
                };

            this.PostData("Analyzing hardware from server", true, "MinerConfigs/analyze", JsonConvert.SerializeObject(new { spec }),
                json =>
                {
                    var config = JsonConvert.DeserializeObject<MinerConfig>(json);
                    success(config);
                }, fail);
        }

        private void Start()
        {
            this._dataSource.Started = true;            
            Task.Run(() =>
            {
                var runMiner = new Action<MinerConfig>(
                    config =>
                    {
                        var result = true;
                        this.Output(MessageType.System, config.Summary + "\n");

                        var save = config.Hardware_ID != this._dataSource.Preference.HardwareID;
                        if (config.Device == MinerDevice.GPU && !string.IsNullOrEmpty(config.SDK_URL))
                        {
                            if (!this._dataSource.Preference.InstalledSDK)
                            {
                                var dialogResult = MessageBox.Show("Do you have an SDK for your video card installed?", "Install SDK", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                                if (dialogResult == MessageBoxResult.No)
                                {
                                    result = false;
                                    this.Execute(MessageType.System, "No video card SDK install. Goto the follow URL to download SDK\n" + config.SDK_URL + "\n", false,
                                        () =>
                                        {
                                            try
                                            {
                                                Process.Start(config.SDK_URL);
                                            }
                                            catch (Win32Exception)
                                            {
                                                // System.ComponentModel.Win32Exception is a known exception that occurs when Firefox is default browser.  
                                                // It actually opens the browser but STILL throws this exception so we can just ignore it.  If not this exception,
                                                // then attempt to open the URL in IE instead.
                                                Process.Start(new ProcessStartInfo("IExplore.exe", config.SDK_URL));
                                            }
                                        });
                                }
                                else
                                {
                                    this._dataSource.Preference.InstalledSDK = true;
                                    save = true;
                                }
                            }
                        }

                        if (save)
                        {
                            this._dataSource.Preference.HardwareID = config.Hardware_ID;
                            result = this.SavePreference();
                        }

                        if (result)
                        {
                            //Check the miner exist or not
                            result = false;
                            var rootPath = this.GetFullPath(MinerConfig.RootPath);
                            var localPath = this.GetFullPath(config.LocalPath);
                            var minerFile = new FileInfo(System.IO.Path.Combine(localPath, config.Execute_File));
                            if (!minerFile.Exists)
                            {
                                //Make sure Miner Root Path exist
                                if (!Directory.Exists(localPath))
                                    Directory.CreateDirectory(localPath);

                                //Download miner
                                var localFile = System.IO.Path.Combine(localPath, config.SourceFile);
                                result = this.Execute(MessageType.System, "Downloading", true, () =>
                                {
                                    switch (config.Source_Protocol)
                                    {
                                        case Protocol.FTP:
                                            {
                                                var client = new FtpClient(config.SourceHost);
                                                var zipFile = client.DownloadFile(localFile, config.SourceFullName);
                                                zipFile.Close();
                                            }
                                            break;

                                        case Protocol.HTTP:
                                            {
                                                var client = new WebClient();
                                                client.DownloadFile("http://" + config.Source_Url, localFile);
                                            }
                                            break;
                                    }

                                    return true;
                                });

                                //Unzip
                                if (result)
                                {
                                    result = this.Execute(MessageType.System, "Extracting", true, () =>
                                    {
                                        ZipFile.ExtractToDirectory(localFile, localPath);

                                        //Delete file
                                        File.Delete(localFile);

                                        return true;
                                    });

                                }
                            }
                            else
                                result = true;

                            //Run Miner
                            if (result)
                            {
                                result = this.Execute(MessageType.System, "Starting Mining\n", false, () =>
                                {
                                    //Parameters
                                    var configParamates = config.Config_Parameters;
                                    if (this._dataSource.Preference.OverrideSetting)
                                        configParamates = this._dataSource.Preference.Config.Config_Parameters;

                                    var parameters = config.Base_Parameters + " " + configParamates;

                                    this.Output(MessageType.System, "Arguments: " + parameters + "\n");
                                    this._dataSource.Miner = new Process();

                                    //Environment_Variables
                                    if (config.Environment_Variables != null)
                                    {
                                        foreach (var variable in config.Environment_Variables)
                                        {
                                            this.Output(MessageType.System, "Variable: " + variable.Name + "=" + variable.Value + "\n");
                                            this._dataSource.Miner.StartInfo.EnvironmentVariables[variable.Name] = variable.Value;
                                        }
                                    }

                                    //Other
                                    this._dataSource.Miner.StartInfo.WorkingDirectory = localPath;
                                    this._dataSource.Miner.StartInfo.Arguments = parameters;
                                    this._dataSource.Miner.StartInfo.FileName = minerFile.FullName;
                                    this._dataSource.Miner.StartInfo.UseShellExecute = false;
                                    this._dataSource.Miner.StartInfo.CreateNoWindow = true;
                                    this._dataSource.Miner.StartInfo.RedirectStandardOutput = true;
                                    this._dataSource.Miner.StartInfo.RedirectStandardError = true;
                                    this._dataSource.Miner.StartInfo.RedirectStandardInput = true;
                                    this._dataSource.Miner.OutputDataReceived += (currentSender, eventArgs) =>
                                    {
                                        if (eventArgs.Data != null)
                                            this.Output(MessageType.Mining, eventArgs.Data + "\n");
                                    };
                                    this._dataSource.Miner.ErrorDataReceived += (currentSender, eventArgs) =>
                                    {
                                        if (eventArgs.Data != null)
                                            this.Output(MessageType.Mining, eventArgs.Data + "\n");
                                    };
                                    this._dataSource.Miner.EnableRaisingEvents = true;
                                    this._dataSource.Miner.Start();
                                    this._dataSource.Miner.BeginOutputReadLine();
                                    this._dataSource.Miner.BeginErrorReadLine();
                                    this._dataSource.Miner.WaitForExit();

                                    return false;
                                });

                                if (this._dataSource.Miner != null)
                                    this.ForceStop();
                            }
                            else
                                this._dataSource.Started = false;
                        }
                        else
                            this._dataSource.Started = false;
                    });

                this.RetrieveMinerSetting(this._dataSource.Preference,
                    config =>
                    {
                        var currentConfig = config;
                        //Use local config
                        if (this._dataSource.Preference.OverrideSetting)
                        {                            
                            currentConfig = this._dataSource.Preference.Config;

                            //Check the config changed
                            if (currentConfig.ID != config.ID)
                            {
                                var dialogResult = MessageBox.Show("The best setting already updated, do you want to use the setting?", "Miner Setting", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                                if (dialogResult == MessageBoxResult.Yes)
                                {
                                    currentConfig = config;
                                    this._dataSource.Preference.OverrideSetting = false;
                                }
                            }
                        }

                        //Run miner
                        runMiner(currentConfig);
                    },
                    json =>
                    {
                        this.Execute(MessageType.Error, "Server error:", false, () =>
                        {
                            var info = JsonConvert.DeserializeObject<NoMatchConfig>(json);
                            if (this._dataSource.Preference.HardwareID != info.ID)
                            {
                                this._dataSource.Preference.HardwareID = info.ID;
                                this.SavePreference();
                            }
                            this.Output(MessageType.Error, info.Message);
                            this.ForceStop();
                        });
                    });
            });
        }

        private string GetFullPath(string filename)
        {
            var root = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigiHash");
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            return System.IO.Path.Combine(root, filename);
        }

        private void IniaitalizeData(Algorithm[] algoritms, string[] gpuSeries)
        {            
            this.Output(MessageType.System, "Initialize....\n");
            this._dataSource = new DataSource() { Algorithms = algoritms, GPUSeries = gpuSeries };
            this.DataContext = this._dataSource;

            var fileName = this.GetFullPath(Preference.FileName);
            if (File.Exists(fileName))
            {
                this._dataSource.Preference = this.Execute(MessageType.System, "Reading " + Preference.FileName, true,
                    () =>
                    {
                        var json = File.ReadAllText(fileName);
                        return JsonConvert.DeserializeObject<Preference>(json);
                    });

                if (this._dataSource.Preference.ID == Guid.Empty)
                {
                    this._dataSource.Preference.ID = Guid.NewGuid();
                    this.Output(MessageType.System, "Migration preference struct\n");
                    this.SavePreference();
                }
            }

            var showDialog = this._dataSource.Preference == null || string.IsNullOrEmpty(this._dataSource.Preference.Wallet) || string.IsNullOrEmpty(this._dataSource.Preference.Algorithm);
            if (!showDialog && gpuSeries != null && gpuSeries.Length > 0)
                showDialog = !gpuSeries.Any(current => current == this._dataSource.Preference.GPUModel);

            if (showDialog)
                this.ShowPreferenceDialog();
        }

        private void TaskbarProgressState(TaskbarItemProgressState state)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.TaskbarItemInfo.ProgressState = state), null);
        }

        public void PostData(string message, bool withStatus, string url, string json, Action<string> success, Action<string> fail = null)
        {            
            this.Output(MessageType.Server, message + (withStatus ? ".........." : "\n"));
            this.TaskbarProgressState(TaskbarItemProgressState.Indeterminate);            

            var requester = new ServiceRequester();
            requester.Post(url, json,
                returnJson =>
                {
                    if (withStatus)
                        this.Output(MessageType.Server, "done\n");

                    success(returnJson);

                    this.TaskbarProgressState(TaskbarItemProgressState.None);
                },
                error =>
                {
                    this.TaskbarProgressState(TaskbarItemProgressState.Error);

                    if (withStatus)
                        this.Output(MessageType.Error, "fail\n");

                    if (fail != null)
                        fail(error);
                    else
                        this.Output(MessageType.Error, error + "\n");
                });
        }

        internal void RetrieveData(string message, bool withStatus, string url, KeyValuePair<string, string>[] parameters, Action<string> success, Action<string> fail = null)
        {
            this.Output(MessageType.Server, message + (withStatus ? ".........." : "\n"));
            this.TaskbarProgressState(TaskbarItemProgressState.Indeterminate);

            var requester = new ServiceRequester();
            requester.Get(url, parameters,
                json =>
                {
                    if (withStatus)
                        this.Output(MessageType.Server, "done\n");

                    success(json);

                    this.TaskbarProgressState(TaskbarItemProgressState.None);
                },
                error =>
                {
                    this.TaskbarProgressState(TaskbarItemProgressState.Error);

                    if (withStatus)
                        this.Output(MessageType.Error, "fail\n");

                    this.Output(MessageType.Error, error + "\n");

                    if (fail != null)
                        fail(error);
                });
        }

        private void Execute(MessageType type, string message, bool withStatus, Action action)
        {
            this.Execute(type, message, withStatus,
                () =>
                {
                    action();
                    return true;
                });
        }

        private T Execute<T>(MessageType type, string message, bool withStatus, Func<T> action)
        {
            this.Output(type, message + (withStatus ? ".........." : "\n"));
            T result = default(T);
            this.TaskbarProgressState(type == MessageType.Error ? TaskbarItemProgressState.Error : TaskbarItemProgressState.Indeterminate);
            
            try
            {
                result = action();

                if (withStatus)
                    this.Output(type, "done\n");                

                this.TaskbarProgressState(TaskbarItemProgressState.None);
            }
            catch (Exception exception)
            {
                if (withStatus)
                    this.Output(MessageType.Error, "fail\n");
                this.Output(MessageType.Error, exception.Message + "\n");

                this.TaskbarProgressState(TaskbarItemProgressState.Error);
            }

            return result;
        }

        private void Output(MessageType type, string message)
        {
            this.OutputRichTextBox.Dispatcher.BeginInvoke(new Action(() =>
                {
                    //Remove line when too more
                    var count = 1000;
                    if (this.Paragraph.Inlines.Count > count)
                    {
                        var lines = this.Paragraph.Inlines.Take(count / 10);
                        foreach (var line in lines.ToArray())
                            this.Paragraph.Inlines.Remove(line);                        
                    }

                    switch (type)
                    {
                        case MessageType.System:
                            {
                                var run = new Run(message);
                                run.Foreground = Brushes.White;
                                this.Paragraph.Inlines.Add(run);                                
                            }
                            break;

                        case MessageType.Server:
                            {
                                var run = new Run(message);
                                run.Foreground = Brushes.ForestGreen;
                                this.Paragraph.Inlines.Add(run);
                            }
                            break;

                        case MessageType.Mining:
                            {
                                var run = new Run(message);
                                run.Foreground = Brushes.Yellow;
                                this.Paragraph.Inlines.Add(run);
                            }
                            break;

                        case MessageType.Error:
                            {
                                var run = new Run(message);
                                run.Foreground = Brushes.Red;
                                this.Paragraph.Inlines.Add(run);
                            }
                            break;

                    }

                    this.OutputRichTextBox.ScrollToEnd();
                }));
        }

        public class DataSource : DataSourceBase
        {
            private Algorithm[] _algorithms;
            private string[] _gpuSeries;
            private KeyValuePair<string, decimal>[] _difficulties;
            private Process _miner;
            private bool _started;
            private Preference _preference;

            public bool Started
            {
                get { return this._started; }
                set
                {
                    this._started = value;
                    this.OnPropertyChange();
                }
            }

            public Process Miner 
            {
                get { return this._miner; }
                set
                {
                    this._miner = value;
                    this.OnPropertyChange();
                }
            }

            public Preference Preference
            {
                get { return this._preference; }
                set
                {
                    this._preference = value;
                    this.OnPropertyChange();
                }
            }

            public Algorithm[] Algorithms
            {
                get { return this._algorithms; }
                set
                {
                    this._algorithms = value;
                    this.OnPropertyChange();
                }
            }

            public string[] GPUSeries
            {
                get { return this._gpuSeries; }
                set
                {
                    this._gpuSeries = value;
                    this.OnPropertyChange();
                }
            }

            public KeyValuePair<string, decimal>[] Difficulties
            {
                get { return this._difficulties; }
                set
                {
                    this._difficulties = value;
                    this.OnPropertyChange();
                }
            }

        }

        public enum MessageType { System, Server, Mining, Error }

        public class NoMatchConfig
        {
            public int ID { get;set; }
            public string Message { get; set; }
        }
    }
}
