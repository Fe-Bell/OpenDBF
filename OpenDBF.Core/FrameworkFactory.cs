using OpenDBF.JSON;
using OpenDBF.Shared.Interface;
using OpenDBF.XML;
using System;
using System.ComponentModel;

namespace OpenDBF.Core
{
    public static class FrameworkFactory
    {
        public enum Framework_e
        {
            XML,
            JSON
        }

        static FrameworkFactory()
        {

        }

        public static IDatabaseFramework GetFramework(Framework_e value)
        {
            IDatabaseFramework framework = null;

            switch(value)
            {
                case Framework_e.XML:
                    framework = new XMLFramework(); 
                    break;
                case Framework_e.JSON:
                    framework = new JSONFramework();
                    break;
                default:
                    break;
            }

            return framework;
        }
    }
}
