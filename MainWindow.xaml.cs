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
            this.IniaitalizeData();

            this.WalletButton.Click += (sender, eventArgs) => this.ShowWalletDialog();
            this.StartButton.Click += (sender, eventArgs) => this.Start();
            this.StopButton.Click += (sender, eventArgs) => this.Stop();
            this.Closing += (sender, eventArgs) =>
                {
                    this.Stop();
                    eventArgs.Cancel = this._dataSource.Miner != null;
                };
            this.Loaded += (sender, eventArgs) => this.RetrieveData("Retrieving data from server", true, "algorithms/gets", null, 
                                                                     json => this._dataSource.Algorithms = JsonConvert.DeserializeObject<Algorithm[]>(json));
            this.AlgorithmComboBox.SelectionChanged += (sender, eventArgs) =>
                {
                    if (this.AlgorithmComboBox.SelectedItem != null)
                    {
                        var algorithm = (Algorithm)this.AlgorithmComboBox.SelectedItem;
                    }
                };

        }

        private Paragraph Paragraph { get; set; }

        private void ShowWalletDialog()
        {
            var dialog = new WalletDialog();
            this._dataSource.Wallets = dialog.Show(this._dataSource.Wallets);
            this.Execute(MessageType.System, "Saving " + Wallet.FileName, true,
            () =>
            {
                var json = JsonConvert.SerializeObject(this._dataSource.Wallets);
                File.WriteAllText(this.GetFullPath(Wallet.FileName), json);
            });
        }

        private void Stop()
        {
            if (this._dataSource.Miner != null)
            {
                var result = MessageBox.Show("It will be stop the mining, are you sure?", "Terminal Mining",  MessageBoxButton.OKCancel, MessageBoxImage.Question);
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

        private void Start()
        {
            this._dataSource.Started = true;

            var algorithm = (Algorithm)this.AlgorithmComboBox.SelectedItem;
            var wallet = (Wallet)this.WalletComboBox.SelectedItem;
            //Save Profile
            var result = this.Execute(MessageType.System, "Saving profile", true, () =>
                {
                    var json = JsonConvert.SerializeObject(this._dataSource.Profile);
                    File.WriteAllText(this.GetFullPath(Profile.FileName), json);

                    return true;
                });

            if (result)
            {
                Task.Run(() =>
                {
                    var cpus = this.Execute(MessageType.System, "Getting CPU info", true, () => Hardware.GetCPUs());
                    var gpus = this.Execute(MessageType.System, "Getting GPU info", true, () => Hardware.GetGPUs());

                    var spec = new
                    {
                        algorithm = algorithm.Name,
                        username = wallet.Address,
                        platform = new
                        {
                            name = "WINDOWS_X" + (Environment.Is64BitOperatingSystem ? "64" : "32"),
                            version = string.Format("{0}.{1}", Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor)
                        },
                        cpu = cpus.Select(current => new { name = current.Model, clock = current.Clock, manufacturer = current.Manufacturer }).First(),
                        gpu = gpus.Select(current => new { name = current.Model, manufacturer = current.Manufacturer }).First(),
                    };

                    this.PostData("Analyzing hardware from server", true, "MinerConfigs/analyze", JsonConvert.SerializeObject(new { spec }),
                        json =>
                        {
                            var config = JsonConvert.DeserializeObject<MinerConfig>(json);
                            var miner = string.Format("Miner: {0}, Version: {1}", config.Miner, config.Version);
                            this.Output(MessageType.System, miner + "\n");

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
                                result = this.Execute(MessageType.System, "Download", true, () =>
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
                                    this._dataSource.Miner = new Process();
                                    this._dataSource.Miner.StartInfo.WorkingDirectory = localPath;
                                    this._dataSource.Miner.StartInfo.Arguments = config.Parameters;
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

                                    return true;
                                });

                                if (!result)
                                    this.ForceStop();
                            }
                            else
                                this._dataSource.Started = false;
                        },
                        error => this._dataSource.Started = false);
                });
            }
        }

        private string GetFullPath(string filename)
        {
            var root = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigiHash");
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            return System.IO.Path.Combine(root, filename);
        }

        private void IniaitalizeData()
        {
            this.Output(MessageType.System, string.Format("{0} Version {1}\n", this.Title, Assembly.GetEntryAssembly().GetName().Version));
            this.Output(MessageType.System, "Initialize....\n");
            this._dataSource = new DataSource();
            this.DataContext = this._dataSource;

            var fileName = this.GetFullPath(Wallet.FileName);
            if (File.Exists(fileName))
            {
                this._dataSource.Wallets = this.Execute(MessageType.System, "Reading " + Wallet.FileName, true,
                    () =>
                    {
                        var json = File.ReadAllText(fileName);
                        return JsonConvert.DeserializeObject<Wallet[]>(json);
                    });
            }
            else
                this.ShowWalletDialog();

            fileName = this.GetFullPath(Profile.FileName);
            if (File.Exists(fileName))
            {
                this._dataSource.Profile = this.Execute(MessageType.System, "Reading " + Profile.FileName, true,
                    () =>
                    {
                        var json = File.ReadAllText(fileName);
                        return JsonConvert.DeserializeObject<Profile>(json);
                    });
            }
            else
                this._dataSource.Profile = new Profile();
        }

        private void TaskbarProgressState(TaskbarItemProgressState state)
        {
            this.Dispatcher.BeginInvoke(new Action(() => this.TaskbarItemInfo.ProgressState = state), null);
        }

        private void PostData(string message, bool withStatus, string url, string json, Action<string> success, Action<string> fail = null)
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

                    this.Output(MessageType.Error, error + "\n");

                    if (fail != null)
                        fail(error);
                });
        }

        private void RetrieveData(string message, bool withStatus, string url, KeyValuePair<string, object>[] parameters, Action<string> success, Action<string> fail = null)
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
            private KeyValuePair<string, decimal>[] _difficulties;
            private Wallet[] _wallets;
            private Process _miner;
            private bool _started;
            private Profile _profile;

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

            public Profile Profile
            {
                get { return this._profile; }
                set
                {
                    this._profile = value;
                    this.OnPropertyChange();
                }
            }

            public Wallet[] Wallets
            {
                get { return this._wallets; }
                set
                {
                    this._wallets = value;
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

        public class Profile
        {
            public const string FileName = "Profile.dat";

            public string Wallet { get; set; }
            public string Algorithm { get; set; }
        }

        public class Algorithm
        {
            public string Name { get; set; }
        }

        public enum Protocol { FTP, HTTP }

        public class MinerConfig
        {
            public const string RootPath = "Miner";

            public string Miner { get; set; }
            public string Version { get; set; }
            public string Execute_File { get; set; }
            public string Parameters { get; set; }
            public Protocol Source_Protocol { get; set; }
            public string Source_Url { get; set; }
            public string SourceHost
            {
                get
                {
                    var paths = this.Source_Url.Split('/');
                    return paths.First();
                }
            }

            public string SourceFullName
            {
                get
                {
                    var paths = this.Source_Url.Split('/');
                    return string.Join("/", paths.Skip(1));
                }
            }

            public string SourceFile
            {
                get
                {
                    var paths = this.Source_Url.Split('/');
                    return paths.Last();
                }
            }

            public string LocalPath
            {
                get
                {
                    var miner = this.Miner.Replace(':', '_').Replace('\\', '_');

                    return string.Format(@"{0}\{1}\{2}", MinerConfig.RootPath, miner, this.Version);
                }
            }
        }
    }
}
