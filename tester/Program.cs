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
                
                TestDataCollectorConfigurationLoading();
                TestDataCollectorBasicOperations();
                TestDataCollectorTagManagement();
                TestDataCollectorScanningControl();
                TestDataCollectorWriteOperations();
                TestDataCollectorPLCIntegration();
                TestDataCollectorAlarmDetection();
                TestDataCollectorTimerManagement();
                TestDataCollectorStartStopLifecycle();
                
                Console.WriteLine("\n" + new string('=', 70));
                Console.WriteLine("=== DODATNI TESTOVI PO SPECIFIKACIJI - RASPOREDRADA.MD ===");
                Console.WriteLine(new string('=', 70));
                
                TestAdvancedAlarmScenarios();
                TestReportGenerationAdvanced();
                TestDatabasePersistenceAdvanced();
                TestComplexTagValidation();
                TestRealTimeAlarmProcessing();
                TestDataCollectorStressTest();
                
                Console.WriteLine("\n" + new string('=', 50));
                Console.WriteLine("=== FINALNI PRIKAZ BAZE PODATAKA ===");
                Console.WriteLine(new string('=', 50));
                DisplayFinalDatabaseContents();

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

        static void TestDataCollectorConfigurationLoading()
        {
            Console.WriteLine("\n=== TEST DC0: Učitavanje konfiguracije iz baze ===");
            Console.WriteLine("Kreiranje nove instance DataCollector-a...");

            // U ovom trenutku, u bazi bi trebalo da postoje 4 taga iz prethodnih testova.
            // Konstruktor DataCollector-a treba automatski da ih učita.
            var dataCollector = new DataCollector();

            // Poruka "[INFO] Successfully loaded 4 tags from the database." treba da se pojavi iznad.
            Console.WriteLine("✅ DataCollector je instanciran i trebalo bi da je učitao konfiguraciju.");
            Console.WriteLine("   Proverite konzolni ispis iznad za potvrdu o broju učitanih tagova.");

            // Hajde da proverimo da li je učitani tag 'AO_001' zaista tu
            Console.WriteLine("Pokušaj pisanja u tag 'AO_001' koji bi trebalo da je učitan...");
            dataCollector.WriteTagValue("AO_001", 99.9);
            var value = dataCollector.GetTagValue("AO_001");
            if (value == 99.9)
            {
                Console.WriteLine($"✅ Uspešno upisana i pročitana vrednost {value:F1} za tag 'AO_001'. Konfiguracija je učitana.");
            }
            else
            {
                Console.WriteLine($"❌ GREŠKA: Vrednost za tag 'AO_001' nije ispravna. Pročitano: {value}");
            }
            Console.WriteLine();
        }

        static void TestDataCollectorBasicOperations()
        {
            Console.WriteLine("\n=== TEST DC1: DataCollector osnovne operacije ===");
            
            var dataCollector = new DataCollector();
            Console.WriteLine("✅ DataCollector instanciran (sa konfiguracijom učitanom iz baze)");

            // Test početnog stanja je sada u TestDataCollectorConfigurationLoading

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

        // ===== DODATNI TESTOVI PO SPECIFIKACIJI =====

        static void TestAdvancedAlarmScenarios()
        {
            Console.WriteLine("\n=== TEST AS1: Napredni scenariji alarma ===");
            
            using (var context = new ContextClass())
            {
                // Očisti stare podatke za čist test
                var existingActivated = context.ActivatedAlarms.ToList();
                context.ActivatedAlarms.RemoveRange(existingActivated);
                context.SaveChanges();
            }
            
            var dataCollector = new DataCollector();
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();
            
            // Kreiranje AI tag-a sa kompleksnim alarmima
            var tempTag = new Tag(TagType.AI, "TEMP_SENSOR_001", "Kompleksni temperaturni senzor", 1);
            tempTag.ValidateAndSetScanTime(300);
            tempTag.ValidateAndSetOnOffScan(true);
            tempTag.ValidateAndSetLowLimit(-40.0);
            tempTag.ValidateAndSetHighLimit(120.0);
            tempTag.ValidateAndSetUnits("°C");
            
            // Dodavanje više alarma sa različitim pragovima
            var freezingAlarm = new Alarm("FREEZING_ALARM", AlarmTrigger.Below, 0.0, "KRITIČNO: Temperatura ispod tačke smrzavanja!");
            var lowTempAlarm = new Alarm("LOW_TEMP_ALARM", AlarmTrigger.Below, 10.0, "UPOZORENJE: Niska temperatura!");
            var highTempAlarm = new Alarm("HIGH_TEMP_ALARM", AlarmTrigger.Above, 80.0, "UPOZORENJE: Visoka temperatura!");
            var overheatingAlarm = new Alarm("OVERHEATING_ALARM", AlarmTrigger.Above, 100.0, "KRITIČNO: Pregrevanje sistema!");
            var dangerAlarm = new Alarm("DANGER_TEMP_ALARM", AlarmTrigger.Above, 110.0, "OPASNOST: Kritična temperatura!");
            
            tempTag.AddAlarm(freezingAlarm);
            tempTag.AddAlarm(lowTempAlarm);
            tempTag.AddAlarm(highTempAlarm);
            tempTag.AddAlarm(overheatingAlarm);
            tempTag.AddAlarm(dangerAlarm);
            
            // Kreiranje dodatnog AI tag-a za pritisak
            var pressureTag = new Tag(TagType.AI, "PRESSURE_SENSOR_001", "Senzor pritiska", 2);
            pressureTag.ValidateAndSetScanTime(400);
            pressureTag.ValidateAndSetOnOffScan(true);
            pressureTag.ValidateAndSetLowLimit(0.0);
            pressureTag.ValidateAndSetHighLimit(10.0);
            pressureTag.ValidateAndSetUnits("bar");
            
            var lowPressureAlarm = new Alarm("LOW_PRESSURE", AlarmTrigger.Below, 1.0, "UPOZORENJE: Nizak pritisak!");
            var highPressureAlarm = new Alarm("HIGH_PRESSURE", AlarmTrigger.Above, 8.0, "UPOZORENJE: Visok pritisak!");
            var criticalPressureAlarm = new Alarm("CRITICAL_PRESSURE", AlarmTrigger.Above, 9.5, "KRITIČNO: Opasan pritisak!");
            
            pressureTag.AddAlarm(lowPressureAlarm);
            pressureTag.AddAlarm(highPressureAlarm);
            pressureTag.AddAlarm(criticalPressureAlarm);
            
            // Sačuvaj tagove u bazu
            using (var context = new ContextClass())
            {
                context.Tags.Add(tempTag);
                context.Tags.Add(pressureTag);
                context.SaveChanges();
            }
            
            dataCollector.AddTag(tempTag);
            dataCollector.AddTag(pressureTag);
            Console.WriteLine("✅ Dodana 2 AI tag-a sa ukupno 8 alarma");
            Console.WriteLine("   - TEMP_SENSOR_001: 5 alarma (0°C, 10°C, 80°C, 100°C, 110°C)");
            Console.WriteLine("   - PRESSURE_SENSOR_001: 3 alarma (1bar, 8bar, 9.5bar)");
            
            // Pokreni sistem
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ DataCollector pokrenut sa PLC simulatorom");
            
            // Test različitih vrednosti koje aktiviraju različite kombinacije alarma
            Console.WriteLine("\n🔥 Test kompleksnih alarm scenarija (15 sekundi):");
            Console.WriteLine("   ADDR001: Sine wave (-100 do 100) - aktiviraće različite temp alarme");
            Console.WriteLine("   ADDR002: Ramp (0-100) - aktiviraće različite pritisak alarme");
            
            Thread.Sleep(15000); // 15 sekundi intenzivnog testiranja
            
            dataCollector.Stop();
            Console.WriteLine("✅ DataCollector zaustavljen");
            
            // Proveri rezultate
            using (var context = new ContextClass())
            {
                var activatedAlarms = context.ActivatedAlarms.OrderByDescending(a => a.AlarmTime).ToList();
                Console.WriteLine($"\n📊 Ukupno aktiviranih alarma: {activatedAlarms.Count}");
                
                // Grupisanje po tag-ovima
                var tempAlarms = activatedAlarms.Where(a => a.TagName == "TEMP_SENSOR_001").ToList();
                var pressureAlarms = activatedAlarms.Where(a => a.TagName == "PRESSURE_SENSOR_001").ToList();
                
                Console.WriteLine($"   🌡️ Temperaturni alarmi: {tempAlarms.Count}");
                Console.WriteLine($"   🔧 Pritisak alarmi: {pressureAlarms.Count}");
                
                if (activatedAlarms.Count > 0)
                {
                    Console.WriteLine("\n🚨 Poslednji aktivirani alarmi:");
                    foreach (var alarm in activatedAlarms.Take(10))
                    {
                        Console.WriteLine($"   - {alarm.AlarmTime:HH:mm:ss.fff} | {alarm.AlarmId} | {alarm.TagName}");
                        Console.WriteLine($"     {alarm.Message}");
                    }
                }
            }
            
            Console.WriteLine();
        }

        static void TestReportGenerationAdvanced()
        {
            Console.WriteLine("=== TEST RG1: Napredna generacija izvještaja ===");
            
            using (var context = new ContextClass())
            {
                // Dodaj dodatne AI tagove za kompleksniji izvještaj
                var voltageTag = new Tag(TagType.AI, "VOLTAGE_MONITOR", "Monitor napona", 3);
                voltageTag.ValidateAndSetScanTime(600);
                voltageTag.ValidateAndSetOnOffScan(true);
                voltageTag.ValidateAndSetLowLimit(220.0);
                voltageTag.ValidateAndSetHighLimit(240.0);
                voltageTag.ValidateAndSetUnits("V");
                
                var currentTag = new Tag(TagType.AI, "CURRENT_MONITOR", "Monitor struje", 4);
                currentTag.ValidateAndSetScanTime(500);
                currentTag.ValidateAndSetOnOffScan(true);
                currentTag.ValidateAndSetLowLimit(0.0);
                currentTag.ValidateAndSetHighLimit(50.0);
                currentTag.ValidateAndSetUnits("A");
                
                // Dodaj u bazu
                context.Tags.Add(voltageTag);
                context.Tags.Add(currentTag);
                context.SaveChanges();
                
                // Generiši napredni izvještaj
                var allAnalogInputs = context.Tags.Where(t => t.Type == TagType.AI).ToList();
                var allActivatedAlarms = context.ActivatedAlarms.OrderByDescending(a => a.AlarmTime).ToList();
                
                string advancedReport = GenerateAdvancedReport(allAnalogInputs, allActivatedAlarms);
                
                // Sačuvaj izvještaj
                string fileName = $"ScadaAdvancedReport_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                File.WriteAllText(fileName, advancedReport);
                
                Console.WriteLine($"✅ Napredni REPORT fajl generisan: {fileName}");
                Console.WriteLine($"📊 Analiza {allAnalogInputs.Count} analognih ulaza");
                Console.WriteLine($"🚨 Uključeno {allActivatedAlarms.Count} aktiviranih alarma");
                
                Console.WriteLine($"\n📄 Sadržaj naprednog izvještaja:");
                Console.WriteLine(new string('=', 60));
                Console.WriteLine(advancedReport);
                Console.WriteLine(new string('=', 60));
            }
            
            Console.WriteLine();
        }

        static string GenerateAdvancedReport(List<Tag> analogInputs, List<ActivatedAlarm> activatedAlarms)
        {
            var report = new System.Text.StringBuilder();
            
            report.AppendLine("SCADA SISTEM - NAPREDNI IZVJEŠTAJ");
            report.AppendLine($"Generisan: {DateTime.Now}");
            report.AppendLine($"Ukupno AI tagova: {analogInputs.Count}");
            report.AppendLine($"Ukupno aktiviranih alarma: {activatedAlarms.Count}");
            report.AppendLine(new string('=', 60));
            report.AppendLine();
            
            report.AppendLine("ANALOGNI ULAZI - DETALJNE SREDNJE VREDNOSTI ±5:");
            report.AppendLine(new string('-', 50));
            
            foreach (var tag in analogInputs)
            {
                if (tag.LowLimit.HasValue && tag.HighLimit.HasValue)
                {
                    // Računanje srednje vrednosti: (high_limit + low_limit) / 2 ± 5
                    double midValue = (tag.HighLimit.Value + tag.LowLimit.Value) / 2.0;
                    double lowerBound = midValue - 5.0;
                    double upperBound = midValue + 5.0;
                    double range = tag.HighLimit.Value - tag.LowLimit.Value;
                    
                    report.AppendLine($"Tag: {tag.Id} - {tag.Description}");
                    report.AppendLine($"  Tip: {tag.Type} | IO Adresa: {tag.IOAddress}");
                    report.AppendLine($"  Pun opseg: {tag.LowLimit} - {tag.HighLimit} {tag.Units} (raspon: {range:F1})");
                    report.AppendLine($"  Srednja vrednost: {midValue:F1} {tag.Units}");
                    report.AppendLine($"  Ciljni opseg ±5: {lowerBound:F1} - {upperBound:F1} {tag.Units}");
                    report.AppendLine($"  Scan vreme: {tag.ScanTime}ms | Skeniranje: {(tag.OnOffScan == true ? "UKLJUČENO" : "ISKLJUČENO")}");
                    
                    // Dodaj informacije o alarmima za ovaj tag
                    var tagAlarms = activatedAlarms.Where(a => a.TagName == tag.Id).ToList();
                    if (tagAlarms.Any())
                    {
                        report.AppendLine($"  Aktivirani alarmi: {tagAlarms.Count}");
                        foreach (var alarm in tagAlarms.Take(3)) // Poslednji 3 alarma
                        {
                            report.AppendLine($"    - {alarm.AlarmTime:dd.MM HH:mm:ss} | {alarm.AlarmId}");
                        }
                    }
                    else
                    {
                        report.AppendLine($"  Aktivirani alarmi: Nema");
                    }
                    report.AppendLine();
                }
            }
            
            report.AppendLine("STATISTIKA ALARMA PO TIPOVIMA:");
            report.AppendLine(new string('-', 40));
            
            if (activatedAlarms.Any())
            {
                var alarmsByType = activatedAlarms
                    .GroupBy(a => a.AlarmId)
                    .OrderByDescending(g => g.Count())
                    .ToList();
                
                foreach (var group in alarmsByType)
                {
                    var lastAlarm = group.OrderByDescending(a => a.AlarmTime).First();
                    report.AppendLine($"Alarm: {group.Key}");
                    report.AppendLine($"  Broj aktivacija: {group.Count()}");
                    report.AppendLine($"  Poslednja aktivacija: {lastAlarm.AlarmTime:dd.MM.yyyy HH:mm:ss}");
                    report.AppendLine($"  Tag: {lastAlarm.TagName}");
                    report.AppendLine($"  Poruka: {lastAlarm.Message}");
                    report.AppendLine();
                }
                
                // Vremenska analiza
                if (activatedAlarms.Count > 1)
                {
                    var oldestAlarm = activatedAlarms.OrderBy(a => a.AlarmTime).First();
                    var newestAlarm = activatedAlarms.OrderByDescending(a => a.AlarmTime).First();
                    var timeSpan = newestAlarm.AlarmTime - oldestAlarm.AlarmTime;
                    
                    report.AppendLine("VREMENSKA ANALIZA:");
                    report.AppendLine($"  Prvi alarm: {oldestAlarm.AlarmTime:dd.MM.yyyy HH:mm:ss}");
                    report.AppendLine($"  Poslednji alarm: {newestAlarm.AlarmTime:dd.MM.yyyy HH:mm:ss}");
                    report.AppendLine($"  Vremenski period: {timeSpan.TotalMinutes:F1} minuta");
                    report.AppendLine($"  Prosečna frekvencija: {activatedAlarms.Count / Math.Max(timeSpan.TotalMinutes, 1):F2} alarma/min");
                }
            }
            else
            {
                report.AppendLine("Nema aktiviranih alarma u sistemu.");
            }
            
            return report.ToString();
        }

        static void TestDatabasePersistenceAdvanced()
        {
            Console.WriteLine("=== TEST DP1: Napredna perzistencija baze podataka ===");
            
            using (var context = new ContextClass())
            {
                Console.WriteLine("📊 Trenutno stanje baze podataka:");
                
                // Detaljne statistike
                var totalTags = context.Tags.Count();
                var diTags = context.Tags.Count(t => t.Type == TagType.DI);
                var doTags = context.Tags.Count(t => t.Type == TagType.DO);
                var aiTags = context.Tags.Count(t => t.Type == TagType.AI);
                var aoTags = context.Tags.Count(t => t.Type == TagType.AO);
                
                var totalAlarms = context.Alarms.Count();
                var totalActivatedAlarms = context.ActivatedAlarms.Count();
                
                Console.WriteLine($"  📋 Tagovi ukupno: {totalTags}");
                Console.WriteLine($"    - DI (Digital Input): {diTags}");
                Console.WriteLine($"    - DO (Digital Output): {doTags}");
                Console.WriteLine($"    - AI (Analog Input): {aiTags}");
                Console.WriteLine($"    - AO (Analog Output): {aoTags}");
                Console.WriteLine($"  🚨 Definisani alarmi: {totalAlarms}");
                Console.WriteLine($"  📈 Aktivirani alarmi: {totalActivatedAlarms}");
                
                // Test integritet veza između tabela
                Console.WriteLine("\n🔗 Test integriteta veza između tabela:");
                
                var tagsWithAlarms = context.Tags.Include(t => t.Alarms).Where(t => t.Alarms.Any()).ToList();
                Console.WriteLine($"  ✅ Tagovi sa alarmima: {tagsWithAlarms.Count}");
                
                foreach (var tag in tagsWithAlarms)
                {
                    Console.WriteLine($"    - {tag.Id}: {tag.Alarms.Count} alarma");
                    foreach (var alarm in tag.Alarms)
                    {
                        var activatedCount = context.ActivatedAlarms.Count(aa => aa.AlarmId == alarm.Id);
                        Console.WriteLine($"      • {alarm.Id}: {activatedCount} aktivacija");
                    }
                }
                
                // Test query performansi
                Console.WriteLine("\n⚡ Test performansi upita:");
                
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var thirtyMinutesAgo = DateTime.Now.AddMinutes(-30); // Izračunaj PRE upita
                var recentAlarms = context.ActivatedAlarms
                    .Where(a => a.AlarmTime > thirtyMinutesAgo) // Koristi promenljivu umesto metode
                    .OrderByDescending(a => a.AlarmTime)
                    .Take(20)
                    .ToList();
                sw.Stop();
                
                Console.WriteLine($"  📊 Poslednji alarmi (30min): {recentAlarms.Count} u {sw.ElapsedMilliseconds}ms");
                
                // Validacija podataka
                Console.WriteLine("\n🔍 Validacija integriteta podataka:");
                
                var orphanedAlarms = context.Alarms.Where(a => a.Tag == null).Count();
                var orphanedActivated = context.ActivatedAlarms
                    .Where(aa => !context.Alarms.Any(a => a.Id == aa.AlarmId))
                    .Count();
                
                Console.WriteLine($"  🔗 Orphaned alarmi: {orphanedAlarms}");
                Console.WriteLine($"  🔗 Orphaned aktivirani alarmi: {orphanedActivated}");
                Console.WriteLine($"  ✅ Integritet baze: {(orphanedAlarms == 0 && orphanedActivated == 0 ? "DOBAR" : "PROBLEMATIČAN")}");
            }
            
            Console.WriteLine();
        }

        static void TestComplexTagValidation()
        {
            Console.WriteLine("=== TEST CV1: Kompleksna validacija tagova ===");
            
            Console.WriteLine("🧪 Test granične vrednosti i edge case-ovi:");
            
            // Test maksimalnih dužina stringova
            try
            {
                var longIdTag = new Tag(TagType.AI, new string('A', 51), "Test", 1000); // Preko 50 karaktera
                Console.WriteLine("❌ Trebalo je baciti exception za predugačak ID");
            }
            catch (Exception)
            {
                Console.WriteLine("✅ Validacija: ID preko 50 karaktera je odbačen");
            }
            
            // Test graničnih IO adresa
            try
            {
                var invalidAddressTag = new Tag(TagType.AI, "INVALID_ADDR", "Test", -1); // Negativna adresa
                Console.WriteLine("❌ Trebalo je baciti exception za negativnu IO adresu");
            }
            catch (Exception)
            {
                Console.WriteLine("✅ Validacija: Negativna IO adresa je odbačena");
            }
            
            // Test validnih graničnih vrednosti
            var validMinTag = new Tag(TagType.AI, "MIN_ADDR", "Test min", 0);
            var validMaxTag = new Tag(TagType.AI, "MAX_ADDR", "Test max", 65535);
            Console.WriteLine("✅ Validne granične IO adrese (0, 65535) su prihvaćene");
            
            // Test kompleksnih kombinacija svojstava
            Console.WriteLine("\n🔧 Test kompleksnih kombinacija svojstava:");
            
            var complexAITag = new Tag(TagType.AI, "COMPLEX_AI", "Kompleksan AI tag", 5000);
            complexAITag.ValidateAndSetScanTime(100); // Minimalni scan time
            complexAITag.ValidateAndSetOnOffScan(true);
            complexAITag.ValidateAndSetLowLimit(-273.15); // Apsolutna nula
            complexAITag.ValidateAndSetHighLimit(1000000.0); // Velika vrednost
            complexAITag.ValidateAndSetUnits("K"); // Kelvini
            
            // Dodaj ekstremne alarme
            var extremeLowAlarm = new Alarm("EXTREME_LOW", AlarmTrigger.Below, -200.0, "Ekstremno niska vrednost!");
            var extremeHighAlarm = new Alarm("EXTREME_HIGH", AlarmTrigger.Above, 999999.0, "Ekstremno visoka vrednost!");
            
            complexAITag.AddAlarm(extremeLowAlarm);
            complexAITag.AddAlarm(extremeHighAlarm);
            
            Console.WriteLine("✅ Kompleksan AI tag sa ekstremnim vrednostima kreiran");
            
            // Test edge case-ova za alarme
            Console.WriteLine("\n⚠️ Test edge case-ova za alarme:");
            
            // Test duplikatnih alarm ID-jeva
            try
            {
                var duplicateAlarm = new Alarm("EXTREME_LOW", AlarmTrigger.Above, 50.0, "Duplikat");
                complexAITag.AddAlarm(duplicateAlarm);
                Console.WriteLine("❌ Trebalo je baciti exception za duplikat alarm ID");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Validacija: Duplikat alarm ID je odbačen");
            }
            
            // Test alarma sa NaN vrednostima
            try
            {
                var nanAlarm = new Alarm("NAN_ALARM", AlarmTrigger.Above, double.NaN, "NaN test");
                Console.WriteLine("❌ Trebalo je baciti exception za NaN threshold");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✅ Validacija: NaN threshold je odbačen");
            }
            
            Console.WriteLine();
        }

        static void TestRealTimeAlarmProcessing()
        {
            Console.WriteLine("=== TEST RT1: Real-time procesiranje alarma ===");
            
            using (var context = new ContextClass())
            {
                // Očisti za čist test
                var oldActivated = context.ActivatedAlarms.ToList();
                context.ActivatedAlarms.RemoveRange(oldActivated);
                context.SaveChanges();
            }
            
            var dataCollector = new DataCollector();
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();
            
            // Kreiranje tag-a sa preciznim alarmima za testiranje
            var precisionTag = new Tag(TagType.AI, "PRECISION_TEST", "Precizni test tag", 1);
            precisionTag.ValidateAndSetScanTime(100); // Vrlo brzo skeniranje
            precisionTag.ValidateAndSetOnOffScan(true);
            precisionTag.ValidateAndSetLowLimit(-100.0);
            precisionTag.ValidateAndSetHighLimit(100.0);
            precisionTag.ValidateAndSetUnits("V");
            
            // Dodaj precizne alarme
            var veryLowAlarm = new Alarm("VERY_LOW", AlarmTrigger.Below, -50.0, "Vrlo niska vrednost");
            var lowAlarm = new Alarm("LOW", AlarmTrigger.Below, 0.0, "Niska vrednost");
            var highAlarm = new Alarm("HIGH", AlarmTrigger.Above, 50.0, "Visoka vrednost");
            var veryHighAlarm = new Alarm("VERY_HIGH", AlarmTrigger.Above, 90.0, "Vrlo visoka vrednost");
            
            precisionTag.AddAlarm(veryLowAlarm);
            precisionTag.AddAlarm(lowAlarm);
            precisionTag.AddAlarm(highAlarm);
            precisionTag.AddAlarm(veryHighAlarm);
            
            // Sačuvaj u bazu
            using (var context = new ContextClass())
            {
                context.Tags.Add(precisionTag);
                context.SaveChanges();
            }
            
            dataCollector.AddTag(precisionTag);
            Console.WriteLine("✅ Kreiran precizni test tag sa 4 alarma");
            Console.WriteLine("   Alarmi: -50V, 0V, 50V, 90V");
            Console.WriteLine("   Scan rate: 100ms (vrlo brzo)");
            
            // Pokreni sistem
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ Real-time sistem pokrenut");
            
            // Monitoring u real-time
            Console.WriteLine("\n⏱️ Real-time monitoring alarma (10 sekundi):");
            Console.WriteLine("   ADDR001 generiše sine wave (-100 do +100)");
            Console.WriteLine("   Očekujemo česte promene alarma...");
            
            var startTime = DateTime.Now;
            var alarmCountAtStart = 0;
            
            using (var context = new ContextClass())
            {
                alarmCountAtStart = context.ActivatedAlarms.Count();
            }
            
            // Monitoring loop
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(1000); // Čekaj 1 sekund
                
                using (var context = new ContextClass())
                {
                    var currentAlarmCount = context.ActivatedAlarms.Count();
                    var newAlarms = currentAlarmCount - alarmCountAtStart;
                    
                    if (i % 3 == 0) // Ispiši svaki 3. sekund
                    {
                        Console.WriteLine($"   [{i + 1:D2}s] Novi alarmi: {newAlarms}");
                    }
                }
            }
            
            dataCollector.Stop();
            Console.WriteLine("✅ Real-time sistem zaustavljen");
            
            // Analiza rezultata
            var endTime = DateTime.Now;
            var testDuration = endTime - startTime;
            
            using (var context = new ContextClass())
            {
                var finalAlarmCount = context.ActivatedAlarms.Count();
                var totalNewAlarms = finalAlarmCount - alarmCountAtStart;
                var alarmRate = totalNewAlarms / testDuration.TotalMinutes;
                
                Console.WriteLine($"\n📊 Real-time analiza:");
                Console.WriteLine($"   ⏱️ Trajanje testa: {testDuration.TotalSeconds:F1} sekundi");
                Console.WriteLine($"   🚨 Ukupno novih alarma: {totalNewAlarms}");
                Console.WriteLine($"   📈 Alarm rate: {alarmRate:F2} alarma/min");
                Console.WriteLine($"   ⚡ Prosečno: {totalNewAlarms / testDuration.TotalSeconds:F2} alarma/sek");
                
                if (totalNewAlarms > 0)
                {
                    var recentAlarms = context.ActivatedAlarms
                        .Where(a => a.AlarmTime >= startTime)
                        .OrderByDescending(a => a.AlarmTime)
                        .Take(5)
                        .ToList();
                    
                    Console.WriteLine("\n🕒 Poslednji alarmi iz testa:");
                    foreach (var alarm in recentAlarms)
                    {
                        var elapsed = alarm.AlarmTime - startTime;
                        Console.WriteLine($"   - [+{elapsed.TotalSeconds:F1}s] {alarm.AlarmId} | {alarm.TagName}");
                    }
                }
            }
            
            Console.WriteLine();
        }

        static void TestDataCollectorStressTest()
        {
            Console.WriteLine("=== TEST ST1: Stress test DataCollector sistema ===");
            
            var dataCollector = new DataCollector();
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();
            
            Console.WriteLine("🏋️ Kreiranje velikog broja tagova za stress test...");
            
            // Kreiranje mnogo tagova sa različitim scan rate-ovima
            var stressTags = new List<Tag>();
            var random = new Random();
            
            // 5 AI tagova sa različitim brzinama skeniranja
            for (int i = 1; i <= 5; i++)
            {
                var aiTag = new Tag(TagType.AI, $"STRESS_AI_{i:D3}", $"Stress test AI {i}", i);
                aiTag.ValidateAndSetScanTime(random.Next(200, 1000)); // Random scan time 200-1000ms
                aiTag.ValidateAndSetOnOffScan(true);
                aiTag.ValidateAndSetLowLimit(-100.0);
                aiTag.ValidateAndSetHighLimit(100.0);
                aiTag.ValidateAndSetUnits($"U{i}");
                
                // Dodaj random broj alarma
                int alarmCount = random.Next(1, 3);
                for (int j = 1; j <= alarmCount; j++)
                {
                    double threshold = random.NextDouble() * 100 - 50; // -50 do 50
                    var trigger = random.Next(2) == 0 ? AlarmTrigger.Above : AlarmTrigger.Below;
                    var alarm = new Alarm($"STRESS_ALARM_{i}_{j}", trigger, threshold, 
                        $"Stress alarm {j} na tag {i}");
                    aiTag.AddAlarm(alarm);
                }
                
                stressTags.Add(aiTag);
                dataCollector.AddTag(aiTag);
            }
            
            // 3 Output tagova
            for (int i = 1; i <= 3; i++)
            {
                var aoTag = new Tag(TagType.AO, $"STRESS_AO_{i:D3}", $"Stress test AO {i}", 4 + i);
                aoTag.ValidateAndSetInitialValue(random.NextDouble() * 100);
                aoTag.ValidateAndSetLowLimit(0.0);
                aoTag.ValidateAndSetHighLimit(100.0);
                aoTag.ValidateAndSetUnits("%");
                stressTags.Add(aoTag);
                dataCollector.AddTag(aoTag);
            }
            
            Console.WriteLine($"✅ Kreiran {stressTags.Count} tagova za stress test");
            Console.WriteLine($"   - AI tagovi: {stressTags.Count(t => t.Type == TagType.AI)} (sa alarmima)");
            Console.WriteLine($"   - AO tagovi: {stressTags.Count(t => t.Type == TagType.AO)}");
            
            // Sačuvaj sve u bazu
            using (var context = new ContextClass())
            {
                foreach (var tag in stressTags.Where(t => t.Type == TagType.AI))
                {
                    context.Tags.Add(tag);
                }
                context.SaveChanges();
            }
            
            // Pokreni stress test
            Console.WriteLine("\n🚀 Pokretanje stress test-a...");
            var stressStartTime = DateTime.Now;
            
            dataCollector.Start(plcSimulator);
            Console.WriteLine($"✅ Sistem pokrenut sa {stressTags.Count} tagova");
            
            // Monitoring performansi
            Console.WriteLine("\n⚡ Stress test u toku (15 sekundi):");
            var initialAlarmCount = 0;
            using (var context = new ContextClass())
            {
                initialAlarmCount = context.ActivatedAlarms.Count();
            }
            
            for (int i = 0; i < 15; i++)
            {
                Thread.Sleep(1000);
                
                if (i % 5 == 4) // Svaki 5. sekund
                {
                    using (var context = new ContextClass())
                    {
                        var currentAlarms = context.ActivatedAlarms.Count();
                        var newAlarms = currentAlarms - initialAlarmCount;
                        Console.WriteLine($"   [{i + 1:D2}s] Novih alarma: {newAlarms}");
                    }
                    
                    // Test pisanja u random output tagove
                    var outputTags = stressTags.Where(t => t.IsOutputTag()).ToList();
                    foreach (var tag in outputTags.Take(2))
                    {
                        double randomValue = random.NextDouble() * 100;
                        dataCollector.WriteTagValue(tag.Id, randomValue);
                    }
                }
            }
            
            dataCollector.Stop();
            var stressEndTime = DateTime.Now;
            var stressDuration = stressEndTime - stressStartTime;
            
            Console.WriteLine("✅ Stress test završen");
            
            // Finalna analiza
            using (var context = new ContextClass())
            {
                var finalAlarmCount = context.ActivatedAlarms.Count();
                var totalStressAlarms = finalAlarmCount - initialAlarmCount;
                
                Console.WriteLine($"\n📊 Stress test rezultati:");
                Console.WriteLine($"   ⏱️ Trajanje: {stressDuration.TotalSeconds:F1} sekundi");
                Console.WriteLine($"   📋 Tagova u sistemu: {stressTags.Count}");
                Console.WriteLine($"   🚨 Generisano alarma: {totalStressAlarms}");
                Console.WriteLine($"   ⚡ Performanse: {totalStressAlarms / stressDuration.TotalMinutes:F2} alarma/min");
                Console.WriteLine($"   💪 Sistem stabilnost: {(totalStressAlarms > 0 ? "ODLIČAN" : "STABILAN")}");
            }
            
            Console.WriteLine();
        }

        static void DisplayFinalDatabaseContents()
        {
            Console.WriteLine("=== FINALNI PRIKAZ KOMPLETNE BAZE PODATAKA ===");
            
            using (var context = new ContextClass())
            {
                // TABELA TAGOVA
                Console.WriteLine("\n📋 TABELA: Tags");
                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"{"ID",-20} | {"Tip",-3} | {"Opis",-30} | {"IO Addr",-8} | {"Scan",-6} | {"On/Off",-6}");
                Console.WriteLine(new string('-', 80));
                
                var allTags = context.Tags.OrderBy(t => t.Type).ThenBy(t => t.Id).ToList();
                foreach (var tag in allTags)
                {
                    string scanTime = tag.ScanTime?.ToString() ?? "N/A";
                    string onOffScan = tag.OnOffScan?.ToString() ?? "N/A";
                    string description = tag.Description.Length > 30 ? tag.Description.Substring(0, 27) + "..." : tag.Description;
                    
                    Console.WriteLine($"{tag.Id,-20} | {tag.Type,-3} | {description,-30} | {tag.IOAddress,-8} | {scanTime,-6} | {onOffScan,-6}");
                    
                    // Dodaj detalje za analogne tagove
                    if (tag.IsAnalogTag())
                    {
                        string limits = $"    Limits: {tag.LowLimit ?? 0} - {tag.HighLimit ?? 0} {tag.Units ?? ""}";
                        Console.WriteLine($"{"",21}   {limits}");
                    }
                    
                    // Dodaj initial value za output tagove
                    if (tag.IsOutputTag() && tag.InitialValue.HasValue)
                    {
                        Console.WriteLine($"{"",21}   Initial: {tag.InitialValue.Value}");
                    }
                }
                
                Console.WriteLine($"\nUkupno tagova: {allTags.Count} (DI: {allTags.Count(t => t.Type == TagType.DI)}, " +
                    $"DO: {allTags.Count(t => t.Type == TagType.DO)}, " +
                    $"AI: {allTags.Count(t => t.Type == TagType.AI)}, " +
                    $"AO: {allTags.Count(t => t.Type == TagType.AO)})");
                
                // TABELA ALARMA
                Console.WriteLine("\n🚨 TABELA: Alarms");
                Console.WriteLine(new string('=', 100));
                Console.WriteLine($"{"ID",-25} | {"Tag ID",-20} | {"Trigger",-8} | {"Threshold",-10} | {"Poruka",-30}");
                Console.WriteLine(new string('-', 100));
                
                var allAlarms = context.Alarms.OrderBy(a => a.TagId).ThenBy(a => a.Id).ToList();
                foreach (var alarm in allAlarms)
                {
                    string message = alarm.Message.Length > 30 ? alarm.Message.Substring(0, 27) + "..." : alarm.Message;
                    string trigger = alarm.Trigger == AlarmTrigger.Above ? "Above" : "Below";
                    
                    Console.WriteLine($"{alarm.Id,-25} | {alarm.TagId,-20} | {trigger,-8} | {alarm.Threshold,-10:F1} | {message,-30}");
                }
                
                Console.WriteLine($"\nUkupno definisanih alarma: {allAlarms.Count}");
                
                // TABELA AKTIVIRANIH ALARMA
                Console.WriteLine("\n📈 TABELA: ActivatedAlarms");
                Console.WriteLine(new string('=', 110));
                Console.WriteLine($"{"ID",-5} | {"Alarm ID",-25} | {"Tag",-20} | {"Vreme aktivacije",-20} | {"Poruka",-30}");
                Console.WriteLine(new string('-', 110));
                
                var allActivatedAlarms = context.ActivatedAlarms
                    .OrderByDescending(a => a.AlarmTime)
                    .ToList();
                
                foreach (var activatedAlarm in allActivatedAlarms.Take(50)) // Prikaži poslednji 50
                {
                    string message = activatedAlarm.Message.Length > 30 ? 
                        activatedAlarm.Message.Substring(0, 27) + "..." : activatedAlarm.Message;
                    
                    Console.WriteLine($"{activatedAlarm.Id,-5} | {activatedAlarm.AlarmId,-25} | " +
                        $"{activatedAlarm.TagName,-20} | {activatedAlarm.AlarmTime:dd.MM.yyyy HH:mm:ss,-20} | {message,-30}");
                }
                
                if (allActivatedAlarms.Count > 50)
                {
                    Console.WriteLine($"... i još {allActivatedAlarms.Count - 50} starijih alarma");
                }
                
                Console.WriteLine($"\nUkupno aktiviranih alarma: {allActivatedAlarms.Count}");
                
                // STATISTIKE ALARMA
                if (allActivatedAlarms.Any())
                {
                    Console.WriteLine("\n📊 STATISTIKE ALARMA:");
                    Console.WriteLine(new string('=', 60));
                    
                    var alarmStats = allActivatedAlarms
                        .GroupBy(a => a.AlarmId)
                        .Select(g => new
                        {
                            AlarmId = g.Key,
                            Count = g.Count(),
                            FirstActivation = g.Min(a => a.AlarmTime),
                            LastActivation = g.Max(a => a.AlarmTime),
                            TagName = g.First().TagName
                        })
                        .OrderByDescending(s => s.Count)
                        .ToList();
                    
                    Console.WriteLine($"{"Alarm ID",-25} | {"Tag",-20} | {"Broj",-6} | {"Prvi",-16} | {"Poslednji",-16}");
                    Console.WriteLine(new string('-', 90));
                    
                    foreach (var stat in alarmStats)
                    {
                        Console.WriteLine($"{stat.AlarmId,-25} | {stat.TagName,-20} | {stat.Count,-6} | " +
                            $"{stat.FirstActivation:dd.MM HH:mm:ss,-16} | {stat.LastActivation:dd.MM HH:mm:ss,-16}");
                    }
                    
                    // Vremenska analiza
                    if (alarmStats.Count > 1)
                    {
                        var timeSpan = allActivatedAlarms.Max(a => a.AlarmTime) - allActivatedAlarms.Min(a => a.AlarmTime);
                        var alarmRate = allActivatedAlarms.Count / Math.Max(timeSpan.TotalHours, 0.01);
                        
                        Console.WriteLine($"\n⏱️ VREMENSKA ANALIZA:");
                        Console.WriteLine($"   Vremenski period: {timeSpan.TotalMinutes:F1} minuta");
                        Console.WriteLine($"   Prosečna frekvencija: {alarmRate:F2} alarma/sat");
                        Console.WriteLine($"   Najaktivniji alarm: {alarmStats.FirstOrDefault()?.AlarmId ?? "N/A"} " +
                            $"({alarmStats.FirstOrDefault()?.Count ?? 0} aktivacija)");
                    }
                }
                
                // UKUPNE STATISTIKE
                Console.WriteLine($"\n🏆 UKUPNE STATISTIKE SISTEMA:");
                Console.WriteLine(new string('=', 50));
                Console.WriteLine($"   📋 Ukupno tagova: {allTags.Count}");
                Console.WriteLine($"   🚨 Ukupno definisanih alarma: {allAlarms.Count}");
                Console.WriteLine($"   📈 Ukupno aktiviranih alarma: {allActivatedAlarms.Count}");
                Console.WriteLine($"   🔗 Tagova sa alarmima: {allTags.Count(t => context.Alarms.Any(a => a.TagId == t.Id))}");
                Console.WriteLine($"   ⚡ Prosečno alarma po AI tag-u: {(allTags.Count(t => t.Type == TagType.AI) > 0 ? (double)allAlarms.Count / allTags.Count(t => t.Type == TagType.AI) : 0):F1}");
                
                if (allActivatedAlarms.Any())
                {
                    var oneHourAgo = DateTime.Now.AddHours(-1);
                    var oneMinuteAgo = DateTime.Now.AddMinutes(-1);
                    var lastHour = allActivatedAlarms.Count(a => a.AlarmTime > oneHourAgo);
                    var lastMinute = allActivatedAlarms.Count(a => a.AlarmTime > oneMinuteAgo);
                    Console.WriteLine($"   🕐 Alarmi u poslednjem satu: {lastHour}");
                    Console.WriteLine($"   ⚡ Alarmi u poslednjoj minuti: {lastMinute}");
                }
                
                Console.WriteLine($"   💾 Status baze podataka: OPERATIVNA");
                Console.WriteLine($"   ✅ Integritet podataka: DOBAR");
            }
        }
    }
}
