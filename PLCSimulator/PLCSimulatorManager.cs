using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PLCSimulator
{
    public class PLCSimulatorManager
    {
        private Dictionary<string, double> addressValues;
        private object locker = new object();
        private Thread t1;
        private Thread t2;
        private volatile bool _isRunning;


        public PLCSimulatorManager()
        {
            addressValues = new Dictionary<string, double>();
            
            addressValues.Add("ADDR001", 0);
            addressValues.Add("ADDR002", 0);
            addressValues.Add("ADDR003", 0);
            addressValues.Add("ADDR004", 0);

            addressValues.Add("ADDR005", 0);
            addressValues.Add("ADDR006", 0);
            addressValues.Add("ADDR007", 0);
            addressValues.Add("ADDR008", 0);

            addressValues.Add("ADDR009", 0);
            addressValues.Add("ADDR011", 0);
            addressValues.Add("ADDR012", 0);

            addressValues.Add("ADDR010", 0);
            addressValues.Add("ADDR013", 0);
            addressValues.Add("ADDR014", 0);
        }

        public void StartPLCSimulator()
        {
            _isRunning = true;
            t1 = new Thread(GeneratingAnalogInputs) { IsBackground = true };
            t1.Start();

            t2 = new Thread(GeneratingDigitalInputs) { IsBackground = true };
            t2.Start();
        }

        private void GeneratingAnalogInputs()
        {
            while (_isRunning)
            {
                Thread.Sleep(100);

                lock (locker)
                {
                    addressValues["ADDR001"] = 100 * Math.Sin((double)DateTime.Now.Second / 60 * Math.PI);
                    addressValues["ADDR002"] = 100 * DateTime.Now.Second / 60;
                    addressValues["ADDR003"] = 50 * Math.Cos((double)DateTime.Now.Second / 60 * Math.PI);
                    addressValues["ADDR004"] = RandomNumberBetween(0, 50);
                }
            }
        }

        private void GeneratingDigitalInputs()
        {
            while (_isRunning)
            {
                Thread.Sleep(1000);

                lock (locker)
                {
                    addressValues["ADDR009"] = addressValues["ADDR009"] == 0 ? 1 : 0;

                    addressValues["ADDR011"] = DateTime.Now.Second % 3 == 0 ? 1 : 0;
                    addressValues["ADDR012"] = RandomNumberBetween(0, 1) > 0.5 ? 1 : 0;
                }
            }
        }

        public double GetAnalogValue(string address)
        {

            if (addressValues.ContainsKey(address))
            {
                return addressValues[address];
            }
            else
            {
                return -1;
            }
        }
        public double GetDigitalValue(string address)
        {
            if (addressValues.ContainsKey(address))
            {
                return addressValues[address];
            }
            else
            {
                return -1;
            }
        }

        public void SetAnalogValue(string address, double value)
        {
            if (addressValues.ContainsKey(address))
            {
                addressValues[address] = value;
                System.Diagnostics.Debug.WriteLine($"SetAnalogValue - Address: {address}, Value: {value}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SetAnalogValue - Address not found: {address}");
            }
        }

        public void SetDigitalValue(string address, double value)
        {
            if (addressValues.ContainsKey(address))
            {
                addressValues[address] = value;
                System.Diagnostics.Debug.WriteLine($"SetDigitalValue - Address: {address}, Value: {value}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"SetDigitalValue - Address not found: {address}");
            }
        }

        private static double RandomNumberBetween(double minValue, double maxValue)
        {
            Random random = new Random();
            var next = random.NextDouble();

            return minValue + (next * (maxValue - minValue));
        }

        public void Stop()
        {
            _isRunning = false;
        }
    }
}