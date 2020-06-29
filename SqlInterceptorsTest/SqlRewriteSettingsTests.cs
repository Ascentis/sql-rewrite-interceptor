using Ascentis.Infrastructure.SqlInterceptors.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SqlInterceptorsTest.Properties;

namespace SqlInterceptorsTest
{
    [TestClass]
    public class SqlRewriteSettingsTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            TestUtils.InitTests();
        }

        [TestMethod]
        public void TestSettingsMatch()
        {
            var settingsObj = new SqlRewriteSettings
            {
                MachineRegEx = Settings.Default.MachineNameMatchString, 
                ProcessNameRegEx = "program Files", // We will ignore case for machine name and process matching
                StackFrameIgnorePrefixes = "Base.Second.Third\r\nAnother.One\rThird.One\nFourth.One"
            };
            Assert.IsTrue(settingsObj.MatchMachineName());
            Assert.IsTrue(settingsObj.MatchProcessName());
        }
    }
}
