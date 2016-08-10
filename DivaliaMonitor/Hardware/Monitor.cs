using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using OpenHardwareMonitor;
using OpenHardwareMonitor.Hardware;

namespace DivaliaMonitor.Hardware
{
    class Monitor
    {
        protected Computer computer = new Computer();
        private static Monitor monitor = new Monitor();
        private static UpdateVisitor updateVisitor = new UpdateVisitor();

        public static List<Dictionary<string, string>> getData()
        {
            return monitor.getUpdatedData();
        }

        public Monitor()
        {
            this.computer.MainboardEnabled = true;
            this.computer.FanControllerEnabled = true;
            this.computer.CPUEnabled = true;
            this.computer.GPUEnabled = true;
            this.computer.HDDEnabled = true;
            this.computer.RAMEnabled = true;
            this.computer.Open();
            this.computer.Close();
            this.computer.Open();
        }

        public List<Dictionary<string, string>> getUpdatedData()
        {
            this.computer.Accept(updateVisitor);

            var list = new List<Dictionary<string, string>>();
            foreach (IHardware device in computer.Hardware)
            {
                foreach (ISensor sensor in device.Sensors)
                {
                    var status = new Dictionary<string, string>();
                    status["device.name"] = device.Name.ToString();
                    status["device.identifier"] = device.Identifier.ToString();
                    status["sensor.name"] = sensor.Name.ToString();
                    status["sensor.identifier"] = sensor.Identifier.ToString();
                    status["sensor.type"] = sensor.SensorType.ToString();
                    status["sensor.value"] = sensor.Value.ToString();
                    list.Add(status);
                }
                foreach (IHardware subDevice in device.SubHardware)
                {
                    foreach (ISensor sensor in subDevice.Sensors)
                    {
                        var status = new Dictionary<string, string>();
                        status["device.parent.name"] = device.Name.ToString();
                        status["device.parent.identifier"] = device.Identifier.ToString();
                        status["device.name"] = subDevice.Name.ToString();
                        status["device.identifier"] = subDevice.Identifier.ToString();
                        status["sensor.name"] = sensor.Name.ToString();
                        status["sensor.identifier"] = sensor.Identifier.ToString();
                        status["sensor.type"] = sensor.SensorType.ToString();
                        status["sensor.value"] = sensor.Value.ToString();
                        list.Add(status);
                    }
                }
            }
            return list;
        }
    }

    public class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware)
                subHardware.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }

        public void VisitParameter(IParameter parameter) { }
    }
}
