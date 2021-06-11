using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

namespace MemoryDbLibrary
{
    public class MemoryDb
    {
        List<GlobalVariableNode> _mainList = new List<GlobalVariableNode>();
        string _filePath;
        static Mutex mutex = new Mutex();

        #region Constructors
        /// <summary>
        /// Base constructor. It can be used if persistent storage is not needed
        /// </summary>
        public MemoryDb()
        {

        }

        /// <summary>
        /// Constructor with persistent storage function
        /// </summary>
        /// <param name="filePath">File path of file where persistent data can be stored</param>
        public MemoryDb(string filePath)
        {
            _filePath = filePath;
        }
        #endregion Deconstructors

        #region Others
        /// <summary>
        /// This function can tell that persistent storage is active
        /// </summary>
        /// <returns>True or false</returns>
        public bool IsPersistentStorageEnabled()
        {
            bool output = _filePath == null ? true : false;
            if (output)
                return false;

            if (!File.Exists(_filePath))
            {
                try
                {
                    var fs1 = File.Create(_filePath);
                    fs1.Close();
                }
                catch
                {
                    return false;
                }
            }

            try
            {
                FileStream fs = new FileStream(_filePath, FileMode.Open, FileAccess.ReadWrite);
                fs.Close();
            }
            catch
            {
                return false;
            }

            return true;
        }
        #endregion

        #region Save record into file
        /// <summary>
        /// This function can be called to save a global variable into file storage.
        /// </summary>
        /// <param name="key">Key of the variable which needs to be saved</param>
        /// <returns>
        /// It return with a tuple: <para/>
        /// <b>bool Status:</b> true or falsestring<para/>
        /// <b>string Message:</b> explanation message
        /// </returns>
        public (bool Status, string Message) Save(string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
                return (false, "Key is not specified");

            if (!IsPersistentStorageEnabled())
                return (false, "Persistent storage is not allowed");

            GlobalVariableList jsonObejct = new GlobalVariableList();

            if (File.Exists(_filePath))
            {
                string previousData = File.ReadAllText(_filePath);
                if (string.IsNullOrEmpty(previousData) || string.IsNullOrWhiteSpace(previousData))
                    jsonObejct.List = new List<GlobalVariable>();
                else
                {
                    try
                    {
                        jsonObejct = JsonSerializer.Deserialize<GlobalVariableList>(previousData);
                    }
                    catch (Exception ex)
                    {
                        return (false, ex.Message);
                    }
                }
            }
            else
                jsonObejct.List = new List<GlobalVariable>();

            var foundOne = Select(key);

            if (foundOne.Value == null)
                return (false, "Variable does not exist");

            bool found = false;
            foreach (var item in jsonObejct.List)
            {
                if(item.Key == foundOne.Key && item.Value == foundOne.Value)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                jsonObejct.List.Add(new GlobalVariable(foundOne.Key, foundOne.Value));
                string newData = JsonSerializer.Serialize(jsonObejct);
                File.WriteAllText(_filePath, newData);
            }

            return (true, "Variable is saved");
        }
        #endregion

        #region Load records from file
        /// <summary>
        /// Load all data from persistent storage into memory
        /// </summary>
        /// <param name="replaceValues">If true and variable already exist, its value will be overwritten</param>
        /// <returns>
        /// It return with a tuple: <para/>
        /// <b>bool Status:</b> true or falsestring<para/>
        /// <b>string Message:</b> explanation message
        /// </returns>
        public (bool Status, string Message) LoadAll(bool replaceValues)
        {
            if (!IsPersistentStorageEnabled())
                return (false, "Persistent storage is not allowed");

            string previousData = File.ReadAllText(_filePath);

            GlobalVariableList jsonObejct = new GlobalVariableList();

            if (string.IsNullOrEmpty(previousData) || string.IsNullOrWhiteSpace(previousData))
                return (false, "File is empty");
            else
            {
                try
                {
                    jsonObejct = JsonSerializer.Deserialize<GlobalVariableList>(previousData);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }

            foreach (var item in jsonObejct.List)
            {
                if (!replaceValues)
                {
                    var foundOne = Select(item.Key);
                    if (foundOne.Value == null)
                        Add(item.Key, item.Value);
                }
                else
                    Add(item.Key, item.Value);
            }

            return (true, "Variables are loaded");
        }


        /// <summary>
        /// This method is able to load specific entry only
        /// </summary>
        /// <param name="replaceValues">If true and variable already exist, then it will be overwritten</param>
        /// <returns>
        /// It return with a tuple: <para/>
        /// <b>bool Status:</b> true or falsestring<para/>
        /// <b>string Message:</b> explanation message
        /// </returns>
        public (bool Status, string Message) Load(bool replaceValues, string key)
        {
            if (!IsPersistentStorageEnabled())
                return (false, "Persistent storage is not allowed");

            string previousData = File.ReadAllText(_filePath);

            GlobalVariableList jsonObejct = new GlobalVariableList();

            if (string.IsNullOrEmpty(previousData) || string.IsNullOrWhiteSpace(previousData))
                return (false, "File is empty");
            else
            {
                try
                {
                    jsonObejct = JsonSerializer.Deserialize<GlobalVariableList>(previousData);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }

            var foundOne = jsonObejct.List.Where(w => w.Key == key).Select(s => s).FirstOrDefault();

            if (foundOne != null)
            {
                if (!replaceValues)
                {
                    var selectOne = Select(foundOne.Key);
                    if (selectOne.Value == null)
                        Add(foundOne.Key, foundOne.Value);
                    else
                        return (false, "Variable already exist and override is not allowed");
                }
                else
                    Add(foundOne.Key, foundOne.Value);

            }
            else
            {
                return (false, "Variable could not located in the file");
            }

            return (true, "Variable is loaded");
        }
        #endregion

        #region Purge variable from file
        /// <summary>
        /// Remove records from the persistent storage
        /// </summary>
        /// <param name="key">Key of the record</param>
        /// <returns>
        /// It return with a tuple: <para/>
        /// <b>bool Status:</b> true or falsestring<para/>
        /// <b>string Message:</b> explanation message
        /// </returns>
        public (bool Status, string Message) Purge(string key)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrWhiteSpace(key))
                return (false, "Key must be specified");

            string previousData = File.ReadAllText(_filePath);

            GlobalVariableList jsonObejct = new GlobalVariableList();

            if (string.IsNullOrEmpty(previousData) || string.IsNullOrWhiteSpace(previousData))
                return (false, "File is empty");
            else
            {
                try
                {
                    jsonObejct = JsonSerializer.Deserialize<GlobalVariableList>(previousData);
                }
                catch (Exception ex)
                {
                    return (false, ex.Message);
                }
            }

            var foundOne = jsonObejct.List.Where(w => w.Key == key).Select(s => s).FirstOrDefault();

            if(foundOne != null)
            {
                jsonObejct.List.Remove(foundOne);
                var outJson = JsonSerializer.Serialize(jsonObejct);
                File.WriteAllText(_filePath, outJson);
            }
            else
            {
                return (false, "Variable did not exist in the file");
            }

            return (true, "Variable is purged from file");
        }
        #endregion

        #region Add new element
        /// <summary>
        /// Add new variable into the database
        /// </summary>
        /// <param name="key">Index for record</param>
        /// <param name="value">Value of record</param>
        public void Add(string key, string value)
        {
            try
            {
                GlobalVariable variable = new GlobalVariable(key, value);

                if (string.IsNullOrEmpty(variable.Value) || string.IsNullOrWhiteSpace(variable.Value))
                    variable.Value = null;

                if (variable.Keys.Count == 0)
                    return;

                mutex.WaitOne();
                _mainList = SetVariable(variable, _mainList);
                mutex.ReleaseMutex();
            }
            catch
            {
                mutex.ReleaseMutex();
            }
        }

        private List<GlobalVariableNode> SetVariable(GlobalVariable input, List<GlobalVariableNode> list)
        {
            var foundNode = list.Where(w => w.Key == input.Keys[0]).Select(s => s).FirstOrDefault();

            if (foundNode == null)
            {
                GlobalVariableNode newOne;
                if (input.Keys.Count == 1)
                    newOne = new GlobalVariableNode(input.Keys[0], input.Value);
                else
                    newOne = new GlobalVariableNode(input.Keys[0], null);

                list.Add(newOne);
                input.Keys.RemoveAt(0);
                if (input.Keys.Count > 0)
                    newOne.SubList = SetVariable(input, newOne.SubList);
            }
            else
            {
                if (input.Keys.Count == 1)
                {
                    foundNode.Value = input.Value;
                    if (foundNode.Value == null && foundNode.SubList.Count == 0)
                        list.Remove(foundNode);
                }

                input.Keys.RemoveAt(0);
                if (input.Keys.Count > 0)
                    foundNode.SubList = SetVariable(input, foundNode.SubList);
            }

            return list;
        }
        #endregion

        #region List all element
        /// <summary>
        /// List all global variable from database
        /// </summary>
        /// <returns>Dictionary which contains the key-value pairs</returns>
        public Dictionary<string, string> ListAll()
        {
            try
            {
                mutex.WaitOne();
                var list = PrintAll("", _mainList);
                mutex.ReleaseMutex();

                Dictionary<string, string> output = new Dictionary<string, string>();
                foreach (var item in list)
                {
                    output.Add(item.Key, item.Value);
                }

                return output;
            }
            catch
            {
                mutex.ReleaseMutex();
                return null;
            }
        }

        private List<GlobalVariable> PrintAll(string key, List<GlobalVariableNode> list)
        {
            List<GlobalVariable> output = new List<GlobalVariable>();
            foreach (var item in list)
            {
                if (item.Value != null)
                {
                    output.Add(new GlobalVariable($"{key}{item.Key}", item.Value));
                }
                List<GlobalVariable> tmp = PrintAll($"{key}{item.Key}/", item.SubList);
                output = output.Union(tmp).ToList();
            }
            return output;
        }
        #endregion

        #region Select specifiec item
        /// <summary>
        /// Find specific item in database
        /// </summary>
        /// <param name="key">Index which record should be found</param>
        /// <returns>
        /// With a tuple which contains: <para/>
        /// <b>string Key:</b> index of record <para/>
        /// <b>string Value:</b> value of record. <u>It is null if variable did not found!</u>
        /// </returns>
        public (string Key, string Value) Select(string key)
        {
            try
            {
                GlobalVariable output = new GlobalVariable(key);
                if (output.Keys.Count == 0)
                    return (key, null);

                mutex.WaitOne();
                output.Value = SelectValue(output, _mainList);
                mutex.ReleaseMutex();

                return (output.Key, output.Value);
            }
            catch
            {
                mutex.ReleaseMutex();
                return (null, null);
            }
        }

        private string SelectValue(GlobalVariable input, List<GlobalVariableNode> list)
        {
            string output = null;

            foreach (var item in list)
            {
                if (item.Key == input.Keys[0])
                {
                    input.Keys.RemoveAt(0);
                    if (input.Keys.Count > 0)
                        output = SelectValue(input, item.SubList);
                    else
                        output = item.Value;

                    break;
                }
            }

            return output;
        }
        #endregion

        #region List directory
        /// <summary>
        /// List all global variable under a specified index path
        /// </summary>
        /// <param name="key">Index path which tells where from data should be collected</param>
        /// <returns>Dictionary which contains the key-value pairs</returns>
        public Dictionary<string, string> ListDir(string key)
        {
            try
            {
                GlobalVariable input = new GlobalVariable(key);
                if (input.Keys.Count == 0)
                    return null;

                mutex.WaitOne();
                var list = PrintDir(input, _mainList);
                mutex.ReleaseMutex();

                Dictionary<string, string> output = new Dictionary<string, string>();
                foreach (var item in list)
                {
                    output.Add(item.Key, item.Value);
                }

                return output;
            }
            catch
            {
                mutex.ReleaseMutex();
                return null;
            }
        }

        private List<GlobalVariable> PrintDir(GlobalVariable input, List<GlobalVariableNode> list)
        {
            List<GlobalVariable> output = new List<GlobalVariable>();

            foreach (var item in list)
            {
                if (item.Key == input.Keys[0])
                {
                    input.Keys.RemoveAt(0);
                    if (input.Keys.Count > 0)
                        output = PrintDir(input, item.SubList);
                    else
                    {
                        output = PrintAll($"{input.Key}/", item.SubList);
                        if (item.Value != null)
                            output.Insert(0, new GlobalVariable(input.Key, item.Value));
                    }

                    break;
                }
            }

            return output;
        }
        #endregion

        #region Remove all data
        /// <summary>
        /// Remove every record from the database
        /// </summary>
        public void RemoveAll()
        {
            try
            {
                mutex.WaitOne();
                PurgeAll(_mainList);
                mutex.ReleaseMutex();
            }
            catch
            {
                mutex.ReleaseMutex();
            }
        }

        private void PurgeAll(List<GlobalVariableNode> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                PurgeAll(list[i].SubList);
                list.RemoveAt(i);
            }
        }
        #endregion

        #region Remove directory
        /// <summary>
        /// Remove every record from a specified index path
        /// </summary>
        /// <param name="key">Index path</param>
        public void RemoveDir(string key)
        {
            try
            {
                GlobalVariable input = new GlobalVariable(key);
                if (input.Keys.Count == 0)
                    return;

                mutex.WaitOne();
                PurgeDir(input, _mainList);
                mutex.ReleaseMutex();
            }
            catch
            {
                mutex.ReleaseMutex();
            }
        }

        private void PurgeDir(GlobalVariable input, List<GlobalVariableNode> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (input.Keys.Count > 0)
                {
                    if (input.Keys[0] == list[i].Key)
                    {
                        if (input.Keys.Count > 1)
                        {
                            input.Keys.RemoveAt(0);
                            PurgeDir(input, list[i].SubList);
                            break;
                        }

                        PurgeAll(list[i].SubList);
                        list.RemoveAt(i);
                        break;
                    }
                }
            }
        }
        #endregion

        #region Used objects
        private class GlobalVariableList
        {
            [JsonPropertyName("VariableName")]
            public List<GlobalVariable> List { get; set; }
        }

        private class GlobalVariable
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

        private class GlobalVariableNode
        {
            public string Key { get; set; }

            public string Value { get; set; }

            public List<GlobalVariableNode> SubList { get; set; } = new List<GlobalVariableNode>();

            public GlobalVariableNode(string _key, string _value)
            {
                Key = _key;
                Value = _value;
            }

            public GlobalVariableNode(GlobalVariable _var)
            {
                Key = _var.Key;
                Value = _var.Value;
            }
        }
        #endregion
    }
}
