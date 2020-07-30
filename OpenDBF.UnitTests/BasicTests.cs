using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenDBF.Core;
using OpenDBF.UnitTests.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenDBF.UnitTests
{
    [TestClass]
    public class OpenDBTests
    {
        public OpenDBTests()
        {

        }

        [TestMethod]
        public void BasicXMLTest()
        {
            try
            {
                //Initializes the database framework.
                var dh = FrameworkFactory.GetFramework(FrameworkFactory.Framework_e.XML);

                //Creates the workspace.
                string workspace = Path.Combine(Directory.GetCurrentDirectory(), @"DBXMLSample");
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

                //Saves the database
                dh.Commit();

                //Exports the database to a .db file.
                dh.ExportDatabase(Path.Combine(Directory.GetCurrentDirectory(), @"Place"), "copyOfSampleDatabase1");

                //Deletes the database
                dh.DeleteDatabase();
                            
                //Deletes the workspace and clears the internal resources.
                dh.ClearHandler();
                Directory.Delete(workspace, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void BasicJSONTest()
        {
            try
            {
                //Initializes the database framework.
                var dh = FrameworkFactory.GetFramework(FrameworkFactory.Framework_e.JSON);

                //Creates the workspace.
                string workspace = Path.Combine(Directory.GetCurrentDirectory(), @"DBJSONSample");
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

                //Saves the database
                dh.Commit();

                //Exports the database to a .db file.
                dh.ExportDatabase(Path.Combine(Directory.GetCurrentDirectory(), @"Place"), "copyOfSampleDatabase1");

                //Deletes the database
                dh.DeleteDatabase();

                //Deletes the workspace and clears the internal resources.
                dh.ClearHandler();
                Directory.Delete(workspace, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail();
            }
        }

        [TestMethod]
        public void BasicDATTest()
        {
            try
            {
                //Initializes the database framework.
                var dh = FrameworkFactory.GetFramework(FrameworkFactory.Framework_e.DAT);

                //Creates the workspace.
                string workspace = Path.Combine(Directory.GetCurrentDirectory(), @"DBDATSample");
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

                //Saves the database
                dh.Commit();

                //Exports the database to a .db file.
                dh.ExportDatabase(Path.Combine(Directory.GetCurrentDirectory(), @"Place"), "copyOfSampleDatabase1");

                //Deletes the database
                dh.DeleteDatabase();

                //Deletes the workspace and clears the internal resources.
                dh.ClearHandler();
                Directory.Delete(workspace, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail();
            }
        }
    }
}
