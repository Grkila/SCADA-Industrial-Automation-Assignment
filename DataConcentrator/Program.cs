using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataConcentrator
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
    }
}
