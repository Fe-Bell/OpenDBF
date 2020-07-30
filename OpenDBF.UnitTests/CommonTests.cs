using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenDBF.Core;
using OpenDBF.UnitTests.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace OpenDBF.UnitTests
{
    public class CommonTests
    {
        public CommonTests()
        {

        }

        public static void GenericTest(FrameworkFactory.Framework_e framework)
        {
            try
            {
                //Initializes the database framework.
                var dh = FrameworkFactory.GetFramework(framework);

                //Creates the workspace.
                string workspace = Path.Combine(Directory.GetCurrentDirectory(), string.Format("DB{0}Sample", framework.ToString()));
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
                string archiveFolder = Path.Combine(Directory.GetCurrentDirectory(), @"ArchiveFolder");
                string archiveFileName = framework.ToString() + "Archive";
                dh.Pack(archiveFolder, archiveFileName);
                
                string fullArchivePath = Path.Combine(archiveFolder, archiveFileName + ".db");
                if (!File.Exists(fullArchivePath))
                {
                    Assert.Fail();
                }

                //Deletes the database
                if(!dh.DropTable<Sample>())
                {
                    Assert.Fail();
                }

                //Deletes the workspace and clears the internal resources.
                dh.Dispose();
                Directory.Delete(workspace, true);
                Directory.Delete(archiveFolder, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail();
            }
        }

        public static void LoadExistingDatabaseTest(FrameworkFactory.Framework_e framework)
        {
            try
            {
                //Initializes the database framework.
                var db = FrameworkFactory.GetFramework(framework);

                //Creates the workspace.
                string workspace = Path.Combine(Directory.GetCurrentDirectory(), string.Format("DB{0}Sample", framework.ToString()));
                db.SetWorkspace(workspace);

                string currentFileName = db.CurrentFileName;

                //Creates a list of objects to be inserted.
                //Sample inherits from ICollectableObject and is in the same namespace of SampleDatabase
                List<Sample> samples = new List<Sample>();
                for (int i = 0; i < 10; i++)
                {
                    samples.Add(new Sample() { SomeData = string.Format("Data{0}", i) });
                }

                //Inserts the items in the database.
                db.Insert(samples);
                             
                //Saves the database
                db.Commit();
                db.Dispose();

                //New framework
                db = FrameworkFactory.GetFramework(framework);
                db.SetWorkspace(workspace, currentFileName);

                var query = db.Get<Sample>();
                if(query.Count() != samples.Count)
                {
                    Assert.Fail();
                }

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
