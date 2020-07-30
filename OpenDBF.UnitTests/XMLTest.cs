using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenDBF.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDBF.UnitTests
{
    [TestClass]
    public class XMLTest
    {
        private static FrameworkFactory.Framework_e FRAMEWORK = FrameworkFactory.Framework_e.XML;

        public XMLTest()
        {

        }

        [TestMethod]
        public void GenericTest()
        {
            CommonTests.GenericTest(FRAMEWORK);
        }

        [TestMethod]
        public void LoadExistingDatabaseTest()
        {
            CommonTests.LoadExistingDatabaseTest(FRAMEWORK);
        }
    }
}
