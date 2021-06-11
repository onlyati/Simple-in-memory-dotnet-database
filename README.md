# Simple in memory database 

This project is a simple in memory database, what I have made for my other projects where I built it. This can be used to manage data in memory and external providers (e.g.: redis) would be too much for the task. It is suitable to store those informations which not needed to update very frequently, for example it is good to store application settings.

## Capabilities

It is a light engine with some simple actions. Action what it can execute:
- Add/Change/Remove record
- List all records
- List subrecords
- Save records into file as persistent storage
- Load records from file

## How data is stored

It is a key-value type database. Key can be specified like file path, which can help to find lists and variables faster even if they are present in bigger number. From external view, this package users can see data in dictionary<string, string> or in (string Key, string Value) tuple.

But to explain how it is stored internal, let us say, that following key-value pairs are stored in this database:
```
settings/port-master            10245
settings/port-slave             10246
settings/port-agent             10247
settings/target-list            host1.com host2.com host3.com
settings/log-path               /var/log/app.log
monitor/memory                  on
monitor/memory/limit            8000
monitor/docker                  on
monitor/docker/list             gitlab pgadmin postgres
monitor/docker/gitlab-status    ok
monitor/docker/pgadmin-status   ok
monitor/docker/postgres-status  ok
```
These variables are stored in the following hierarchy:
```
+----------+
| settings | ---------------> +-------------+
| monitor  | ---+             | port-master |
+----------+    |             | port-slave  |
                |             | port-agent  |
                |             | target-list |
                |             | log-path    |
                |             +-------------+
                |
                +-----------> +-------------+
                              | memory      | ----------> +-----------------+
                              | docker      | -----+      | limit           |
                              +-------------+      |      +-----------------+
                                                   |
                                                   +----> +-----------------+
                                                          | list            |
                                                          | gitlab-status   |
                                                          | pgadmin-status  |
                                                          | postgres-status |
                                                          +-----------------+
```

## Example calls

**Constructors**
```cs
// Without persistent storage (save and load functions)
MemoryDb db = new MemoryDb();

// With persistent storage, filepath must be specified
MemoryDb db = new MemoryDb(@"/usr/share/application-name/storage.json");
```

**Add, Change or delete record**
```cs
// Add new record
db.Add("test1/sub1/val1", "Here is the value");

// If record already exist, it will be overwritten with the new value
db.Add("test1/sub1/val1", "Change value");

// Remove record is to add empty or null value
db.Add("test1/sub1/val1", "");
db.Add("test1/sub1/val1", null);
```

**List and select records**
```cs
// List every record
Dictionary<string, string> allRecord = db.ListAll();

// List every record under specified path
// This command would put these items into a dictionary and return with it:
// - settings/port-master 10245
// - settings/port-slave 10246
// - settings/port-agent 10247
// - settings/target-list host1.com host2.com host3.com
// - settings/log-path /var/log/app.log
Dictionary<string, string> settings = db.ListDir("settings");

// Find only for a specific entry. If variable does not exist then gitStatus.Value is null
(string Key, string Value) gitStatus = db.Select("monitor/docker/gitlab-status");
```

**Remove**
```cs
// Clean the whole database
db.RemoveAll();

// Clean only monitor/docker records:
db.RemoveDir("monitor/docker");
```

**Save and load**
```cs
// Settings has been changed and this changed value would required after a restart too
// So, save into the file
(bool Status, string Message) save = db.Save("settings/port-master");

// Load specific entry from file
// First parameter: if record already exist then load should overwrite it?
// Second paramater: record key
(bool Status, string Message) save = db.Load(true, "settings/port-master");

// Load everything from file
(bool Status, string Message) save = db.LoadAll(true);


```

