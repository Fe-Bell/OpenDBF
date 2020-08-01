<p align="center">
  <img src="Art/Logo.svg">
</p>

![.NET Core](https://github.com/Fe-Bell/OpenDBF/workflows/.NET%20Core/badge.svg)

# OpenDBF
The Open Database Framework is a free and open source database library based on object serialization to file.

OpendDBF is a child project of [ReflectXMLDB](https://github.com/Fe-Bell/ReflectXMLDB/).

The project was written with [.NET Standard 2.0](https://docs.microsoft.com/en-us/dotnet/standard/net-standard), 
which is compatible with .NET Core 2.0+, .NET Framework 4.6.1+ and .NET 5.0+.

OpenDBF is cross platform and can run in any OS that supports .NET Core 2.0 or higher.

# Features
- Multithread and process safety.
- Supports XML, JSON and DAT file formats.
- Provides high-level methods to read/write data to the database files.
- Compatible with Linq.
- It is completely open source and under the very permissive [MIT License](https://github.com/Fe-Bell/OpenDBF/blob/master/LICENSE).!

More to come!

# Get started
Download OpenDBF from [nuget](https://www.nuget.org/packages/OpenDBF.Core/).

OpenDBF offers the traditional Get, Insert, Remove and Update methods commonly found in other database frameworks. 
It creates a single file for all database objects and also supports zip compression with the use of Pack/Unpack methods.

There are two steps to get OpenDBF running:
1. Custom objects must inherit from OpenDBF.Shared.Interface.ICollectableObject and be serializable so they can be inserted in the databse.

Example:
```csharp
	[Serializable]
	public class Sample : ICollectableObject
	{
		public string GUID { get; set; }
		public uint EID { get; set; }
		public string SomeData { get; set; }
	}	
```

2. Users must create an instance of an IDatabaseFramework. OpenDBF.Core offers a FrameworkFactory for that. It is also possible to manually instantiate these frameworks.

Example:
```csharp

	//Initializes the database framework.
	var dh = FrameworkFactory.GetFramework(FrameworkFactory.Framework_e.JSON);

	//Creates the workspace.
	string workspace = Path.Combine(Directory.GetCurrentDirectory(), @"DBSample");
	dh.SetWorkspace(workspace);

	//Creates a list of objects to be inserted.
	//Sample inherits from ICollectableObject and is in the same namespace of SampleDatabase
	List<Sample> samples = new List<Sample>();
	for (int i = 0; i < 10; i++)
	{
		samples.Add(new Sample() { SomeData = string.Format("Data{0}", i) });
	}

	//Inserts the items in the database.
	dh.Insert(samples);

	//Gets all samples in the database.
	var queryAllSamples = dh.Get<Sample>();
	//Gets all samples that have Data5 as the value of the SomeData property.
	var querySomeSamples = dh.Get<Sample>(x => x.SomeData == "Data5");

	//Removes some of the items in the database.
	dh.Remove<Sample>(querySomeSamples);

	//Saves the database to a file
	dh.Commit();

	//Exports the database to a .db file.
	dh.Pack(Path.Combine(Directory.GetCurrentDirectory(), @"Place"), "copyOfSampleDatabase1");

	//Removes all "samples" from the database
	dh.DropTable<Sample>();

	//Clears internal resources.
	dh.Dispose();
	
```

Happy coding!

# License
Licensed under [MIT License](https://github.com/Fe-Bell/OpenDBF/blob/master/LICENSE).
