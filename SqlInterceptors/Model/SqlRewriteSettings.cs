using System;
using System.Text.RegularExpressions;

namespace Ascentis.Infrastructure
{
    public class SqlRewriteSettings : SqlRewriteModelBase
    {
        public int Id;

        protected override Regex BuildRegEx(string pattern, RegexOptions regexOptions)
        {
            return base.BuildRegEx(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase | regexOptions);
        }

        private Regex _machineRegEx;
        private string _machineRegExPattern;
        public string MachineRegEx
        {
            get => _machineRegExPattern;
            set => SetRegExProperty(value, ref _machineRegExPattern, ref _machineRegEx);
        }

        private Regex _processNameRegEx;
        private string _processNameRegExPattern;
        public string ProcessNameRegEx
        {
            get => _processNameRegExPattern;
            set => SetRegExProperty(value, ref _processNameRegExPattern, ref _processNameRegEx);
        }

        public bool MatchProcessName()
        {
            return _processNameRegEx != null && _processNameRegEx.IsMatch(Environment.CommandLine);
        }

        public bool MatchMachineName()
        {
            return _machineRegEx != null && _machineRegEx.IsMatch(Environment.MachineName);
        }

        public bool Enabled { get; set; }
        public bool HashInjectionEnabled { get; set; }
        public bool RegExInjectionEnabled { get; set; }
        public bool StackFrameInjectionEnabled { get; set; }
        public int CallStackEntriesToReport { get; set; }
    }
}
