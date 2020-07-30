using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenDBF.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenDBF.UnitTests
{
    [TestClass]
    public class DATTest
    {
        private static FrameworkFactory.Framework_e FRAMEWORK = FrameworkFactory.Framework_e.DAT;

        public DATTest()
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
