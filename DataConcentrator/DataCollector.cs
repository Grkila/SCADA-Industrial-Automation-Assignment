using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
namespace DataConcentrator
{
    public class ValuesFromPLC
    {
        public Dictionary<string, int> currentValues { get; set; }
        public TagType type { get; set; }
        public ValuesFromPLC(TagType type)
        {
            this.type = type;
            currentValues = new Dictionary<string, int>();
        }
    }
    public class DataCollector
    {
        
        private Thread scanCycleThread;
        private volatile bool isRunning = false;
        private static readonly object locker = new object();
        private PLCSimulator.PLCSimulatorManager plcSimulator;

        private readonly int scanCycleMs;

        public DataCollector()
        {
            tags = new List<Tag>();
            scanCycleMs = 1000; // Default scan cycle 1 second
            ValuesFromPLC currentValuesDI = new ValuesFromPLC(TagType.DI);
            ValuesFromPLC currentValuesDO = new ValuesFromPLC(TagType.DO);
            ValuesFromPLC currentValuesAI = new ValuesFromPLC(TagType.AI);
            ValuesFromPLC currentValuesAO = new ValuesFromPLC(TagType.AO);
        }

        public void Start(PLCSimulator.PLCSimulatorManager plcSimulator)
        {
            if(isRunning)
                return;
            this.plcSimulator = plcSimulator ?? throw new ArgumentNullException(nameof(plcSimulator));
            // Initialize currentValues with addresses from plcSimulator
            plcSimulator.StartPLCSimulator();
            foreach (var (address, type) in plcSimulator.GetAllAddresses())
            {
                switch (type)
                {
                    case TagType.DI:
                        currentValuesDI.currentValues[address] = 0;
                        break;
                    case TagType.DO:
                        currentValuesDO.currentValues[address] = 0;
                        break;
                    case TagType.AI:
                        currentValuesAI.currentValues[address] = 0;
                        break;
                    case TagType.AO:
                        currentValuesAO.currentValues[address] = 0;
                        break;
                }
            }

            isRunning = true;
            scanCycleThread = new Thread(ScanCycle);
            scanCycleThread.Start();
        }
        public void Stop()
        {
            if(!isRunning)
                return;
            isRunning = false;
            scanCycleThread.Join();
        }
        private void ScanCycle()
        {
            while (isRunning)
            {
                lock (locker)
                {
                    foreach (var adress in currentValues.Values)
                    {

                    }
                }
                Thread.Sleep(scanCycleMs);
            }
        }
    }
}
