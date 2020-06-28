using System.Text.RegularExpressions;

namespace Ascentis.Infrastructure.SqlInterceptors.Model.Utils
{
    public readonly struct RegExCacheKey
    {
        public string Pattern { get; }
        public RegexOptions RegExOptions { get; }

        public RegExCacheKey(string pattern, RegexOptions regexOptions)
        {
            Pattern = pattern;
            RegExOptions = regexOptions;
        }
    }
}
