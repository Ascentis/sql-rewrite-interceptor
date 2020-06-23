namespace Ascentis.Infrastructure
{
    public class SqlRewriteSettings
    {
        public int Id;
        public string MachineRegEx;
        public string ProcessNameRegEx;
        public bool Enabled;
        public bool HashInjectionEnabled;
        public bool RegExInjectionEnabled;
        public bool StackFrameInjectionEnabled;
    }
}
