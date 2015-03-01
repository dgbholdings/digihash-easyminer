using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace DigiHash
{
    /// <summary>
    /// Interaction logic for EnvironmentVariableDialog.xaml
    /// </summary>
    public partial class EnvironmentVariableDialog : Window
    {
        private bool _isNew;
        private bool _isEdit;
        private DataSource _dataSource;

        public EnvironmentVariableDialog()
        {
            InitializeComponent();
            this._dataSource = new DataSource();
            this.DataContext = this._dataSource;

            this.Closing += (sender, eventArgs) =>
                {
                    if (this._isEdit)
                    {
                        var result = MessageBox.Show("Do you want exit without save?", "Exit", MessageBoxButton.YesNo, MessageBoxImage.Information);
                        eventArgs.Cancel = result == MessageBoxResult.No;
                    }
                };
            this.CloseButton.Click += (sender, eventArgs) => this.Close();
            var descriptor = DependencyPropertyDescriptor.FromProperty(TextBox.TextProperty, typeof(TextBox));
            descriptor.AddValueChanged(this.NameTextBox, (sender, eventArgs) => this._isEdit = true);
            descriptor.AddValueChanged(this.ValueTextBox, (sender, eventArgs) => this._isEdit = true);
            this.AddButton.Click += (sender, eventArgs) =>
                {
                    this._isNew = true;
                    this._isEdit = true;
                    this._dataSource.Parameter = new Parameter();
                    this.NameTextBox.Focus();
                    
                };
            this.RemoveButton.Click += (sender, eventArgs) =>
            {
                if (this.ParameterListBox.SelectedItem != null)
                    this._dataSource.Parameters.Remove((Parameter)this.ParameterListBox.SelectedItem);
            };
            this.SaveButton.Click += (sender, eventArgs) =>
                {
                    //Validate
                    var isValid = true;
                    if (string.IsNullOrEmpty(this._dataSource.Parameter.Name))
                    {
                        isValid = false;                        
                        MessageBox.Show("Name cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else if (string.IsNullOrEmpty(this._dataSource.Parameter.Value))
                    {
                        isValid = false;
                        MessageBox.Show("Value cannot be empty", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    else //Check duplicate
                    {
                        var parameter = this._isNew ? null : (Parameter)this.ParameterListBox.SelectedItem;
                        var exist = (from current in this._dataSource.Parameters
                                     where string.Compare(current.Name, this._dataSource.Parameter.Name, true) == 0
                                     where current != parameter
                                     select current).Any();
                        if (exist)
                        {
                            isValid = false;
                            MessageBox.Show("Parameter name already exist", "Validation", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                    if (isValid)
                    {
                        this._isEdit = false;
                        if (this._isNew)
                        {
                            this._isNew = false;
                            this._dataSource.Parameters.Add(this._dataSource.Parameter);
                        }
                        else
                        {
                            var Parameter = (Parameter)this.ParameterListBox.SelectedItem;
                            Parameter.Name = this._dataSource.Parameter.Name;
                            Parameter.Value = this._dataSource.Parameter.Value;
                        }
                    }
                };
            this.ParameterListBox.SelectionChanged += (sender, eventArgs) =>
                {
                    if (this.ParameterListBox.SelectedItem != null)
                    {
                        var parameter = (Parameter)this.ParameterListBox.SelectedItem;
                        this._dataSource.Parameter = (Parameter)parameter.Clone();
                        this._isNew = false;
                        this._isEdit = false;
                    }                        
                };
        }

        public Parameter[] Show(Parameter[] parameters)
        {
            if (parameters != null)
            {
                foreach (var Parameter in parameters)
                    this._dataSource.Parameters.Add(Parameter);
            }

            this.ShowDialog();

            return this._dataSource.Parameters.ToArray();
        }

        public class DataSource : DataSourceBase
        {
            private ObservableCollection<Parameter> _parameters;
            private Parameter _parameter;

            public DataSource()
            {
                this._parameters = new ObservableCollection<Parameter>();
            }

            public Parameter Parameter
            {
                get { return this._parameter; }
                set
                {
                    this._parameter = value;
                    this.OnPropertyChange();                    
                }
            }

            public ObservableCollection<Parameter> Parameters
            {
                get { return this._parameters; }
                set
                {
                    this._parameters = value;
                    this.OnPropertyChange();
                }
            }
        }
    }
}
