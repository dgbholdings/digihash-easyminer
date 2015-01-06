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
    public partial class WalletDialog : Window
    {
        private bool _isNew;
        private DataSource _dataSource;

        public WalletDialog()
        {
            InitializeComponent();
            this._dataSource = new DataSource();
            this.DataContext = this._dataSource;

            this.CloseButton.Click += (sender, eventArgs) => this.Close();
            this.AddButton.Click += (sender, eventArgs) =>
                {
                    this._isNew = true;
                    this._dataSource.Wallet = new Wallet();
                    this.NameTextBox.Focus();
                    
                };
            this.RemoveButton.Click += (sender, eventArgs) =>
            {
                if (this.WalletListBox.SelectedItem != null)
                    this._dataSource.Wallets.Remove((Wallet)this.WalletListBox.SelectedItem);
            };
            this.SaveButton.Click += (sender, eventArgs) =>
                {
                    //Validate
                    var isValid = true;
                    if (string.IsNullOrEmpty(this._dataSource.Wallet.Name))
                    {
                        isValid = false;                        
                        MessageBox.Show("Name cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (string.IsNullOrEmpty(this._dataSource.Wallet.Address))
                    {
                        isValid = false;
                        MessageBox.Show("Address cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (this._isNew && this._dataSource.Wallets.Any(wallet => string.Compare(wallet.Name, this._dataSource.Wallet.Name, true) == 0))
                    {                        
                        isValid = false;
                        MessageBox.Show("Wallet name already exist", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    if (isValid)
                    {
                        if (this._isNew)
                        {
                            this._isNew = false;
                            this._dataSource.Wallets.Add(this._dataSource.Wallet);
                        }
                        else
                        {
                            var wallet = (Wallet)this.WalletListBox.SelectedItem;
                            wallet.Name = this._dataSource.Wallet.Name;
                            wallet.Address = this._dataSource.Wallet.Address;
                        }
                    }
                };
            this.WalletListBox.SelectionChanged += (sender, eventArgs) =>
                {
                    if (this.WalletListBox.SelectedItem != null)
                    {
                        var wallet = (Wallet)this.WalletListBox.SelectedItem;
                        this._dataSource.Wallet = (Wallet)wallet.Clone();
                    }                        
                };
        }

        public Wallet[] Show(Wallet[] wallets)
        {
            if (wallets != null)
            {
                foreach (var wallet in wallets)
                    this._dataSource.Wallets.Add(wallet);
            }

            this.ShowDialog();

            return this._dataSource.Wallets.ToArray();
        }

        public class DataSource : DataSourceBase
        {
            private ObservableCollection<Wallet> _wallets;
            private Wallet _wallet;

            public DataSource()
            {
                this.Wallets = new ObservableCollection<Wallet>();
            }

            public Wallet Wallet
            {
                get { return this._wallet; }
                set
                {
                    this._wallet = value;
                    this.OnPropertyChange();                    
                }
            }

            public ObservableCollection<Wallet> Wallets
            {
                get { return this._wallets; }
                set
                {
                    this._wallets = value;
                    this.OnPropertyChange();
                }
            }
        }
    }
}
