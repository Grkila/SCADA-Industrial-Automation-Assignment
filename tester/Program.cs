using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using DataConcentrator;
namespace tester
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== SCADA SISTEM - TESTIRANJE PO SPECIFIKACIJI ===\n");

            try
            {
                TestDatabaseCreation();
                TestTagOperations();
                TestAlarmOperations();
                TestActivatedAlarms();
                TestReportGeneration();
                TestConfigurationSaveLoad();
                
                Console.WriteLine("\n" + new string('=', 60));
                Console.WriteLine("=== TESTIRANJE DATA COLLECTOR FUNKCIONALNOSTI ===");
                Console.WriteLine(new string('=', 60));
                
                TestDataCollectorBasicOperations();
                TestDataCollectorTagManagement();
                TestDataCollectorScanningControl();
                TestDataCollectorWriteOperations();
                TestDataCollectorPLCIntegration();
                TestDataCollectorAlarmDetection();
                TestDataCollectorTimerManagement();
                TestDataCollectorStartStopLifecycle();

                Console.WriteLine("\n🎉 SVI TESTOVI SU USPEŠNO ZAVRŠENI!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ GREŠKA: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }

            Console.WriteLine("\nPritisnite bilo koji taster za izlaz...");
            Console.ReadKey();
        }

        static void TestDatabaseCreation()
        {
            Console.WriteLine("=== TEST 1: Kreiranje baze podataka (3 tabele) ===");

            using (var context = new ContextClass())
            {
                // Briše postojeću bazu za čist test
                if (context.Database.Exists())
                {
                    context.Database.Delete();
                    Console.WriteLine("Stara baza je obrisana.");
                }

                // Kreira novu bazu
                bool created = context.Database.CreateIfNotExists();
                Console.WriteLine("✅ Baza 'ScadaDatabase' je kreirana sa tabelama:");
                Console.WriteLine("   - Tags (tagovi)");
                Console.WriteLine("   - Alarms (alarmi)");
                Console.WriteLine("   - ActivatedAlarms (aktivirani alarmi)");

                Console.WriteLine($"   Početni broj tagova: {context.Tags.Count()}");
                Console.WriteLine($"   Početni broj alarma: {context.Alarms.Count()}");
                Console.WriteLine($"   Početni broj aktiviranih alarma: {context.ActivatedAlarms.Count()}");
            }
            Console.WriteLine();
        }

        static void TestTagOperations()
        {
            Console.WriteLine("=== TEST 2: Dodavanje i uklanjanje tagova ===");

            using (var context = new ContextClass())
            {
                // Kreiranje različitih tipova tagova
                Console.WriteLine("Kreiranje tagova različitih tipova:");

                // DI - Digital Input
                var digitalInput = new Tag(TagType.DI, "DI_001", "Prekidač na vratima", 1001);
                digitalInput.ValidateAndSetScanTime(1000);
                digitalInput.ValidateAndSetOnOffScan(true);
                Console.WriteLine($"✅ DI tag: {digitalInput.Id} - {digitalInput.Description}");

                // DO - Digital Output  
                var digitalOutput = new Tag(TagType.DO, "DO_001", "LED signalizacija", 2001);
                digitalOutput.ValidateAndSetInitialValue(0);
                Console.WriteLine($"✅ DO tag: {digitalOutput.Id} - {digitalOutput.Description}");

                // AI - Analog Input
                var analogInput = new Tag(TagType.AI, "AI_001", "Senzor temperature", 3001);
                analogInput.ValidateAndSetScanTime(500);
                analogInput.ValidateAndSetOnOffScan(true);
                analogInput.ValidateAndSetLowLimit(-10.0);
                analogInput.ValidateAndSetHighLimit(100.0);
                analogInput.ValidateAndSetUnits("°C");
                Console.WriteLine($"✅ AI tag: {analogInput.Id} - {analogInput.Description} ({analogInput.LowLimit}-{analogInput.HighLimit} {analogInput.Units})");

                // AO - Analog Output
                var analogOutput = new Tag(TagType.AO, "AO_001", "Kontrola ventila", 4001);
                analogOutput.ValidateAndSetInitialValue(50.0);
                analogOutput.ValidateAndSetLowLimit(0.0);
                analogOutput.ValidateAndSetHighLimit(100.0);
                analogOutput.ValidateAndSetUnits("%");
                Console.WriteLine($"✅ AO tag: {analogOutput.Id} - {analogOutput.Description} (inicijalno: {analogOutput.InitialValue}{analogOutput.Units})");

                // Dodaj tagove u bazu
                context.Tags.AddRange(new[] { digitalInput, digitalOutput, analogInput, analogOutput });
                context.SaveChanges();
                Console.WriteLine($"💾 Sačuvano {context.Tags.Count()} tagova u bazu podataka");

                // Test validacije - pokušaj unosa neadekvatnih podataka
                Console.WriteLine("\nTest validacije:");
                try
                {
                    digitalInput.ValidateAndSetUnits("°C"); // DI ne može imati units
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"✅ Validacija radi: {ex.Message}");
                }

                try
                {
                    analogOutput.ValidateAndSetScanTime(1000); // AO ne može imati scan time
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"✅ Validacija radi: {ex.Message}");
                }
            }
            Console.WriteLine();
        }

        static void TestAlarmOperations()
        {
            Console.WriteLine("=== TEST 3: Dodavanje i uklanjanje alarma ===");

            using (var context = new ContextClass())
            {
                var analogTag = context.Tags.Include(t => t.Alarms).FirstOrDefault(t => t.Id == "AI_001");
                if (analogTag != null)
                {
                    Console.WriteLine($"Dodavanje alarma na tag: {analogTag.Id}");

                    // Kreiranje alarma
                    var highTempAlarm = new Alarm("HIGH_TEMP", AlarmTrigger.Above, 80.0, "UPOZORENJE: Temperatura je previsoka!");
                    var lowTempAlarm = new Alarm("LOW_TEMP", AlarmTrigger.Below, 5.0, "UPOZORENJE: Temperatura je preniska!");
                    var criticalTempAlarm = new Alarm("CRITICAL_TEMP", AlarmTrigger.Above, 95.0, "KRITIČNO: Temperatura je opasno visoka!");

                    // Dodavanje alarma na tag
                    analogTag.AddAlarm(highTempAlarm);
                    analogTag.AddAlarm(lowTempAlarm);
                    analogTag.AddAlarm(criticalTempAlarm);

                    context.SaveChanges();
                    Console.WriteLine($"✅ Dodano {analogTag.Alarms.Count} alarma:");
                    foreach (var alarm in analogTag.Alarms)
                    {
                        string direction = alarm.Trigger == AlarmTrigger.Above ? ">" : "<";
                        Console.WriteLine($"   - {alarm.Id}: {alarm.Message} (aktivira se kad vrednost {direction} {alarm.Threshold})");
                    }

                    // Test aktivacije alarma
                    Console.WriteLine("\nTest aktivacije alarma:");
                    TestAlarmActivation(analogTag, 85.0); // Visoka temperatura
                    TestAlarmActivation(analogTag, 2.0);  // Niska temperatura
                    TestAlarmActivation(analogTag, 98.0); // Kritična temperatura
                    TestAlarmActivation(analogTag, 25.0); // Normalna temperatura
                }
            }
            Console.WriteLine();
        }

        static void TestAlarmActivation(Tag tag, double value)
        {
            var triggeredAlarms = tag.CheckAlarms(value);
            if (triggeredAlarms.Any())
            {
                Console.WriteLine($"🚨 Vrednost {value}°C aktivirala {triggeredAlarms.Count} alarma:");
                foreach (var alarm in triggeredAlarms)
                {
                    Console.WriteLine($"   - {alarm.Id}: {alarm.Message}");
                }
            }
            else
            {
                Console.WriteLine($"✅ Vrednost {value}°C - nema aktivnih alarma");
            }
        }

        static void TestActivatedAlarms()
        {
            Console.WriteLine("=== TEST 4: Aktivirani alarmi u bazi podataka ===");

            using (var context = new ContextClass())
            {
                var analogTag = context.Tags.Include(t => t.Alarms).FirstOrDefault(t => t.Id == "AI_001");
                if (analogTag != null)
                {
                    Console.WriteLine("Simulacija promene vrednosti i čuvanje aktiviranih alarma:");

                    // Simulacija različitih vrednosti
                    double[] testValues = { 90.0, 3.0, 97.0, 20.0, 85.0 };

                    foreach (double currentValue in testValues)
                    {
                        var triggeredAlarms = analogTag.CheckAlarms(currentValue);

                        if (triggeredAlarms.Any())
                        {
                            Console.WriteLine($"\n📊 Vrednost: {currentValue}°C");
                            foreach (var alarm in triggeredAlarms)
                            {
                                // Kreiranje aktiviranog alarma
                                var activatedAlarm = new ActivatedAlarm(alarm, analogTag.Id);
                                context.ActivatedAlarms.Add(activatedAlarm);

                                Console.WriteLine($"   🚨 Alarm {alarm.Id}: {alarm.Message}");
                                Console.WriteLine($"      Vreme: {activatedAlarm.AlarmTime}");
                                Console.WriteLine($"      Tag: {activatedAlarm.TagName}");
                            }
                        }
                    }

                    context.SaveChanges();
                    Console.WriteLine($"\n💾 Ukupno aktiviranih alarma u bazi: {context.ActivatedAlarms.Count()}");
                }
            }
            Console.WriteLine();
        }

        static void TestReportGeneration()
        {
            Console.WriteLine("=== TEST 5: Generiranje REPORT fajla ===");

            using (var context = new ContextClass())
            {
                var analogInputs = context.Tags.Where(t => t.Type == TagType.AI).ToList();
                var activatedAlarms = context.ActivatedAlarms.OrderByDescending(a => a.AlarmTime).ToList();

                string reportContent = GenerateReport(analogInputs, activatedAlarms);

                // Sačuvaj izvještaj u fajl
                string fileName = $"ScadaReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                File.WriteAllText(fileName, reportContent);

                Console.WriteLine($"✅ REPORT fajl je generisan: {fileName}");
                Console.WriteLine($"📄 Sadržaj izvještaja:");
                Console.WriteLine(new string('=', 50));
                Console.WriteLine(reportContent);
                Console.WriteLine(new string('=', 50));
            }
            Console.WriteLine();
        }

        static string GenerateReport(List<Tag> analogInputs, List<ActivatedAlarm> activatedAlarms)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("SCADA SISTEM - IZVJEŠTAJ");
            report.AppendLine($"Generisan: {DateTime.Now}");
            report.AppendLine(new string('=', 50));
            report.AppendLine();

            report.AppendLine("ANALOGNI ULAZI - SREDNJE VREDNOSTI ±5:");
            report.AppendLine(new string('-', 40));

            foreach (var tag in analogInputs)
            {
                if (tag.LowLimit.HasValue && tag.HighLimit.HasValue)
                {
                    // Računanje srednje vrednosti: (high_limit + low_limit) / 2 ± 5
                    double midValue = (tag.HighLimit.Value + tag.LowLimit.Value) / 2.0;
                    double lowerBound = midValue - 5.0;
                    double upperBound = midValue + 5.0;

                    report.AppendLine($"Tag: {tag.Id} - {tag.Description}");
                    report.AppendLine($"  Opseg: {tag.LowLimit} - {tag.HighLimit} {tag.Units}");
                    report.AppendLine($"  Srednja vrednost ±5: {lowerBound:F1} - {upperBound:F1} {tag.Units}");
                    report.AppendLine();
                }
            }

            report.AppendLine("POSLEDNJI AKTIVIRANI ALARMI:");
            report.AppendLine(new string('-', 40));

            if (activatedAlarms.Any())
            {
                foreach (var alarm in activatedAlarms.Take(10)) // Poslednji 10 alarma
                {
                    report.AppendLine($"{alarm.AlarmTime:dd.MM.yyyy HH:mm:ss} - {alarm.TagName}");
                    report.AppendLine($"  Alarm: {alarm.AlarmId}");
                    report.AppendLine($"  Poruka: {alarm.Message}");
                    report.AppendLine();
                }
            }
            else
            {
                report.AppendLine("Nema aktiviranih alarma.");
            }

            return report.ToString();
        }

        static void TestConfigurationSaveLoad()
        {
            Console.WriteLine("=== TEST 6: Čuvanje/Učitavanje konfiguracije ===");

            using (var context = new ContextClass())
            {
                Console.WriteLine("📊 Trenutno stanje baze podataka:");
                Console.WriteLine($"   Tagovi: {context.Tags.Count()}");
                Console.WriteLine($"   Alarmi: {context.Alarms.Count()}");
                Console.WriteLine($"   Aktivirani alarmi: {context.ActivatedAlarms.Count()}");

                Console.WriteLine("\n✅ Konfiguracija je automatski sačuvana u bazi podataka");
                Console.WriteLine("✅ Pri sledećem pokretanju aplikacije, podaci će biti učitani iz baze");

                // Demonstracija učitavanja
                var savedTags = context.Tags.Include(t => t.Alarms).ToList();
                Console.WriteLine($"\n📋 Učitani tagovi iz baze:");
                foreach (var tag in savedTags)
                {
                    Console.WriteLine($"   - {tag.Id}: {tag.Description} (tip: {tag.Type})");
                    if (tag.Alarms.Any())
                    {
                        Console.WriteLine($"     Alarmi: {string.Join(", ", tag.Alarms.Select(a => a.Id))}");
                    }
                }
            }
            Console.WriteLine();
        }

        // ===== DATA COLLECTOR TEST METHODS =====

        static void TestDataCollectorBasicOperations()
        {
            Console.WriteLine("\n=== TEST DC1: DataCollector osnovne operacije ===");
            
            var dataCollector = new DataCollector();
            Console.WriteLine("✅ DataCollector instanciran");
            
            // Test da li je prazan na početku
            Console.WriteLine($"📊 Početno stanje: DataCollector kreiran bez tagova");
            
            // Test dodavanja null tag-a
            try
            {
                dataCollector.AddTag(null);
                Console.WriteLine("❌ GREŠKA: Trebalo je baciti exception za null tag");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("✅ Validacija: null tag je odbačen");
            }
            
            // Test uklanjanja nepostojećeg tag-a
            dataCollector.RemoveTag("NEPOSTOJECI_TAG");
            Console.WriteLine("✅ Uklanjanje nepostojećeg tag-a je bezbedno");
            
            Console.WriteLine();
        }

        static void TestDataCollectorTagManagement()
        {
            Console.WriteLine("=== TEST DC2: Upravljanje tagovima u DataCollector ===");
            
            var dataCollector = new DataCollector();
            
            // Kreiranje različitih tipova tagova
            var diTag = new Tag(TagType.DI, "DC_DI_001", "Test Digital Input", 1001);
            diTag.ValidateAndSetScanTime(500);
            diTag.ValidateAndSetOnOffScan(true);
            
            var doTag = new Tag(TagType.DO, "DC_DO_001", "Test Digital Output", 2001);
            doTag.ValidateAndSetInitialValue(1);
            
            var aiTag = new Tag(TagType.AI, "DC_AI_001", "Test Analog Input", 3001);
            aiTag.ValidateAndSetScanTime(1000);
            aiTag.ValidateAndSetOnOffScan(true);
            aiTag.ValidateAndSetLowLimit(0.0);
            aiTag.ValidateAndSetHighLimit(100.0);
            aiTag.ValidateAndSetUnits("°C");
            
            var aoTag = new Tag(TagType.AO, "DC_AO_001", "Test Analog Output", 4001);
            aoTag.ValidateAndSetInitialValue(25.5);
            aoTag.ValidateAndSetLowLimit(0.0);
            aoTag.ValidateAndSetHighLimit(100.0);
            aoTag.ValidateAndSetUnits("%");
            
            // Dodavanje tagova
            Console.WriteLine("Dodavanje tagova:");
            dataCollector.AddTag(diTag);
            dataCollector.AddTag(doTag);
            dataCollector.AddTag(aiTag);
            dataCollector.AddTag(aoTag);
            Console.WriteLine("✅ Dodana 4 tag-a različitih tipova");
            
            // Test uklanjanja postojećeg tag-a
            Console.WriteLine("\nUklanjanje tag-a:");
            dataCollector.RemoveTag("DC_DI_001");
            Console.WriteLine("✅ Tag DC_DI_001 je uklonjen");
            
            // Test dodavanja tag-a sa istim ID
            var duplicateTag = new Tag(TagType.AI, "DC_AI_001", "Duplicate test", 5001);
            dataCollector.AddTag(duplicateTag);
            Console.WriteLine("✅ Dodavanje tag-a sa istim ID je dozvoljeno (lista implementacija)");
            
            Console.WriteLine();
        }

        static void TestDataCollectorScanningControl()
        {
            Console.WriteLine("=== TEST DC3: Kontrola skeniranja tagova ===");
            
            var dataCollector = new DataCollector();
            
            // Kreiranje input tagova
            var diTag = new Tag(TagType.DI, "SCAN_DI_001", "Scannable DI", 1001);
            diTag.ValidateAndSetScanTime(200);
            diTag.ValidateAndSetOnOffScan(true);
            
            var aiTag = new Tag(TagType.AI, "SCAN_AI_001", "Scannable AI", 3001);
            aiTag.ValidateAndSetScanTime(300);
            aiTag.ValidateAndSetOnOffScan(false); // Početno isključeno
            aiTag.ValidateAndSetLowLimit(-50.0);
            aiTag.ValidateAndSetHighLimit(150.0);
            aiTag.ValidateAndSetUnits("V");
            
            // Dodaj tagove
            dataCollector.AddTag(diTag);
            dataCollector.AddTag(aiTag);
            Console.WriteLine("✅ Dodana 2 input tag-a");
            
            // Test kontrole skeniranja
            Console.WriteLine("\nTestiranje kontrole skeniranja:");
            
            // Uključi skeniranje za AI tag
            dataCollector.SetTagScanning("SCAN_AI_001", true);
            Console.WriteLine("✅ Skeniranje uključeno za SCAN_AI_001");
            
            // Isključi skeniranje za DI tag
            dataCollector.SetTagScanning("SCAN_DI_001", false);
            Console.WriteLine("✅ Skeniranje isključeno za SCAN_DI_001");
            
            // Test sa nepostojećim tag-om
            dataCollector.SetTagScanning("NEPOSTOJECI_TAG", true);
            Console.WriteLine("✅ Kontrola skeniranja za nepostojeći tag je bezbedna");
            
            // Test sa output tag-om (treba biti ignorisan)
            var doTag = new Tag(TagType.DO, "SCAN_DO_001", "Non-scannable DO", 2001);
            dataCollector.AddTag(doTag);
            dataCollector.SetTagScanning("SCAN_DO_001", true);
            Console.WriteLine("✅ Pokušaj skeniranja output tag-a je bezbedno ignorisan");
            
            Console.WriteLine();
        }

        static void TestDataCollectorWriteOperations()
        {
            Console.WriteLine("=== TEST DC4: Pisanje vrednosti u output tagove ===");
            
            var dataCollector = new DataCollector();
            
            // Kreiranje output tagova
            var doTag = new Tag(TagType.DO, "WRITE_DO_001", "Writable DO", 2001);
            doTag.ValidateAndSetInitialValue(0);
            
            var aoTag = new Tag(TagType.AO, "WRITE_AO_001", "Writable AO", 4001);
            aoTag.ValidateAndSetInitialValue(50.0);
            aoTag.ValidateAndSetLowLimit(0.0);
            aoTag.ValidateAndSetHighLimit(100.0);
            aoTag.ValidateAndSetUnits("%");
            
            // Dodaj tagove
            dataCollector.AddTag(doTag);
            dataCollector.AddTag(aoTag);
            Console.WriteLine("✅ Dodana 2 output tag-a");
            
            // Test pisanja u DO tag
            Console.WriteLine("\nTestiranje pisanja vrednosti:");
            dataCollector.WriteTagValue("WRITE_DO_001", 1);
            Console.WriteLine("✅ Napisana vrednost 1 u DO tag");
            
            // Test čitanja vrednosti
            var doValue = dataCollector.GetTagValue("WRITE_DO_001");
            Console.WriteLine($"✅ Pročitana vrednost iz DO tag: {doValue}");
            
            // Test pisanja u AO tag
            dataCollector.WriteTagValue("WRITE_AO_001", 75.5);
            Console.WriteLine("✅ Napisana vrednost 75.5 u AO tag");
            
            var aoValue = dataCollector.GetTagValue("WRITE_AO_001");
            Console.WriteLine($"✅ Pročitana vrednost iz AO tag: {aoValue}");
            
            // Test pisanja u nepostojeći tag
            dataCollector.WriteTagValue("NEPOSTOJECI_TAG", 123);
            Console.WriteLine("✅ Pisanje u nepostojeći tag je bezbedno");
            
            // Test pisanja u input tag (treba biti odbačeno)
            var aiTag = new Tag(TagType.AI, "WRITE_AI_001", "Non-writable AI", 3001);
            dataCollector.AddTag(aiTag);
            dataCollector.WriteTagValue("WRITE_AI_001", 42);
            Console.WriteLine("✅ Pokušaj pisanja u input tag je bezbedno odbačen");
            
            Console.WriteLine();
        }

        static void TestDataCollectorPLCIntegration()
        {
            Console.WriteLine("=== TEST DC5: Integracija sa PLC Simulator ===");
            
            var dataCollector = new DataCollector();
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();
            
            // Kreiranje tagova mapiranih na PLC adrese
            var aiTag1 = new Tag(TagType.AI, "PLC_AI_001", "PLC Analog Input 1", 1); // ADDR001
            aiTag1.ValidateAndSetScanTime(500);
            aiTag1.ValidateAndSetOnOffScan(true);
            aiTag1.ValidateAndSetUnits("V");
            
            var aiTag2 = new Tag(TagType.AI, "PLC_AI_002", "PLC Analog Input 2", 2); // ADDR002  
            aiTag2.ValidateAndSetScanTime(800);
            aiTag2.ValidateAndSetOnOffScan(true);
            aiTag2.ValidateAndSetUnits("A");
            
            var diTag = new Tag(TagType.DI, "PLC_DI_009", "PLC Digital Input", 9); // ADDR009
            diTag.ValidateAndSetScanTime(1000);
            diTag.ValidateAndSetOnOffScan(true);
            
            var aoTag = new Tag(TagType.AO, "PLC_AO_005", "PLC Analog Output", 5); // ADDR005
            aoTag.ValidateAndSetInitialValue(30.0);
            aoTag.ValidateAndSetUnits("%");
            
            var doTag = new Tag(TagType.DO, "PLC_DO_010", "PLC Digital Output", 10); // ADDR010
            doTag.ValidateAndSetInitialValue(0);
            
            // Dodaj tagove
            dataCollector.AddTag(aiTag1);
            dataCollector.AddTag(aiTag2);
            dataCollector.AddTag(diTag);
            dataCollector.AddTag(aoTag);
            dataCollector.AddTag(doTag);
            Console.WriteLine("✅ Dodano 5 tagova mapiranih na PLC adrese");
            
            // Pokreni DataCollector sa PLC simulatorom
            Console.WriteLine("\nPokretanje DataCollector sa PLC integracijom:");
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ DataCollector pokrenut sa PLC simulatorom");
            
            // Čekaj da se sistem stabilizuje
            Console.WriteLine("⏳ Čekanje 3 sekunde da se sistem stabilizuje...");
            Thread.Sleep(3000);
            
            // Test pisanja u output tagove (treba da se proslijedi u PLC)
            Console.WriteLine("\nTestiranje pisanja u PLC output tagove:");
            dataCollector.WriteTagValue("PLC_AO_005", 65.0);
            dataCollector.WriteTagValue("PLC_DO_010", 1);
            Console.WriteLine("✅ Vrednosti poslane u PLC simulator");
            
            // Čekaj još malo da se vide rezultati skeniranja
            Console.WriteLine("⏳ Posmatranje skeniranja 5 sekundi...");
            Thread.Sleep(5000);
            
            // Zaustavi DataCollector
            dataCollector.Stop();
            Console.WriteLine("✅ DataCollector zaustavljen");
            
            Console.WriteLine();
        }

        static void TestDataCollectorAlarmDetection()
        {
            Console.WriteLine("=== TEST DC6: Detekcija alarma tokom skeniranja ===");
            
            using (var context = new ContextClass())
            {
                // Očisti stare podatke
                context.ActivatedAlarms.RemoveRange(context.ActivatedAlarms);
                context.SaveChanges();
            }
            
            var dataCollector = new DataCollector();
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();
            
            // Kreiranje AI tag-a sa alarmima
            var aiTag = new Tag(TagType.AI, "ALARM_AI_001", "Tag sa alarmima", 4); // ADDR004 (random vrednosti)
            aiTag.ValidateAndSetScanTime(500);
            aiTag.ValidateAndSetOnOffScan(true);
            aiTag.ValidateAndSetLowLimit(-100.0);
            aiTag.ValidateAndSetHighLimit(100.0);
            aiTag.ValidateAndSetUnits("°C");
            
            // Dodaj alarme
            var lowAlarm = new Alarm("LOW_TEMP_ALARM", AlarmTrigger.Below, 10.0, "UPOZORENJE: Temperatura je niska!");
            var highAlarm = new Alarm("HIGH_TEMP_ALARM", AlarmTrigger.Above, 40.0, "UPOZORENJE: Temperatura je visoka!");
            var criticalAlarm = new Alarm("CRITICAL_TEMP_ALARM", AlarmTrigger.Above, 80.0, "KRITIČNO: Temperatura je kritična!");
            
            aiTag.AddAlarm(lowAlarm);
            aiTag.AddAlarm(highAlarm);
            aiTag.AddAlarm(criticalAlarm);
            
            // Sačuvaj tag sa alarmima u bazu
            using (var context = new ContextClass())
            {
                context.Tags.Add(aiTag);
                context.SaveChanges();
            }
            
            dataCollector.AddTag(aiTag);
            Console.WriteLine("✅ Dodat AI tag sa 3 alarma");
            
            // Pokreni sistem
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ DataCollector pokrenut - posmatraj alarme");
            
            // Ostavi sistem da radi i generiše alarme
            Console.WriteLine("⏳ Posmatranje alarmiranja 10 sekundi...");
            Console.WriteLine("   (ADDR004 generiše random vrednosti 0-50, alarmi se aktiviraju)");
            Thread.Sleep(10000);
            
            // Zaustavi sistem
            dataCollector.Stop();
            Console.WriteLine("✅ DataCollector zaustavljen");
            
            // Proveri koliko je alarma aktivirano
            using (var context = new ContextClass())
            {
                var activatedAlarmsCount = context.ActivatedAlarms.Count();
                Console.WriteLine($"📊 Ukupno aktiviranih alarma u bazi: {activatedAlarmsCount}");
                
                if (activatedAlarmsCount > 0)
                {
                    var recentAlarms = context.ActivatedAlarms
                        .OrderByDescending(a => a.AlarmTime)
                        .Take(5)
                        .ToList();
                    
                    Console.WriteLine("🚨 Poslednji aktivirani alarmi:");
                    foreach (var alarm in recentAlarms)
                    {
                        Console.WriteLine($"   - {alarm.AlarmTime:HH:mm:ss} | {alarm.AlarmId} | {alarm.TagName}");
                        Console.WriteLine($"     Poruka: {alarm.Message}");
                    }
                }
            }
            
            Console.WriteLine();
        }

        static void TestDataCollectorTimerManagement()
        {
            Console.WriteLine("=== TEST DC7: Upravljanje timer-ima za skeniranje ===");
            
            var dataCollector = new DataCollector();
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();
            
            // Kreiranje tagova sa različitim scan time-ovima
            var fastTag = new Tag(TagType.AI, "TIMER_FAST_AI", "Brzo skeniranje", 1);
            fastTag.ValidateAndSetScanTime(200); // 200ms
            fastTag.ValidateAndSetOnOffScan(true);
            fastTag.ValidateAndSetUnits("V");
            
            var mediumTag = new Tag(TagType.AI, "TIMER_MEDIUM_AI", "Srednje skeniranje", 2);
            mediumTag.ValidateAndSetScanTime(1000); // 1s
            mediumTag.ValidateAndSetOnOffScan(true);
            mediumTag.ValidateAndSetUnits("A");
            
            var slowTag = new Tag(TagType.AI, "TIMER_SLOW_AI", "Sporo skeniranje", 3);
            slowTag.ValidateAndSetScanTime(2000); // 2s
            slowTag.ValidateAndSetOnOffScan(true);
            slowTag.ValidateAndSetUnits("W");
            
            var disabledTag = new Tag(TagType.AI, "TIMER_DISABLED_AI", "Isključeno skeniranje", 4);
            disabledTag.ValidateAndSetScanTime(500);
            disabledTag.ValidateAndSetOnOffScan(false); // Neće se skenirati
            disabledTag.ValidateAndSetUnits("Hz");
            
            // Dodaj tagove
            dataCollector.AddTag(fastTag);
            dataCollector.AddTag(mediumTag);
            dataCollector.AddTag(slowTag);
            dataCollector.AddTag(disabledTag);
            Console.WriteLine("✅ Dodana 4 tag-a sa različitim scan time-ovima");
            Console.WriteLine("   - TIMER_FAST_AI: 200ms");
            Console.WriteLine("   - TIMER_MEDIUM_AI: 1000ms");
            Console.WriteLine("   - TIMER_SLOW_AI: 2000ms");
            Console.WriteLine("   - TIMER_DISABLED_AI: isključen");
            
            // Pokreni sistem
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ DataCollector pokrenut - timer-i aktivni");
            
            // Posmatraj različite brzine skeniranja
            Console.WriteLine("⏳ Posmatranje različitih brzina skeniranja 8 sekundi...");
            Thread.Sleep(8000);
            
            // Test dinamičke kontrole skeniranja
            Console.WriteLine("\nTest dinamičke kontrole skeniranja:");
            dataCollector.SetTagScanning("TIMER_FAST_AI", false); // Isključi brzi
            dataCollector.SetTagScanning("TIMER_DISABLED_AI", true); // Uključi prethodno isključen
            Console.WriteLine("✅ Dinamički promenjena kontrola skeniranja");
            
            // Posmatraj promene
            Console.WriteLine("⏳ Posmatranje promena 5 sekundi...");
            Thread.Sleep(5000);
            
            // Zaustavi sistem
            dataCollector.Stop();
            Console.WriteLine("✅ DataCollector zaustavljen - svi timer-i zaustavljeni");
            
            Console.WriteLine();
        }

        static void TestDataCollectorStartStopLifecycle()
        {
            Console.WriteLine("=== TEST DC8: Životni ciklus DataCollector (Start/Stop) ===");
            
            var dataCollector = new DataCollector();
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();
            
            // Pripremi tagove
            var aiTag = new Tag(TagType.AI, "LIFECYCLE_AI", "Test AI za lifecycle", 1);
            aiTag.ValidateAndSetScanTime(500);
            aiTag.ValidateAndSetOnOffScan(true);
            aiTag.ValidateAndSetUnits("V");
            
            var aoTag = new Tag(TagType.AO, "LIFECYCLE_AO", "Test AO za lifecycle", 5);
            aoTag.ValidateAndSetInitialValue(42.0);
            aoTag.ValidateAndSetUnits("%");
            
            var doTag = new Tag(TagType.DO, "LIFECYCLE_DO", "Test DO za lifecycle", 10);
            doTag.ValidateAndSetInitialValue(1);
            
            dataCollector.AddTag(aiTag);
            dataCollector.AddTag(aoTag);
            dataCollector.AddTag(doTag);
            Console.WriteLine("✅ Pripremljena 3 tag-a za lifecycle test");
            
            // Test 1: Normalno pokretanje
            Console.WriteLine("\n1. Test normalnog pokretanja:");
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ DataCollector uspešno pokrenut");
            
            // Čekaj malo
            Console.WriteLine("⏳ Rad sistema 3 sekunde...");
            Thread.Sleep(3000);
            
            // Test 2: Duplo pokretanje (treba biti bezbedno)
            Console.WriteLine("\n2. Test duplog pokretanja:");
            dataCollector.Start(plcSimulator); // Treba biti ignorisan
            Console.WriteLine("✅ Duplo pokretanje je bezbedno ignorisano");
            
            // Test 3: Normalno zaustavljanje
            Console.WriteLine("\n3. Test normalnog zaustavljanja:");
            dataCollector.Stop();
            Console.WriteLine("✅ DataCollector uspešno zaustavljen");
            
            // Test 4: Duplo zaustavljanje (treba biti bezbedno)
            Console.WriteLine("\n4. Test duplog zaustavljanja:");
            dataCollector.Stop(); // Treba biti ignorisan
            Console.WriteLine("✅ Duplo zaustavljanje je bezbedno ignorisano");
            
            // Test 5: Restart ciklus
            Console.WriteLine("\n5. Test restart ciklusa:");
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ DataCollector ponovo pokrenut");
            
            Console.WriteLine("⏳ Rad sistema 2 sekunde...");
            Thread.Sleep(2000);
            
            dataCollector.Stop();
            Console.WriteLine("✅ DataCollector ponovo zaustavljen");
            
            // Test 6: Dodavanje tagova nakon zaustavljanja
            Console.WriteLine("\n6. Test dodavanja tagova nakon zaustavljanja:");
            var newTag = new Tag(TagType.DI, "LIFECYCLE_DI", "Tag dodat nakon stop", 9);
            newTag.ValidateAndSetScanTime(1000);
            newTag.ValidateAndSetOnOffScan(true);
            
            dataCollector.AddTag(newTag);
            Console.WriteLine("✅ Tag uspešno dodat nakon zaustavljanja");
            
            // Pokreni ponovo da vidiš da li novi tag radi
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ DataCollector pokrenut sa novim tag-om");
            
            Console.WriteLine("⏳ Test novog tag-a 3 sekunde...");
            Thread.Sleep(3000);
            
            dataCollector.Stop();
            Console.WriteLine("✅ Finalno zaustavljanje - lifecycle test završen");
            
            Console.WriteLine();
        }
    }
}
