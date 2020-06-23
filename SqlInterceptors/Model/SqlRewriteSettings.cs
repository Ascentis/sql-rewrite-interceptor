using System;
using System.Text.RegularExpressions;

namespace Ascentis.Infrastructure
{
    public class SqlRewriteSettings
    {
        public int Id;

        private Regex BuildRegEx(string pattern)
        {
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private Regex _machineRegEx;
        private string _machineRegExPattern;
        public string MachineRegEx
        {
            get => _machineRegExPattern;
            set
            {
                if (value == _machineRegExPattern)
                    return;
                if (value == "")
                {
                    _machineRegEx = null;
                    return;
                }
                _machineRegExPattern = value;
                _machineRegEx = BuildRegEx(_machineRegExPattern);
            }
        }

        private Regex _processNameRegEx;
        private string _processNameRegExPattern;
        public string ProcessNameRegEx
        {
            get => _processNameRegExPattern;
            set
            {
                if (value == _processNameRegExPattern)
                    return;
                if (value == "")
                {
                    _processNameRegEx = null;
                    return;
                }
                _processNameRegExPattern = value;
                _processNameRegEx = BuildRegEx(_processNameRegExPattern);
            }
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
    }
}
