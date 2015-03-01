using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DigiHash
{
    /// <summary>
    /// Interaction logic for WalletDialog.xaml
    /// </summary>
    public partial class PreferenceDialog : Window
    {
        private DataSource _dataSource;
        private bool _isValid;
        private MainWindow _window;

        public PreferenceDialog()
        {
            InitializeComponent();

            this.SaveButton.Click += (sender, eventArgs) =>
                {
                    //Validate
                    var isValid = true;
                    if (string.IsNullOrEmpty(this._dataSource.Preference.Wallet))
                    {
                        isValid = false;
                        MessageBox.Show("Wallet cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (string.IsNullOrEmpty(this._dataSource.Preference.Algorithm))
                    {
                        isValid = false;
                        MessageBox.Show("Algorithm cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (this._dataSource.Preference.OverrideSetting && string.IsNullOrEmpty(this._dataSource.Preference.Config.Config_Parameters))
                    {
                        isValid = false;
                        MessageBox.Show("Miner parameter cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (isValid)
                    {
                        this._isValid = isValid;
                        this.Close();
                    }
                };
            this.RetrieveSettingButton.Click += (sender, eventArgs) => this.RetrieveMinerSetting();
            this.EnvironmentVariableButton.Click += (sender, eventArgs) =>
                {
                    var dialog = new EnvironmentVariableDialog();
                    this._dataSource.Preference.Config.Environment_Variables = dialog.Show(this._dataSource.Preference.Config.Environment_Variables);
                };
            this.SubmitConfigButton.Click += (sender, eventArgs) =>
            {
                var cursor = this.Cursor;
                this.Cursor = Cursors.Wait;
                this.IsEnabled = false;
                var suggestion = new
                {
                    submitter_uuid = this._dataSource.Preference.ID,
                    hardware_id = this._dataSource.Preference.HardwareID,
                    miner_config_id = this._dataSource.Preference.Config.ID,
                    parameters = this._dataSource.Preference.Config.Config_Parameters,
                    environment_variables = this._dataSource.Preference.Config.Environment_Variables == null ||
                                            this._dataSource.Preference.Config.Environment_Variables.Length == 0 ? null :
                                            string.Join(",", (from current in this._dataSource.Preference.Config.Environment_Variables
                                                               select current.Name + "=" + current.Value).ToArray())
                };

                this._window.PostData("Submitting configuration", true, "config_suggestions", JsonConvert.SerializeObject(new { config_suggestion = suggestion }),
                    json =>
                    {
                        MessageBox.Show("Thank you for you help!", "Submit Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Cursor = cursor;
                        this.IsEnabled = true;
                    },
                    error =>
                    {
                        MessageBox.Show(error, "Submit Fail", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        this.Cursor = cursor;
                        this.IsEnabled = true;
                    });
            };
            this.AlgorithmComboBox.SelectionChanged += (sender, eventArgs) =>
                {
                    if (this._dataSource.Preference.OverrideSetting && this._dataSource.Preference.Algorithm != this._dataSource.OriginalAlgorithm)
                    {
                        var result = MessageBox.Show("You need to reload configuration and will lost their existing overrides", "Configuration Changed", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        if (result == MessageBoxResult.Yes)
                        {
                            this.RetrieveMinerSetting();
                            this._dataSource.OriginalAlgorithm = this._dataSource.Preference.Algorithm;
                        }
                        else
                            this._dataSource.Preference.Algorithm = this._dataSource.OriginalAlgorithm;
                    }
                };

            this.GPUComboBox.SelectionChanged += (sender, eventArgs) =>
            {
                if (this._dataSource.Preference.OverrideSetting && this._dataSource.Preference.GPUModel != this._dataSource.OriginalGPUModel)
                {
                    var result = MessageBox.Show("You need to reload configuration and will lost their existing overrides", "Configuration Changed", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        this.RetrieveMinerSetting();
                        this._dataSource.OriginalGPUModel = this._dataSource.Preference.GPUModel;
                    }
                    else
                        this._dataSource.Preference.GPUModel = this._dataSource.OriginalGPUModel;
                }
            };
        }

        private void RetrieveMinerSetting()
        {
            var isValid = true;
            if (string.IsNullOrEmpty(this._dataSource.Preference.Wallet))
            {
                isValid = false;
                MessageBox.Show("Wallet cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else if (string.IsNullOrEmpty(this._dataSource.Preference.Algorithm))
            {
                isValid = false;
                MessageBox.Show("Algorithm cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (isValid)
            {
                var cursor = this.Cursor;
                this.Cursor = Cursors.Wait;
                this.IsEnabled = false;

                this._window.RetrieveMinerSetting(this._dataSource.Preference,
                    config =>
                    {
                        this._dataSource.Preference.Config = config;
                        this._dataSource.Preference.HardwareID = config.Hardware_ID;
                        this.Cursor = cursor;
                        this.IsEnabled = true;                    
                    },
                    error =>
                    {
                        MessageBox.Show(error, "Retrive Fail", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                        this.Cursor = cursor;
                        this.IsEnabled = true;
                    });
            }

        }

        public Preference Show(MainWindow window, MainWindow.DataSource dataSource)
        {
            this._window = window;
            this._dataSource = new DataSource()
            {
                Algorithms = dataSource.Algorithms,
                GPUSeries = dataSource.GPUSeries,
                Preference = dataSource.Preference == null ? new Preference() { ID = Guid.NewGuid() } : (Preference)dataSource.Preference.Clone(),
                OriginalAlgorithm = dataSource.Preference.Algorithm,
                OriginalGPUModel = dataSource.Preference.GPUModel
            };

            if (this._dataSource.Preference.GPUModel == null)
                this._dataSource.Preference.GPUModel = window.GPUs.First().Model;

            if (this._dataSource.GPUSeries == null || !this._dataSource.GPUSeries.Any())
                this._dataSource.GPUSeries = new string[] { this._dataSource.Preference.GPUModel };

            this.DataContext = this._dataSource;

            this.ShowDialog();

            return this._isValid ? this._dataSource.Preference : null;
        }

        public class DataSource : DataSourceBase
        {
            private Algorithm[] _algorithms;
            private string[] _gpuSeries;
            private Preference _preference;

            public string OriginalAlgorithm { get; set; }
            public string OriginalGPUModel { get; set; }

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
        }
    }
}
