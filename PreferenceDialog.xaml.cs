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
            this.RetrieveSettingButton.Click += (sender, eventArgs) =>
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
                        this._window.RetrieveMinerSetting(this._dataSource.Preference,
                            config =>
                            {
                                this._dataSource.Preference.Config = config;
                                this._dataSource.Preference.HardwareID = config.Hardware_ID;
                            });
                    }
                };
        }

        public Preference Show(MainWindow window, MainWindow.DataSource dataSource)
        {
            this._window = window;
            this._dataSource = new DataSource()
            {
                Algorithms = dataSource.Algorithms,
                GPUSeries = dataSource.GPUSeries,
                Preference = dataSource.Preference == null ? new Preference() : (Preference)dataSource.Preference.Clone()
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
