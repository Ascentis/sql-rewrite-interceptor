using System.Text.RegularExpressions;

namespace Ascentis.Infrastructure.SqlInterceptors.Utils
{
    public class SqlInjectorRegEx
    {
        public Regex RegEx { get; private set; }
        public string Pattern { get; private set; }
        private readonly RegexOptions _defaultOptions;

        public SqlInjectorRegEx(RegexOptions defaultOptions = 0)
        {
            _defaultOptions = defaultOptions;
        }

        public void Set(string pattern, RegexOptions regexOptions = 0)
        {
            if (pattern == Pattern)
                return;
            Pattern = pattern;
            RegEx = Pattern != "" ? new Regex(pattern, _defaultOptions | regexOptions) : null;
        }

        public void Recompile(RegexOptions regexOptions)
        {
            var oldPattern = Pattern;
            Set("");
            Set(oldPattern, regexOptions);
        }
    }
}
