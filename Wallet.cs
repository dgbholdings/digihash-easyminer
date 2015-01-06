using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DigiHash
{
    public class Wallet : ICloneable
    {
        public const string FileName = "Wallet.dat";

        public string Name { get; set; }
        public string Address { get; set; }

        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }
}
