using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace PLCSimulator
{
    /// <summary>
    /// PLC Simulator
    /// 
    /// 4 x ANALOG INPUT : ADDR001 - ADDR004
    /// 4 x ANALOG OUTPUT: ADDR005 - ADDR008
    /// 1 x DIGITAL INPUT: ADDR009
    /// 1 x DIGITAL OUTPUT: ADDR010
    /// </summary>
    public class PLCSimulatorManager
    {
        public class TagValue
        {
            public double Value { get; set; }
            public TagType Type { get; }

            public TagValue(double value, TagType type)
            {
                Value = value;
                Type = type;
            }
        }

        private Dictionary<string, TagValue> addressValues;
        private object locker = new object();
        private Thread t1;
        private Thread t2;

        public PLCSimulatorManager()
        {
  
            addressValues = new Dictionary<string, (double value, TagType type)>();

            // AI
            addressValues.Add("ADDR001", new TagValue(0, TagType.AI));
            addressValues.Add("ADDR002", new TagValue(0, TagType.AI));
            addressValues.Add("ADDR003", new TagValue(0, TagType.AI));
            addressValues.Add("ADDR004", new TagValue(0, TagType.AI));

            // AO
            addressValues.Add("ADDR005", new TagValue(0, TagType.AO));
            addressValues.Add("ADDR006", new TagValue(0, TagType.AO));
            addressValues.Add("ADDR007", new TagValue(0, TagType.AO));
            addressValues.Add("ADDR008", new TagValue(0, TagType.AO));

            // DI
            // TODO: dodati jos nekoliko adresa za DI (recimo po 4 za svaku vrstu tagova)
            addressValues.Add("ADDR009", new TagValue(0, TagType.DI));
            //generate 4 aditional DI addresses
            addressValues.Add("ADDR011", new TagValue(0, TagType.DI));
            addressValues.Add("ADDR012", new TagValue(0, TagType.DI));
            addressValues.Add("ADDR013", new TagValue(0, TagType.DI));

            // DO
            // TODO: dodati jos nekoliko adresa za DI (recimo po 4 za svaku vrstu tagova)
            addressValues.Add("ADDR010", new TagValue(0, TagType.DO));
            //generate 4 aditional DO addresses
            addressValues.Add("ADDR014", new TagValue(0, TagType.DO));
            addressValues.Add("ADDR015", new TagValue(0, TagType.DO));
            addressValues.Add("ADDR016", new TagValue(0, TagType.DO));
            addressValues.Add("ADDR017", new TagValue(0, TagType.DO));
        }

        public void StartPLCSimulator()
        {
            t1 = new Thread(GeneratingAnalogInputs);
            t1.Start();

            t2 = new Thread(GeneratingDigitalInputs);
            t2.Start();
        }

        private void GeneratingAnalogInputs()
        {
            while (true)
            {
                Thread.Sleep(100);

                lock (locker)
                {
                    //can i modify only values of AI addresses?

                    addressValues["ADDR001"].Value = 100 * Math.Sin((double)DateTime.Now.Second / 60 * Math.PI); //SINE
                    addressValues["ADDR002"].Value = 100 * DateTime.Now.Second / 60; //RAMP
                    addressValues["ADDR003"].Value = 50 * Math.Cos((double)DateTime.Now.Second / 60 * Math.PI); //COS
                    addressValues["ADDR004"].Value = RandomNumberBetween(0, 50);  //rand
                }
            }
        }

        private void GeneratingDigitalInputs()
        {
            while (true)
            {
                Thread.Sleep(1000);

                lock (locker)
                {
                    if (addressValues["ADDR009"].Value == 0)
                    {
                        addressValues["ADDR009"].Value = 1;
                    }
                    else
                    {
                        addressValues["ADDR009"].Value = 0;
                    }
                }
            }
        }

        public double GetAnalogValue(string address)
        {

            if (addressValues.ContainsKey(address))
            {
                return addressValues[address].Value;
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
                addressValues[address].Value = value;
            }
        }

        public void SetDigitalValue(string address, double value)
        {
            if (addressValues.ContainsKey(address))
            {
                addressValues[address].Value = value;
            }
        }

        private static double RandomNumberBetween(double minValue, double maxValue)
        {
            Random random = new Random();
            var next = random.NextDouble();

            return minValue + (next * (maxValue - minValue));
        }

        public void Abort()
        {
            t1.Abort();
            t2.Abort();
        }
        public List<(string, TagType)> GetAllAddresses()
        {
            return addressValues.Select(kvp => (kvp.Key, kvp.Value.Type)).ToList();
        }
    }
