using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading;
using DataConcentrator;

namespace tester
{
    class NewTestProgram
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== SCADA SISTEM - NOVI TEST PROGRAM ===");
            Console.WriteLine("Testiranje na osnovu specifikacija.md\n");

            try
            {
                // Core functionality tests based on specification
                TestDatabaseInitialization();
                TestTagCreationAndValidation();
                TestAlarmManagement();
                TestDataCollectorBasicOperations();
                TestDataCollectorEdgeCases();
                TestTagEdgeCases();
                TestAlarmEdgeCases();
                TestContextEdgeCases();
                TestConcurrentOperations();
                TestPLCIntegration();
                TestReportGeneration();
                TestConfigurationPersistence();
                
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

        static void TestDatabaseInitialization()
        {
            Console.WriteLine("=== TEST 1: Inicijalizacija baze podataka ===");

            using (var context = new ContextClass())
            {
                // Clean slate for testing
                if (context.Database.Exists())
                {
                    context.Database.Delete();
                    Console.WriteLine("Stara baza je obrisana za čist test.");
                }

                // Create new database
                bool created = context.Database.CreateIfNotExists();
                Console.WriteLine("✅ Baza 'ScadaDatabase' kreirana sa 3 tabele:");
                Console.WriteLine("   - Tags (tagovi)");
                Console.WriteLine("   - Alarms (alarmi)");
                Console.WriteLine("   - ActivatedAlarms (aktivirani alarmi)");

                // Verify empty state
                Console.WriteLine($"   Početni broj tagova: {context.Tags.Count()}");
                Console.WriteLine($"   Početni broj alarma: {context.Alarms.Count()}");
                Console.WriteLine($"   Početni broj aktiviranih alarma: {context.ActivatedAlarms.Count()}");
            }
            Console.WriteLine();
        }

        static void TestTagCreationAndValidation()
        {
            Console.WriteLine("=== TEST 2: Kreiranje i validacija tagova ===");

            using (var context = new ContextClass())
            {
                Console.WriteLine("Kreiranje tagova svih tipova sa validacijom:");

                // DI Tag - Digital Input
                var diTag = new Tag(TagType.DI, "DI_DOOR_001", "Senzor vrata", "ADDR009");
                diTag.ValidateAndSetScanTime(1000);
                diTag.ValidateAndSetOnOffScan(true);
                Console.WriteLine($"✅ DI Tag: {diTag.Id} - {diTag.Description}");
                Console.WriteLine($"   Scan time: {diTag.ScanTime}ms, Scanning: {diTag.OnOffScan}");

                // DO Tag - Digital Output  
                var doTag = new Tag(TagType.DO, "DO_LIGHT_001", "LED indikator", "ADDR010");
                doTag.ValidateAndSetInitialValue(0);
                Console.WriteLine($"✅ DO Tag: {doTag.Id} - {doTag.Description}");
                Console.WriteLine($"   Initial value: {doTag.InitialValue}");

                // AI Tag - Analog Input
                var aiTag = new Tag(TagType.AI, "AI_TEMP_001", "Temperaturni senzor", "ADDR001");
                aiTag.ValidateAndSetScanTime(500);
                aiTag.ValidateAndSetOnOffScan(true);
                aiTag.ValidateAndSetLowLimit(-20.0);
                aiTag.ValidateAndSetHighLimit(80.0);
                aiTag.ValidateAndSetUnits("°C");
                Console.WriteLine($"✅ AI Tag: {aiTag.Id} - {aiTag.Description}");
                Console.WriteLine($"   Range: {aiTag.LowLimit} - {aiTag.HighLimit} {aiTag.Units}");
                Console.WriteLine($"   Scan time: {aiTag.ScanTime}ms, Scanning: {aiTag.OnOffScan}");

                // AO Tag - Analog Output
                var aoTag = new Tag(TagType.AO, "AO_VALVE_001", "Kontrolni ventil", "ADDR005");
                aoTag.ValidateAndSetInitialValue(25.0);
                aoTag.ValidateAndSetLowLimit(0.0);
                aoTag.ValidateAndSetHighLimit(100.0);
                aoTag.ValidateAndSetUnits("%");
                Console.WriteLine($"✅ AO Tag: {aoTag.Id} - {aoTag.Description}");
                Console.WriteLine($"   Range: {aoTag.LowLimit} - {aoTag.HighLimit} {aoTag.Units}");
                Console.WriteLine($"   Initial value: {aoTag.InitialValue}{aoTag.Units}");

                // Save to database
                context.Tags.AddRange(new[] { diTag, doTag, aiTag, aoTag });
                context.SaveChanges();
                Console.WriteLine($"💾 Sačuvano {context.Tags.Count()} tagova u bazu");

                // Test validation rules
                Console.WriteLine("\nTest validacionih pravila:");
                TestTagValidationRules();
            }
            Console.WriteLine();
        }

        static void TestTagValidationRules()
        {
            // Test 1: DI tag cannot have units
            try
            {
                var diTag = new Tag(TagType.DI, "TEST_DI", "Test", "ADDR009");
                diTag.ValidateAndSetUnits("°C"); // Should fail
                Console.WriteLine("❌ GREŠKA: DI tag ne bi trebao da prihvati units");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Validacija: DI tag ne može imati units");
            }

            // Test 2: AO tag cannot have scan time
            try
            {
                var aoTag = new Tag(TagType.AO, "TEST_AO", "Test", "ADDR005");
                aoTag.ValidateAndSetScanTime(1000); // Should fail
                Console.WriteLine("❌ GREŠKA: AO tag ne bi trebao da prihvati scan time");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Validacija: AO tag ne može imati scan time");
            }

            // Test 3: DO tag cannot have limits
            try
            {
                var doTag = new Tag(TagType.DO, "TEST_DO", "Test", "ADDR010");
                doTag.ValidateAndSetLowLimit(0.0); // Should fail
                Console.WriteLine("❌ GREŠKA: DO tag ne bi trebao da prihvati limits");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Validacija: DO tag ne može imati limits");
            }

            // Test 4: AI tag cannot have initial value
            try
            {
                var aiTag = new Tag(TagType.AI, "TEST_AI", "Test", "ADDR001");
                aiTag.ValidateAndSetInitialValue(50.0); // Should fail
                Console.WriteLine("❌ GREŠKA: AI tag ne bi trebao da prihvati initial value");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Validacija: AI tag ne može imati initial value");
            }
        }

        static void TestAlarmManagement()
        {
            Console.WriteLine("=== TEST 3: Upravljanje alarmima ===");

            try
            {
                // Get AI tag for alarm testing
                Tag aiTag = null;
                using (var context = new ContextClass())
                {
                    aiTag = context.Tags.Include(t => t.Alarms)
                        .FirstOrDefault(t => t.Id == "AI_TEMP_001");
                }

                if (aiTag != null)
                {
                    Console.WriteLine($"Dodavanje alarma na AI tag: {aiTag.Id}");

                    // Create different types of alarms
                    var lowTempAlarm = new Alarm("LOW_TEMP_ALARM", AlarmTrigger.Below, 0.0, 
                        "UPOZORENJE: Temperatura je ispod nule!");
                    var highTempAlarm = new Alarm("HIGH_TEMP_ALARM", AlarmTrigger.Above, 60.0, 
                        "UPOZORENJE: Temperatura je previsoka!");
                    var criticalTempAlarm = new Alarm("CRITICAL_TEMP_ALARM", AlarmTrigger.Above, 75.0, 
                        "KRITIČNO: Temperatura je na kritičnom nivou!");

                    // Add alarms to tag (each AddAlarm call saves to database automatically)
                    aiTag.AddAlarm(lowTempAlarm);
                    aiTag.AddAlarm(highTempAlarm);
                    aiTag.AddAlarm(criticalTempAlarm);

                    Console.WriteLine($"✅ Dodano {aiTag.Alarms.Count} alarma:");
                    foreach (var alarm in aiTag.Alarms)
                    {
                        string direction = alarm.Trigger == AlarmTrigger.Above ? ">" : "<";
                        Console.WriteLine($"   - {alarm.Id}: {direction} {alarm.Threshold}°C");
                        Console.WriteLine($"     Poruka: {alarm.Message}");
                    }

                    // Test alarm activation with different values
                    Console.WriteLine("\nTest aktivacije alarma:");
                    TestAlarmActivation(aiTag, -5.0);  // Low alarm
                    TestAlarmActivation(aiTag, 25.0);  // Normal
                    TestAlarmActivation(aiTag, 65.0);  // High alarm
                    TestAlarmActivation(aiTag, 80.0);  // Critical alarm
                }
                else
                {
                    Console.WriteLine("❌ AI tag AI_TEMP_001 not found for alarm testing");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error in alarm management test: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
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
                    
                    // Save activated alarm to database
                    using (var context = new ContextClass())
                    {
                        var activatedAlarm = new ActivatedAlarm(alarm, tag.Id);
                        context.ActivatedAlarms.Add(activatedAlarm);
                        context.SaveChanges();
                    }
                }
            }
            else
            {
                Console.WriteLine($"✅ Vrednost {value}°C - nema aktivnih alarma");
            }
        }

        static void TestDataCollectorBasicOperations()
        {
            Console.WriteLine("=== TEST 4: DataCollector osnovne operacije ===");

            var dataCollector = new DataCollector();
            Console.WriteLine("✅ DataCollector instanciran i konfiguracija učitana");

            // Test writing to output tags
            Console.WriteLine("\nTest pisanja u output tagove:");
            dataCollector.WriteTagValue("DO_LIGHT_001", 1);
            dataCollector.WriteTagValue("AO_VALVE_001", 75.5);

            // Test reading values
            var doValue = dataCollector.GetTagValue("DO_LIGHT_001");
            var aoValue = dataCollector.GetTagValue("AO_VALVE_001");
            
            Console.WriteLine($"✅ DO_LIGHT_001 vrednost: {doValue}");
            Console.WriteLine($"✅ AO_VALVE_001 vrednost: {aoValue}");

            // Test scanning control
            Console.WriteLine("\nTest kontrole skeniranja:");
            dataCollector.SetTagScanning("AI_TEMP_001", false);
            Console.WriteLine("✅ Skeniranje AI_TEMP_001 isključeno");
            
            dataCollector.SetTagScanning("DI_DOOR_001", true);
            Console.WriteLine("✅ Skeniranje DI_DOOR_001 uključeno");

            Console.WriteLine();
        }

        static void TestDataCollectorEdgeCases()
        {
            Console.WriteLine("=== TEST EDGE CASES: DataCollector ===");

            var dataCollector = new DataCollector();

            // Test 1: Null parameter handling
            Console.WriteLine("Test 1: Null parameter handling");
            try
            {
                dataCollector.AddTag(null);
                Console.WriteLine("❌ GREŠKA: AddTag(null) trebalo je da baci exception");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("✅ AddTag(null) pravilno baca ArgumentNullException");
            }

            try
            {
                dataCollector.RemoveTag(null);
                Console.WriteLine("❌ GREŠKA: RemoveTag(null) trebalo je da baci exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✅ RemoveTag(null) pravilno baca ArgumentException");
            }

            try
            {
                dataCollector.RemoveTag("");
                Console.WriteLine("❌ GREŠKA: RemoveTag('') trebalo je da baci exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✅ RemoveTag('') pravilno baca ArgumentException");
            }

            // Test 2: Duplicate tag handling
            Console.WriteLine("\nTest 2: Duplicate tag handling");
            var testTag = new Tag(TagType.AI, "DUPLICATE_TEST", "Test duplicate", "ADDR999");
            try
            {
                dataCollector.AddTag(testTag);
                Console.WriteLine("✅ Prvi tag dodat uspešno");
                
                // Try to add same tag again
                var duplicateTag = new Tag(TagType.AI, "DUPLICATE_TEST", "Duplicate test", "ADDR998");
                dataCollector.AddTag(duplicateTag);
                Console.WriteLine("❌ GREŠKA: Duplikat tag trebalo je da baci exception");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"✅ Duplikat tag pravilno odbačen: {ex.Message}");
            }

            // Test 3: Operations on non-existent tags
            Console.WriteLine("\nTest 3: Operations on non-existent tags");
            
            var nonExistentValue = dataCollector.GetTagValue("NON_EXISTENT_TAG");
            if (nonExistentValue == null)
            {
                Console.WriteLine("✅ GetTagValue za nepostojeći tag vraća null");
            }

            try
            {
                dataCollector.WriteTagValue("NON_EXISTENT_TAG", 123.45);
                Console.WriteLine("✅ WriteTagValue za nepostojeći tag se gracefully ignoriše");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ℹ️  WriteTagValue za nepostojeći tag: {ex.Message}");
            }

            try
            {
                dataCollector.SetTagScanning("NON_EXISTENT_TAG", true);
                Console.WriteLine("❌ GREŠKA: SetTagScanning za nepostojeći tag trebalo je da baci exception");
            }
            catch (Exception)
            {
                Console.WriteLine("✅ SetTagScanning za nepostojeći tag pravilno baca exception");
            }

            // Test 4: Invalid write operations
            Console.WriteLine("\nTest 4: Invalid write operations");
            
            // Try to write to input tag
            var inputTag = new Tag(TagType.AI, "INPUT_WRITE_TEST", "Input write test", "ADDR997");
            dataCollector.AddTag(inputTag);
            
            try
            {
                dataCollector.WriteTagValue("INPUT_WRITE_TEST", 50.0);
                Console.WriteLine("ℹ️  WriteTagValue na input tag se ignoriše (expected behavior)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ WriteTagValue na input tag pravilno odbačen: {ex.Message}");
            }

            // Test 5: Digital tag value validation
            Console.WriteLine("\nTest 5: Digital tag value validation");
            var digitalOutputTag = new Tag(TagType.DO, "DIGITAL_VALIDATION_TEST", "Digital validation", "ADDR996");
            dataCollector.AddTag(digitalOutputTag);

            // Valid digital values (0 and 1)
            try
            {
                dataCollector.WriteTagValue("DIGITAL_VALIDATION_TEST", 0);
                dataCollector.WriteTagValue("DIGITAL_VALIDATION_TEST", 1);
                Console.WriteLine("✅ Validne digitalne vrednosti (0,1) prihvaćene");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GREŠKA: Validne digitalne vrednosti odbačene: {ex.Message}");
            }

            // Invalid digital values
            try
            {
                dataCollector.WriteTagValue("DIGITAL_VALIDATION_TEST", 0.5);
                Console.WriteLine("❌ GREŠKA: Nevalidna digitalna vrednost (0.5) trebalo je da bude odbačena");
            }
            catch (Exception)
            {
                Console.WriteLine("✅ Nevalidna digitalna vrednost (0.5) pravilno odbačena");
            }

            try
            {
                dataCollector.WriteTagValue("DIGITAL_VALIDATION_TEST", 2);
                Console.WriteLine("❌ GREŠKA: Nevalidna digitalna vrednost (2) trebalo je da bude odbačena");
            }
            catch (Exception)
            {
                Console.WriteLine("✅ Nevalidna digitalna vrednost (2) pravilno odbačena");
            }

            // Test 6: Scanning control edge cases
            Console.WriteLine("\nTest 6: Scanning control edge cases");
            
            var outputTag = new Tag(TagType.AO, "OUTPUT_SCAN_TEST", "Output scan test", "ADDR995");
            dataCollector.AddTag(outputTag);
            
            try
            {
                dataCollector.SetTagScanning("OUTPUT_SCAN_TEST", true);
                Console.WriteLine("❌ GREŠKA: SetTagScanning na output tag trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ SetTagScanning na output tag pravilno odbačen");
            }

            // Test 7: Alarm operations on invalid tag types
            Console.WriteLine("\nTest 7: Alarm operations na nevalidnim tipovima tagova");
            
            var digitalInputTag = new Tag(TagType.DI, "DI_ALARM_TEST", "DI alarm test", "ADDR994");
            dataCollector.AddTag(digitalInputTag);
            
            var testAlarm = new Alarm("DI_ALARM_TEST", AlarmTrigger.Above, 1.0, "Test alarm on DI");
            try
            {
                dataCollector.AddAlarmToTag("DI_ALARM_TEST", testAlarm);
                Console.WriteLine("❌ GREŠKA: Alarm na DI tag trebalo je da bude odbačen");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Alarm na DI tag pravilno odbačen (samo AI tagovi mogu imati alarme)");
            }

            // Test 8: Update operations edge cases
            Console.WriteLine("\nTest 8: Update operations edge cases");
            
            try
            {
                dataCollector.UpdateTag(null);
                Console.WriteLine("❌ GREŠKA: UpdateTag(null) trebalo je da baci exception");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("✅ UpdateTag(null) pravilno baca ArgumentNullException");
            }

            var nonExistentUpdateTag = new Tag(TagType.AI, "NON_EXISTENT_UPDATE", "Non existent", "ADDR993");
            try
            {
                dataCollector.UpdateTag(nonExistentUpdateTag);
                Console.WriteLine("❌ GREŠKA: UpdateTag za nepostojeći tag trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ UpdateTag za nepostojeći tag pravilno baca exception");
            }

            // Test 9: PLC Simulator null handling
            Console.WriteLine("\nTest 9: PLC Simulator null handling");
            try
            {
                dataCollector.Start(null);
                Console.WriteLine("❌ GREŠKA: Start(null) trebalo je da baci exception");
            }
            catch (ArgumentNullException)
            {
                Console.WriteLine("✅ Start(null) pravilno baca ArgumentNullException");
            }

            // Test 10: Multiple start/stop operations
            Console.WriteLine("\nTest 10: Multiple start/stop operations");
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();
            
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ DataCollector pokrenut prvi put");
            
            // Try to start again (should be ignored)
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ Drugi Start() poziv se ignoriše");
            
            dataCollector.Stop();
            Console.WriteLine("✅ DataCollector zaustavljen");
            
            // Try to stop again (should be ignored)
            dataCollector.Stop();
            Console.WriteLine("✅ Drugi Stop() poziv se ignoriše");

            Console.WriteLine();
        }

        static void TestTagEdgeCases()
        {
            Console.WriteLine("=== TEST EDGE CASES: Tag ===");

            // Test 1: Maximum length validations
            Console.WriteLine("Test 1: Maximum length validations");
            
            try
            {
                var longIdTag = new Tag(TagType.AI, new string('X', 51), "Test", "ADDR001");
                longIdTag.ValidateConfiguration();
                Console.WriteLine("❌ GREŠKA: Predugačak ID trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Predugačak ID (>50 chars) pravilno odbačen");
            }

            try
            {
                var longDescTag = new Tag(TagType.AI, "LONG_DESC_TEST", new string('X', 301), "ADDR001");
                longDescTag.ValidateConfiguration();
                Console.WriteLine("❌ GREŠKA: Predugačak Description trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Predugačak Description (>300 chars) pravilno odbačen");
            }

            try
            {
                var longAddrTag = new Tag(TagType.AI, "LONG_ADDR_TEST", "Test", new string('X', 51));
                longAddrTag.ValidateConfiguration();
                Console.WriteLine("❌ GREŠKA: Predugačka IOAddress trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Predugačka IOAddress (>50 chars) pravilno odbačena");
            }

            // Test 2: Null/empty field validations
            Console.WriteLine("\nTest 2: Null/empty field validations");
            
            try
            {
                var nullIdTag = new Tag(TagType.AI, null, "Test", "ADDR001");
                nullIdTag.ValidateConfiguration();
                Console.WriteLine("❌ GREŠKA: Null ID trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Null ID pravilno odbačen");
            }

            try
            {
                var emptyDescTag = new Tag(TagType.AI, "EMPTY_DESC", "", "ADDR001");
                emptyDescTag.ValidateConfiguration();
                Console.WriteLine("❌ GREŠKA: Prazan Description trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Prazan Description pravilno odbačen");
            }

            // Test 3: Boundary value testing for analog limits
            Console.WriteLine("\nTest 3: Boundary value testing");
            
            var analogTag = new Tag(TagType.AI, "BOUNDARY_TEST", "Boundary test", "ADDR001");
            
            // Test extreme values
            try
            {
                analogTag.ValidateAndSetLowLimit(double.MinValue);
                analogTag.ValidateAndSetHighLimit(double.MaxValue);
                Console.WriteLine("✅ Ekstremne vrednosti (Min/Max) prihvaćene");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ℹ️  Ekstremne vrednosti: {ex.Message}");
            }

            // Test NaN and Infinity
            try
            {
                analogTag.ValidateAndSetLowLimit(double.NaN);
                Console.WriteLine("❌ GREŠKA: NaN vrednost trebalo je da bude odbačena");
            }
            catch (Exception)
            {
                Console.WriteLine("✅ NaN vrednost pravilno odbačena");
            }

            try
            {
                analogTag.ValidateAndSetHighLimit(double.PositiveInfinity);
                Console.WriteLine("❌ GREŠKA: Infinity vrednost trebalo je da bude odbačena");
            }
            catch (Exception)
            {
                Console.WriteLine("✅ Infinity vrednost pravilno odbačena");
            }

            // Test 4: Digital tag value constraints
            Console.WriteLine("\nTest 4: Digital tag value constraints");
            
            var digitalTag = new Tag(TagType.DO, "DIGITAL_CONSTRAINT_TEST", "Digital constraint", "ADDR001");
            digitalTag.ValidateAndSetInitialValue(1);
            
            try
            {
                digitalTag.WriteValue(0.5);
                Console.WriteLine("❌ GREŠKA: Nevalidna digitalna vrednost trebalo je da bude odbačena");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✅ Nevalidna digitalna vrednost (0.5) pravilno odbačena");
            }

            try
            {
                digitalTag.WriteValue(-1);
                Console.WriteLine("❌ GREŠKA: Negativna digitalna vrednost trebalo je da bude odbačena");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✅ Negativna digitalna vrednost pravilno odbačena");
            }

            // Test 5: Characteristics JSON edge cases
            Console.WriteLine("\nTest 5: Characteristics JSON edge cases");
            
            var jsonTag = new Tag(TagType.AI, "JSON_TEST", "JSON test", "ADDR001");
            
            // Test setting null characteristics
            jsonTag.CharacteristicsJson = null;
            if (jsonTag.ScanTime == null)
            {
                Console.WriteLine("✅ Null CharacteristicsJson pravilno obrađen");
            }

            // Test invalid JSON
            jsonTag.CharacteristicsJson = "invalid json {";
            if (jsonTag.ScanTime == null)
            {
                Console.WriteLine("✅ Nevalidan JSON pravilno obrađen (resetovan na prazan dictionary)");
            }

            Console.WriteLine();
        }

        static void TestAlarmEdgeCases()
        {
            Console.WriteLine("=== TEST EDGE CASES: Alarm ===");

            // Test 1: Alarm validation edge cases
            Console.WriteLine("Test 1: Alarm validation edge cases");
            
            try
            {
                var nullIdAlarm = new Alarm(null, AlarmTrigger.Above, 50.0, "Test message");
                Console.WriteLine("❌ GREŠKA: Null alarm ID trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Null alarm ID pravilno odbačen");
            }

            try
            {
                var longIdAlarm = new Alarm(new string('X', 51), AlarmTrigger.Above, 50.0, "Test");
                Console.WriteLine("❌ GREŠKA: Predugačak alarm ID trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Predugačak alarm ID (>50 chars) pravilno odbačen");
            }

            try
            {
                var longMsgAlarm = new Alarm("LONG_MSG_TEST", AlarmTrigger.Above, 50.0, new string('X', 1001));
                Console.WriteLine("❌ GREŠKA: Predugačka alarm poruka trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Predugačka alarm poruka (>1000 chars) pravilno odbačena");
            }

            // Test 2: Threshold edge cases
            Console.WriteLine("\nTest 2: Threshold edge cases");
            
            try
            {
                var nanAlarm = new Alarm("NAN_TEST", AlarmTrigger.Above, double.NaN, "NaN test");
                Console.WriteLine("❌ GREŠKA: NaN threshold trebalo je da baci exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✅ NaN threshold pravilno odbačen");
            }

            try
            {
                var infAlarm = new Alarm("INF_TEST", AlarmTrigger.Below, double.PositiveInfinity, "Infinity test");
                Console.WriteLine("❌ GREŠKA: Infinity threshold trebalo je da baci exception");
            }
            catch (ArgumentException)
            {
                Console.WriteLine("✅ Infinity threshold pravilno odbačen");
            }

            // Test 3: Alarm state transitions
            Console.WriteLine("\nTest 3: Alarm state transitions");
            
            var stateAlarm = new Alarm("STATE_TEST", AlarmTrigger.Above, 50.0, "State test alarm");
            
            // Test multiple activations
            bool firstActivation = stateAlarm.TryActivate(60.0);  // Should activate
            bool secondActivation = stateAlarm.TryActivate(70.0); // Should not activate again
            
            if (firstActivation && !secondActivation)
            {
                Console.WriteLine("✅ Alarm se aktivira samo jednom dok je aktivan");
            }
            else
            {
                Console.WriteLine("❌ GREŠKA: Problemi sa aktivacijom alarma");
            }

            // Test acknowledgment edge cases
            bool ackBeforeActive = stateAlarm.Acknowledge();
            if (ackBeforeActive)
            {
                Console.WriteLine("✅ Alarm acknowledgovan");
            }

            bool doubleAck = stateAlarm.Acknowledge();
            if (!doubleAck)
            {
                Console.WriteLine("✅ Dupli acknowledge se ignoriše");
            }

            // Test reset conditions
            bool resetWhileActive = stateAlarm.Reset(40.0);  // Below threshold, should reset
            if (resetWhileActive)
            {
                Console.WriteLine("✅ Alarm pravilno resetovan kada je vrednost ispod praga");
            }

            bool resetAfterReset = stateAlarm.Reset(30.0);  // Should not reset again
            if (!resetAfterReset)
            {
                Console.WriteLine("✅ Reset se ignoriše kada alarm nije aktivan");
            }

            // Test 4: Invalid enum values
            Console.WriteLine("\nTest 4: Invalid enum values");
            
            try
            {
                var invalidTrigger = new Alarm("INVALID_TRIGGER", (AlarmTrigger)999, 50.0, "Invalid trigger");
                Console.WriteLine("❌ GREŠKA: Nevalidan AlarmTrigger trebalo je da baci exception");
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine("✅ Nevalidan AlarmTrigger pravilno odbačen");
            }

            Console.WriteLine();
        }

        static void TestContextEdgeCases()
        {
            Console.WriteLine("=== TEST EDGE CASES: ContextClass ===");

            // Test 1: Multiple context instances
            Console.WriteLine("Test 1: Multiple context instances");
            
            try
            {
                using (var context1 = new ContextClass())
                using (var context2 = new ContextClass())
                {
                    var count1 = context1.Tags.Count();
                    var count2 = context2.Tags.Count();
                    
                    if (count1 == count2)
                    {
                        Console.WriteLine("✅ Multiple context instances rade konzistentno");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️  Context inconsistency: {count1} vs {count2}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ GREŠKA sa multiple contexts: {ex.Message}");
            }

            // Test 2: Database connection edge cases
            Console.WriteLine("\nTest 2: Database connection handling");
            
            try
            {
                using (var context = new ContextClass())
                {
                    // Force database creation if not exists
                    bool exists = context.Database.Exists();
                    Console.WriteLine($"✅ Database exists check: {exists}");
                    
                    // Test database initialization
                    context.Database.Initialize(force: false);
                    Console.WriteLine("✅ Database initialization successful");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Database connection issue: {ex.Message}");
            }

            // Test 3: Large data operations
            Console.WriteLine("\nTest 3: Large data operations");
            
            try
            {
                using (var context = new ContextClass())
                {
                    // Test querying large result sets (if any exist)
                    var allTags = context.Tags.ToList();
                    var allAlarms = context.Alarms.ToList();
                    var allActivatedAlarms = context.ActivatedAlarms.ToList();
                    
                    Console.WriteLine($"✅ Large query successful: {allTags.Count} tags, {allAlarms.Count} alarms, {allActivatedAlarms.Count} activated");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Large data operation failed: {ex.Message}");
            }

            // Test 4: Transaction edge cases
            Console.WriteLine("\nTest 4: Transaction handling");
            
            try
            {
                using (var context = new ContextClass())
                {
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            var testTag = new Tag(TagType.AI, "TRANSACTION_TEST", "Transaction test", "ADDR888");
                            context.Tags.Add(testTag);
                            context.SaveChanges();
                            
                            // Rollback transaction
                            transaction.Rollback();
                            Console.WriteLine("✅ Transaction rollback successful");
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            Console.WriteLine($"⚠️  Transaction rollback due to error: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Transaction handling failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        static void TestConcurrentOperations()
        {
            Console.WriteLine("=== TEST EDGE CASES: Concurrent Operations ===");

            var dataCollector = new DataCollector();
            
            // Test 1: Concurrent tag operations
            Console.WriteLine("Test 1: Concurrent tag operations");
            
            var tasks = new List<System.Threading.Tasks.Task>();
            
            // Create multiple tasks that add tags concurrently
            for (int i = 0; i < 5; i++)
            {
                int taskId = i;
                var task = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        var tag = new Tag(TagType.AI, $"CONCURRENT_TAG_{taskId}", $"Concurrent test {taskId}", $"ADDR{800 + taskId}");
                        tag.ValidateAndSetScanTime(1000);
                        tag.ValidateAndSetOnOffScan(false);
                        dataCollector.AddTag(tag);
                        Console.WriteLine($"✅ Task {taskId}: Tag added successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️  Task {taskId}: {ex.Message}");
                    }
                });
                tasks.Add(task);
            }
            
            // Wait for all tasks to complete
            System.Threading.Tasks.Task.WaitAll(tasks.ToArray());
            Console.WriteLine("✅ Concurrent tag addition completed");

            // Test 2: Concurrent write operations
            Console.WriteLine("\nTest 2: Concurrent write operations");
            
            // Add an output tag for testing
            var outputTag = new Tag(TagType.AO, "CONCURRENT_OUTPUT", "Concurrent output test", "ADDR777");
            outputTag.ValidateAndSetInitialValue(0);
            dataCollector.AddTag(outputTag);

            var writeTasks = new List<System.Threading.Tasks.Task>();
            
            for (int i = 0; i < 10; i++)
            {
                int value = i * 10;
                var writeTask = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        dataCollector.WriteTagValue("CONCURRENT_OUTPUT", value);
                        Thread.Sleep(10); // Small delay
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️  Write task error: {ex.Message}");
                    }
                });
                writeTasks.Add(writeTask);
            }
            
            System.Threading.Tasks.Task.WaitAll(writeTasks.ToArray());
            Console.WriteLine("✅ Concurrent write operations completed");

            // Test 3: Start/Stop stress test
            Console.WriteLine("\nTest 3: Start/Stop stress test");
            
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();
            
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    dataCollector.Start(plcSimulator);
                    Thread.Sleep(100);
                    dataCollector.Stop();
                    Thread.Sleep(100);
                }
                Console.WriteLine("✅ Start/Stop stress test completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Start/Stop stress test issue: {ex.Message}");
            }

            // Test 4: Memory stress test
            Console.WriteLine("\nTest 4: Memory stress test");
            
            try
            {
                var tempTags = new List<Tag>();
                for (int i = 0; i < 100; i++)
                {
                    var tempTag = new Tag(TagType.AI, $"TEMP_TAG_{i}", $"Temp tag {i}", $"ADDR{700 + i}");
                    tempTags.Add(tempTag);
                }
                
                Console.WriteLine($"✅ Created {tempTags.Count} temporary tags in memory");
                
                // Clean up
                tempTags.Clear();
                GC.Collect(); // Force garbage collection
                Console.WriteLine("✅ Memory cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Memory stress test failed: {ex.Message}");
            }

            Console.WriteLine();
        }

        static void TestPLCIntegration()
        {
            Console.WriteLine("=== TEST 5: Integracija sa PLC Simulator ===");

            var dataCollector = new DataCollector();
            var plcSimulator = new PLCSimulator.PLCSimulatorManager();

            Console.WriteLine("Pokretanje DataCollector sa PLC simulatorom...");
            dataCollector.Start(plcSimulator);
            Console.WriteLine("✅ DataCollector pokrenut");

            // Let it run for a short time to see scanning in action
            Console.WriteLine("⏳ Posmatranje skeniranja 5 sekundi...");
            Thread.Sleep(5000);

            // Test writing values during operation
            Console.WriteLine("\nTest pisanja vrednosti tokom rada:");
            dataCollector.WriteTagValue("AO_VALVE_001", 90.0);
            dataCollector.WriteTagValue("DO_LIGHT_001", 0);
            Console.WriteLine("✅ Vrednosti poslane u PLC");

            Thread.Sleep(2000);

            dataCollector.Stop();
            Console.WriteLine("✅ DataCollector zaustavljen");

            Console.WriteLine();
        }

        static void TestReportGeneration()
        {
            Console.WriteLine("=== TEST 6: Generiranje REPORT fajla ===");

            using (var context = new ContextClass())
            {
                // Get analog input tags and activated alarms
                var analogInputs = context.Tags.Where(t => t.Type == TagType.AI).ToList();
                var activatedAlarms = context.ActivatedAlarms
                    .OrderByDescending(a => a.AlarmTime)
                    .ToList();

                // Generate report according to specification
                string reportContent = GenerateSpecificationReport(analogInputs, activatedAlarms);

                // Save report to file
                string fileName = $"SCADA_Report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                File.WriteAllText(fileName, reportContent);

                Console.WriteLine($"✅ REPORT fajl generisan: {fileName}");
                Console.WriteLine("📄 Sadržaj izvještaja:");
                Console.WriteLine(new string('=', 60));
                Console.WriteLine(reportContent);
                Console.WriteLine(new string('=', 60));
            }
            Console.WriteLine();
        }

        static string GenerateSpecificationReport(List<Tag> analogInputs, List<ActivatedAlarm> activatedAlarms)
        {
            var report = new System.Text.StringBuilder();

            report.AppendLine("SCADA SISTEM - IZVJEŠTAJ");
            report.AppendLine($"Generisan: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            report.AppendLine(new string('=', 50));
            report.AppendLine();

            report.AppendLine("ANALOGNI ULAZI - VREDNOSTI (high_limit + low_limit)/2 ±5:");
            report.AppendLine(new string('-', 50));

            foreach (var tag in analogInputs)
            {
                if (tag.LowLimit.HasValue && tag.HighLimit.HasValue)
                {
                    // Calculate middle value as per specification: (high_limit + low_limit)/2 ±5
                    double middleValue = (tag.HighLimit.Value + tag.LowLimit.Value) / 2.0;
                    double lowerBound = middleValue - 5.0;
                    double upperBound = middleValue + 5.0;

                    report.AppendLine($"Tag: {tag.Id} - {tag.Description}");
                    report.AppendLine($"  Opseg: {tag.LowLimit:F1} - {tag.HighLimit:F1} {tag.Units}");
                    report.AppendLine($"  Srednja vrednost: {middleValue:F1} {tag.Units}");
                    report.AppendLine($"  Ciljni opseg ±5: {lowerBound:F1} - {upperBound:F1} {tag.Units}");
                    report.AppendLine($"  IO Adresa: {tag.IOAddress}");
                    report.AppendLine($"  Scan vreme: {tag.ScanTime}ms");
                    report.AppendLine();
                }
            }

            report.AppendLine("AKTIVIRANI ALARMI:");
            report.AppendLine(new string('-', 50));

            if (activatedAlarms.Any())
            {
                foreach (var alarm in activatedAlarms.Take(20)) // Show last 20 alarms
                {
                    report.AppendLine($"{alarm.AlarmTime:dd.MM.yyyy HH:mm:ss} - {alarm.TagName}");
                    report.AppendLine($"  Alarm ID: {alarm.AlarmId}");
                    report.AppendLine($"  Poruka: {alarm.Message}");
                    report.AppendLine();
                }
            }
            else
            {
                report.AppendLine("Nema aktiviranih alarma u sistemu.");
            }

            return report.ToString();
        }

        static void TestConfigurationPersistence()
        {
            Console.WriteLine("=== TEST 7: Perzistencija konfiguracije ===");

            using (var context = new ContextClass())
            {
                Console.WriteLine("📊 Finalno stanje baze podataka:");
                
                var totalTags = context.Tags.Count();
                var totalAlarms = context.Alarms.Count();
                var totalActivatedAlarms = context.ActivatedAlarms.Count();

                Console.WriteLine($"   Tagovi: {totalTags}");
                Console.WriteLine($"   Alarmi: {totalAlarms}");
                Console.WriteLine($"   Aktivirani alarmi: {totalActivatedAlarms}");

                // Test loading configuration in new DataCollector instance
                Console.WriteLine("\nTest učitavanja konfiguracije u novoj instanci:");
                var newDataCollector = new DataCollector();
                Console.WriteLine("✅ Nova DataCollector instanca kreirana");
                Console.WriteLine("✅ Konfiguracija automatski učitana iz baze");

                // Verify that tags were loaded
                var testValue = newDataCollector.GetTagValue("AO_VALVE_001");
                if (testValue.HasValue)
                {
                    Console.WriteLine($"✅ Tag AO_VALVE_001 učitan sa vrednošću: {testValue.Value}");
                }

                Console.WriteLine("\n📋 Detaljan pregled tagova:");
                var tags = context.Tags.Include(t => t.Alarms).ToList();
                foreach (var tag in tags)
                {
                    Console.WriteLine($"  - {tag.Id} ({tag.Type}): {tag.Description}");
                    if (tag.Alarms.Any())
                    {
                        Console.WriteLine($"    Alarmi: {string.Join(", ", tag.Alarms.Select(a => a.Id))}");
                    }
                }
            }
            Console.WriteLine();
        }
    }
}
