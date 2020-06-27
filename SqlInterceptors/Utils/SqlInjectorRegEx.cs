using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Ascentis.Infrastructure.SqlInterceptors.Utils
{
    public class SqlInjectorRegEx
    {
        // RegEx stored in cache never expire
        private static readonly ConcurrentDictionary<RegExCacheKey, Regex> RegExCache = new ConcurrentDictionary<RegExCacheKey, Regex>();
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
            try
            {
                RegEx = Pattern != ""
                    ? RegExCache.GetOrAdd(new RegExCacheKey(pattern, _defaultOptions | regexOptions),
                        tuple => new Regex(tuple.Pattern, tuple.RegExOptions))
                    : null;
            }
            catch (Exception)
            {
                RegEx = null;
                throw;
            }
        }

        public void Recompile(RegexOptions regexOptions)
        {
            var oldPattern = Pattern;
            Set("");
            Set(oldPattern, regexOptions);
        }
    }
}
