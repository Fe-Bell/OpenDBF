using OpenDBF.DAT;
using OpenDBF.JSON;
using OpenDBF.Shared.Interface;
using OpenDBF.XML;

namespace OpenDBF.Core
{
    /// <summary>
    /// Static factory to build database frameworks.
    /// </summary>
    public static class FrameworkFactory
    {
        /// <summary>
        /// Enum for known database frameworks
        /// </summary>
        public enum Framework_e
        {
            XML,
            JSON,
            DAT
        }

        /// <summary>
        /// Constructs a new instance of an OpenDBF database framework.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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
                case Framework_e.DAT:
                    framework = new DATFramework();
                    break;
                default:
                    break;
            }

            return framework;
        }
    }
}
