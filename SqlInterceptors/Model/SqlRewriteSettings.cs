using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Ascentis.Infrastructure.SqlInterceptors.Model.Utils;

namespace Ascentis.Infrastructure.SqlInterceptors.Model
{
    public class SqlRewriteSettings
    {
        private const RegexOptions DefaultRegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        public int Id;
        
        private readonly SqlInjectorRegEx _machineRegEx = new SqlInjectorRegEx(DefaultRegexOptions);
        public string MachineRegEx
        {
            get => _machineRegEx.Pattern;
            set => _machineRegEx.Set(value);
        }

        private readonly SqlInjectorRegEx _processNameRegEx = new SqlInjectorRegEx(DefaultRegexOptions);
        public string ProcessNameRegEx
        {
            get => _processNameRegEx.Pattern;
            set => _processNameRegEx.Set(value);
        }

        public bool MatchProcessName()
        {
            return _processNameRegEx.RegEx != null && _processNameRegEx.RegEx.IsMatch(Environment.CommandLine);
        }

        public bool MatchMachineName()
        {
            return _machineRegEx.RegEx != null && _machineRegEx.RegEx.IsMatch(Environment.MachineName);
        }

        public bool Enabled { get; set; }
        public bool HashInjectionEnabled { get; set; }
        public bool RegExInjectionEnabled { get; set; }
        public bool StackFrameInjectionEnabled { get; set; }
        public int CallStackEntriesToReport { get; set; }
        public string StackFrameIgnorePrefixes { get; set; } = "";
    }
}
