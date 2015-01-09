using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace DigiHash
{
    public static class Hardware
    {
        public static CPU[] GetCPUs()
        {
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_processor");
            var cpus = new List<CPU>();
            foreach (var mo in searcher.Get())
            {
                var cpu = new CPU()
                {
                    Model = mo["Name"].ToString().Trim(),
                    Manufacturer = mo["Manufacturer"].ToString().Trim(),
                    Clock = Convert.ToInt32(mo["MaxClockSpeed"]),
                    NumberOfCores = Convert.ToInt32(mo["NumberOfCores"]),
                    NumberOfLogicalProcessors = Convert.ToInt32(mo["NumberOfLogicalProcessors"]),
                };
                cpus.Add(cpu);
            }

            return cpus.ToArray();
        }

        public static GPU[] GetGPUs()
        {
            //General information
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
            var gpus = new List<GPU>();
            foreach (var mo in searcher.Get())
            {
                var gpu = new GPU()
                {
                    Model = mo["Name"].ToString().Trim(),
                    Manufacturer = mo["AdapterCompatibility"].ToString().Trim(),
                };
                gpus.Add(gpu);
            }

            return gpus.ToArray();
        }
    }

    public abstract class Device
    {
        public string Model { get; set; }
        public string Manufacturer { get; set; }
    }

    public class CPU:Device
    {
        public int Clock { get; set; }
        public int NumberOfCores { get; set; }
        public int NumberOfLogicalProcessors { get; set; }
    }

    public class GPU : Device
    {
    }
}
