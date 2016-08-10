using System;
using System.Linq;
using System.Text;
using System.Management;
using System.Collections.Generic;
using System.Collections.Specialized;
using DivaliaMonitor.SMART;

namespace DivaliaMonitor.Hardware
{
    class HDD
    {
        public static List<Dictionary<string, string>> GetSMART()
        {
            var list = new List<Dictionary<string, string>>();
            try
            {
                var dicDrives = Reader.read();
                foreach (var drive in dicDrives)
                {
                    var status = new Dictionary<string, string>();
                    status["serial"] = drive.Value.Serial;
                    status["model"] = drive.Value.Model;
                    status["type"] = drive.Value.Type;
                    status["status"] = (drive.Value.IsOK) ? "OK" : "BAD";
                    foreach (var attr in drive.Value.Attributes)
                    {
                        if (attr.Value.HasData)
                        {
                            status["attr." + attr.Value.Attribute + ".name"] = attr.Value.Attribute;
                            status["attr." + attr.Value.Attribute + ".data"] = attr.Value.Data.ToString();
                            status["attr." + attr.Value.Attribute + ".current"] = attr.Value.Current.ToString();
                            status["attr." + attr.Value.Attribute + ".worst"] = attr.Value.Worst.ToString();
                            status["attr." + attr.Value.Attribute + ".threshold"] = attr.Value.Threshold.ToString();
                            status["attr." + attr.Value.Attribute + ".status"] = (attr.Value.IsOK) ? "OK" : "BAD";
                        }
                    }
                    list.Add(status);
                }
                return list;
            }
            catch (ManagementException e)
            {
                return list;
            }
        }
    }
}