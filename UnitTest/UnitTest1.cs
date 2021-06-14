using Microsoft.VisualStudio.TestTools.UnitTesting;
using MemoryDbLibrary;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void PersistentStorageCheck()
        {
            MemoryDb db1 = new MemoryDb();
            var enabled = db1.IsPersistentStorageEnabled();
            Assert.AreEqual(false, enabled, "File was not specified but function returned with true");

            MemoryDb db2 = new MemoryDb("test.txt");
            enabled = db2.IsPersistentStorageEnabled();
            Assert.AreEqual(true, enabled, "Filw was specified but function returned with false");
        }

        [TestMethod]
        public void AddAndSelectTest()
        {
            MemoryDb db1 = new MemoryDb();

            string key = "/test/dir1/val1", value = "Here is the value";

            db1.Add(key, value);
            var s = db1.Select(key);

            Assert.AreEqual((key, value), s, "Added data did not collected");
        }

        [TestMethod]
        public void SaveAndLoadTest()
        {
            MemoryDb db1 = new MemoryDb("Load1.txt");

            string key = "/test/dir1/val1", value = "Here is the value";

            db1.Add(key, value);
            var respond = db1.Save(key);
            Assert.AreEqual(true, respond.Status, $"Save has failed: {respond.Message}");

            db1.RemoveAll();
            var s = db1.Select(key);
            Assert.AreEqual(null, s.Value, "Record did not deleted from database");

            respond = db1.Load(false, key);
            Assert.AreEqual(true, respond.Status, $"Load has failed: {respond.Message}");

            s = db1.Select(key);
            Assert.AreNotEqual(null, s.Value, "Value is null after load");
        }

        [TestMethod]
        public void OverrideLoadTest()
        {
            MemoryDb db1 = new MemoryDb("Load2.txt");

            string key = "/test/dir1/val1", value = "Here is the value";

            db1.Add(key, value);
            var respond = db1.Save(key);
            Assert.AreEqual(true, respond.Status, $"Save has failed: {respond.Message}");

            db1.Add(key, "Changed value");
            var s = db1.Select(key);
            Assert.AreEqual("Changed value", s.Value, "Value did not changed");

            respond = db1.Load(false, key);
            Assert.AreEqual(false, respond.Status, $"Load has failed: {respond.Message}");

            s = db1.Select(key);
            Assert.AreEqual("Changed value", s.Value, "Value did not changed");

            respond = db1.Load(true, key);
            Assert.AreEqual(true, respond.Status, $"Load has failed: {respond.Message}");

            s = db1.Select(key);
            Assert.AreEqual(value, s.Value, "Value was not reloaded");
        }

        [TestMethod]
        public void ListAllTest()
        {
            MemoryDb db1 = new MemoryDb();

            Dictionary<string, string> control = new Dictionary<string, string>();
            control.Add("test1/proc1/val1", "sample value #1");
            control.Add("test1/proc1/val2", "sample value #2");
            control.Add("test1/proc2/val1", "sample value #3");
            control.Add("test2/val1", "sample value #4");

            foreach (var item in control)
            {
                db1.Add(item.Key, item.Value);
            }

            var check = db1.ListAll();
            Assert.AreEqual(control.Count, check.Count, "Lenght of lists are not same");

            bool same = true;
            foreach (var item in control)
            {
                string value;
                if(check.TryGetValue(item.Key, out value))
                {
                    if (value != item.Value)
                    {
                        same = false;
                        break;
                    }
                }
                else
                {
                    same = false;
                    break;
                }
            }

            Assert.AreEqual(same, true, "Database did not send the same values back");
        }

        [TestMethod]
        public void ListRemoveLoadAllTest()
        {
            MemoryDb db1 = new MemoryDb("loadAll.txt");

            Dictionary<string, string> control = new Dictionary<string, string>();
            control.Add("test1/proc1/val1", "sample value #1");
            control.Add("test1/proc1/val2", "sample value #2");
            control.Add("test1/proc2/val1", "sample value #3");
            control.Add("test2/val1", "sample value #4");

            foreach (var item in control)
            {
                db1.Add(item.Key, item.Value);
                db1.Save(item.Key);
            }

            db1.RemoveAll();

            var check = db1.ListAll();
            Assert.AreEqual(0, check.Count, "Remove all did not work");

            var load = db1.LoadAll(false);
            Assert.AreEqual(true, load.Status, $"Load has failed: {load.Message}");

            check = db1.ListAll();
            Assert.AreEqual(control.Count, check.Count, "Lenght of lists are not same");

            bool same = true;
            foreach (var item in control)
            {
                string value;
                if (check.TryGetValue(item.Key, out value))
                {
                    if (value != item.Value)
                    {
                        same = false;
                        break;
                    }
                }
                else
                {
                    same = false;
                    break;
                }
            }

            Assert.AreEqual(same, true, "Database did not send the same values back");
        }

        [TestMethod]
        public void DeleteDirectoryTest()
        {
            MemoryDb db1 = new MemoryDb();

            Dictionary<string, string> source = new Dictionary<string, string>();
            source.Add("test1/proc1", "sample value #0");
            source.Add("test1/proc1/val1", "sample value #1");
            source.Add("test1/proc1/val2", "sample value #2");
            source.Add("test1/proc2/val1", "sample value #3");
            source.Add("test2/val1", "sample value #4");

            Dictionary<string, string> control = new Dictionary<string, string>();
            control.Add("test1/proc2/val1", "sample value #3");
            control.Add("test2/val1", "sample value #4");

            foreach (var item in source)
            {
                db1.Add(item.Key, item.Value);
            }

            db1.RemoveDir("test1/proc1");

            var check = db1.ListAll();
            Assert.AreEqual(control.Count, check.Count, "Lenght of lists are not same");

            bool same = true;
            foreach (var item in control)
            {
                string value;
                if (check.TryGetValue(item.Key, out value))
                {
                    if (value != item.Value)
                    {
                        same = false;
                        break;
                    }
                }
                else
                {
                    same = false;
                    break;
                }
            }

            Assert.AreEqual(same, true, "Database did not send the same values back");
        }

        [TestMethod]
        public void GetDirectoryTest()
        {
            MemoryDb db1 = new MemoryDb();

            Dictionary<string, string> source = new Dictionary<string, string>();
            source.Add("test1/proc1", "sample value #0");
            source.Add("test1/proc1/val1", "sample value #1");
            source.Add("test1/proc1/val2", "sample value #2");
            source.Add("test1/proc2/val1", "sample value #3");
            source.Add("test2/val1", "sample value #4");

            Dictionary<string, string> control = new Dictionary<string, string>();
            control.Add("test1/proc1", "sample value #0");
            control.Add("test1/proc1/val1", "sample value #1");
            control.Add("test1/proc1/val2", "sample value #2");

            foreach (var item in source)
            {
                db1.Add(item.Key, item.Value);
            }

            var check = db1.ListDir("test1/proc1");
            Assert.AreEqual(control.Count, check.Count, "Lenght of lists are not same");

            bool same = true;
            foreach (var item in control)
            {
                string value;
                if (check.TryGetValue(item.Key, out value))
                {
                    if (value != item.Value)
                    {
                        same = false;
                        break;
                    }
                }
                else
                {
                    same = false;
                    break;
                }
            }

            Assert.AreEqual(same, true, "Database did not send the same values back");
        }

        [TestMethod]
        public void PurgeFromFile()
        {
            MemoryDb db1 = new MemoryDb("purge-test.txt");

            db1.Add("test1/valami", "Here is the value");
            var respond = db1.Save("test1/valami");
            Assert.AreEqual(true, respond.Status, "Variable save did not work");

            respond = db1.Purge("test1/valami");
            Assert.AreEqual(true, respond.Status, "Variable purge did not work");

            respond = db1.Load(true, "test1/valami");
            Assert.AreEqual(false, respond.Status, respond.Message);
        }

        [TestMethod]
        public void JsonRecordDuplicate()
        {
            MemoryDb db1 = new MemoryDb("duplicate-test.json");

            db1.Add("test/valami", "Here is the value");
            db1.Save("test/valami");
            db1.Save("test/valami");

            string jsonString = File.ReadAllText("duplicate-test.json");
            var records = JsonSerializer.Deserialize<GlobalVariableList>(jsonString);

            Assert.AreEqual(1, records.List.Count, "Not one records was saved into file");
        }

        [TestMethod]
        public void MetricTest1()
        {
            MemoryDb db1 = new MemoryDb();

            Assert.AreEqual(false, db1.IsMetricEnabled(), "Metric is not false by default");

            db1.EnableMetric();
            Assert.AreEqual(true, db1.IsMetricEnabled(), "Metric did not enabled");

            db1.DisableMetric();
            Assert.AreEqual(false, db1.IsMetricEnabled(), "Metric did not disabled");
        }

        [TestMethod]
        public void MetricTest2()
        {
            MemoryDb db1 = new MemoryDb();

            db1.Add("test/valami", "Érték mindeütt");
            db1.Add("test/valami2", "Érték mindeütt");
            db1.Add("test/valami3", "Érték mindeütt");

            var metric = db1.DumpMetrics();
            Assert.AreEqual(0, metric.Count, "There was metric although it was disabled by default");
        }

        [TestMethod]
        public void MetricTest3()
        {
            MemoryDb db1 = new MemoryDb();

            db1.EnableMetric();

            db1.Add("test/valami", "Érték mindeütt");
            db1.Add("test/valami2", "Érték mindeütt");
            db1.Add("test/valami3", "Érték mindeütt");

            var metric = db1.DumpMetrics();
            Assert.AreNotEqual(0, metric.Count, "There was no mesured metric data");
        }
    }

    public class GlobalVariableList
    {
        [JsonPropertyName("VariableName")]
        public List<GlobalVariable> List { get; set; }
    }

    public class GlobalVariable
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("value")]
        public string Value { get; set; }

        [JsonIgnore]
        public List<string> Keys { get; set; } = new List<string>();

        [JsonConstructor]
        public GlobalVariable(string key)
        {
            Key = key;
            string[] temp = key.Split("/");
            if (key != null)
            {
                foreach (var item in temp)
                {
                    Keys.Add(item);
                }
            }
        }

        public GlobalVariable(string _key, string _value)
        {
            Key = _key;
            Value = _value;
            if (_key != null)
            {
                string[] temp = _key.Split("/");
                foreach (var item in temp)
                {
                    Keys.Add(item);
                }
            }
        }
    }
}
