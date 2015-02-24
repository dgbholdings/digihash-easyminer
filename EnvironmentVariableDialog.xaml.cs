using System;
using System.Collections.Generic;
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
        public EnvironmentVariableDialog()
        {
            InitializeComponent();
            
            this.Closing += (sender, eventArgs) =>
                {
                    foreach(var parameter in this.Parameters)
                    {
                        foreach(ValidationRuleBase rule in this.MainDataGrid.RowValidationRules)
                        {
                            var result = rule.Validate(parameter);
                            if (!result.IsValid)
                            {
                                MessageBox.Show(result.ErrorContent.ToString(), "Validation Fail", MessageBoxButton.OK, MessageBoxImage.Error);
                                eventArgs.Cancel = true;
                                break;
                            }
                        }

                        if (eventArgs.Cancel)
                            break;
                    }
                };
        }

        private List<Parameter> Parameters { get; set; }

        public Parameter[] Show(Parameter[] parameters)
        {
            //Perpare data source
            this.Parameters = new List<Parameter>();
            this.Parameters.AddRange(parameters);
            //Binding
            this.DataContext = new { Parameters = this.Parameters };

            //Initialize rule
            var rule = (DuplicateValidationRule)this.MainDataGrid.RowValidationRules.Single(current => current.GetType() == typeof(DuplicateValidationRule));
            rule.Parameters = this.Parameters;

            //Show dialog
            this.ShowDialog();

            return this.Parameters.ToArray();
        }
    }

    public abstract class ValidationRuleBase : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            return this.Validate((Parameter)(value as BindingGroup).Items[0]);
        }

        public abstract ValidationResult Validate(Parameter parameter);
    }

    public class AllEmptyValidationRule : ValidationRuleBase
    {
        public override ValidationResult Validate(Parameter parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter.Name) || string.IsNullOrWhiteSpace(parameter.Value))
                return new ValidationResult(false, "Name or value cannot be empty");
            else
                return ValidationResult.ValidResult;
        }
    }

    public class DuplicateValidationRule : ValidationRuleBase
    {
        public List<Parameter> Parameters { get; set; }

        public override ValidationResult Validate(Parameter parameter)
        {
            var count = (from current in this.Parameters
                         where string.Compare(current.Name, parameter.Name, true) == 0
                         select current).Count();

            if (count > 1)
                return new ValidationResult(false, "Duplicate name");
            else
                return ValidationResult.ValidResult;
        }
    }
}
