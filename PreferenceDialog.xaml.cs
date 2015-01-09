using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

                    if (isValid)
                    {
                        this._isValid = isValid;
                        this.Close();
                    }
                };
        }

        public Preference Show(Preference preference, Algorithm[] algorithms)
        {
            this._dataSource = new DataSource()
            {
                Algorithms = algorithms,
                Preference = preference == null? new Preference(): (Preference) preference.Clone()
            };
            this.DataContext = this._dataSource;

            this.ShowDialog();

            return this._isValid ? this._dataSource.Preference : null;
        }

        public class DataSource : DataSourceBase
        {
            private Algorithm[] _algorithms;
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
        }
    }
}
