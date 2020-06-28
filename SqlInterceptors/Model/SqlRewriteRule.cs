using System.Text.RegularExpressions;
using Ascentis.Infrastructure.SqlInterceptors.Model.Utils;

namespace Ascentis.Infrastructure.SqlInterceptors.Model
{
    public class SqlRewriteRule
    {
        private const RegexOptions DefaultRegExOptions = RegexOptions.Compiled | RegexOptions.Singleline;
        private readonly SqlInjectorRegEx _databaseRegex = new SqlInjectorRegEx(DefaultRegExOptions);
        private readonly SqlInjectorRegEx _queryMatchRegEx = new SqlInjectorRegEx(DefaultRegExOptions);

        public int Id { get; set; }

        private RegexOptions _regexOptions;
        public RegexOptions RegExOptions
        {
            get => _regexOptions;
            set
            {
                if (_regexOptions == value)
                    return;
                _regexOptions = value;
                RecompileRegExes();
            }
        }
        
        private void RecompileRegExes()
        {
            _databaseRegex.Recompile(_regexOptions);
            _queryMatchRegEx.Recompile(_regexOptions);
        }

        public string QueryReplacementString { get; set; }
 
        public string DatabaseRegEx
        {
            get => _databaseRegex.Pattern;
            set => _databaseRegex.Set(value, RegExOptions);
        }
        
        public string QueryMatchRegEx
        {
            get => _queryMatchRegEx.Pattern;
            set => _queryMatchRegEx.Set(value, RegExOptions);
        }

        public bool MatchDatabase(string database)
        {
            return _databaseRegex.RegEx != null && _databaseRegex.RegEx.IsMatch(database);
        }

        public string ProcessQuery(string query)
        {
            return _queryMatchRegEx.RegEx == null ? query : _queryMatchRegEx.RegEx.Replace(query, QueryReplacementString);
        }
    }
}
