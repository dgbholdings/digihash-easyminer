using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiHash
{
    public class Preference : DataSourceBase, ICloneable
    {
        public const string FileName = "Preference.dat";

        private string _wallet;
        private string _algorithm;
        private bool _installedSDK;
        private string _gpuModel;
        private MinerConfig _config;
        private bool _overrideSetting;
        private int _hardwareID;

        public string Wallet
        {
            get { return this._wallet; }
            set
            {
                this._wallet = value;
                this.OnPropertyChange();
            }
        }
        public string Algorithm
        {
            get { return this._algorithm; }
            set
            {
                this._algorithm = value;
                this.OnPropertyChange();
            }
        }
        public bool InstalledSDK
        {
            get { return this._installedSDK; }
            set
            {
                this._installedSDK = value;
                this.OnPropertyChange();
            }
        }
        public int HardwareID
        {
            get { return this._hardwareID; }
            set
            {
                this._hardwareID = value;
                this.OnPropertyChange();
            }
        }
        public string GPUModel
        {
            get { return this._gpuModel; }
            set
            {
                this._gpuModel = value;
                this.OnPropertyChange();
            }
        }
        public bool OverrideSetting
        {
            get { return this._overrideSetting; }
            set
            {
                this._overrideSetting = value;
                this.OnPropertyChange();
            }
        }
        public MinerConfig Config
        {
            get { return this._config; }
            set
            {
                this._config = value;
                this.OnPropertyChange();
            }
        }

        public bool IsValid
        {
            get { return !string.IsNullOrEmpty(this.Wallet) && !string.IsNullOrEmpty(this.Algorithm); }
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }


    public class Algorithm
    {
        public string Name { get; set; }
    }

    public enum Protocol { FTP, HTTP }
    public enum MinerDevice { CPU, GPU }

    public class MinerConfig
    {
        public const string RootPath = "Miner";

        public int ID { get; set; }
        public int Hardware_ID { get; set; }
        public string Miner { get; set; }
        public string Version { get; set; }
        public string Execute_File { get; set; }
        public KeyValuePair<string, string>[] Environment_Variables { get; set; }
        public string Base_Parameters { get; set; }
        public string Config_Parameters { get; set; }
        public MinerDevice Device { get; set; }
        public string SDK_URL { get; set; }
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

        public string Summary
        {
            get
            {
                return string.Format("Config: {0}, Miner: {1}, Version: {2}", this.ID, this.Miner, this.Version);
            }
        }
    }
}
