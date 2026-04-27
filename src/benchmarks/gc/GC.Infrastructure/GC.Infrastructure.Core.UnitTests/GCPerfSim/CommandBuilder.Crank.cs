using GC.Infrastructure.Core.CommandBuilders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GC.Infrastructure.Core.UnitTests.GCPerfSim
{
    [TestClass]
    public sealed class ConfigurationBuilder_Crank
    {
        [TestMethod]
        public void Dummy()
        {
            string path = Path.Combine(Common.CONFIGURATION_PATH, "SimpleValidConfiguration.yaml");
            //GCPerfSimCommandBuilder.BuildForServer()
        }
    }
}
