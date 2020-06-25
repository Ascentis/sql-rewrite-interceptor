using System.Text.RegularExpressions;

namespace Ascentis.Infrastructure
{
    public class SqlRewriteRule : SqlRewriteModelBase
    {
        private Regex _databaseRegex;
        private string _databaseRegExPattern;
        private Regex _queryMatchRegEx;
        private string _queryMatchRegExPattern;

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

        private void RebuildRegEx(ref Regex regex, ref string patternField)
        {
            var oldPattern = patternField;
            patternField = "";
            SetRegExProperty(oldPattern, ref patternField, ref regex);
        }

        private void RecompileRegExes()
        {
            RebuildRegEx(ref _databaseRegex, ref _databaseRegExPattern);
            RebuildRegEx(ref _queryMatchRegEx, ref _queryMatchRegExPattern);
        }

        protected override Regex BuildRegEx(string pattern, RegexOptions regexOptions)
        {
            return base.BuildRegEx(pattern, RegexOptions.Compiled | RegexOptions.Singleline | regexOptions);
        }

        public string QueryReplacementString { get; set; }
 
        public string DatabaseRegEx
        {
            get => _databaseRegExPattern;
            set => SetRegExProperty(value, ref _databaseRegExPattern, ref _databaseRegex, RegExOptions);
        }
        
        public string QueryMatchRegEx
        {
            get => _queryMatchRegExPattern;
            set => SetRegExProperty(value, ref _queryMatchRegExPattern, ref _queryMatchRegEx, RegExOptions);
        }

        public bool MatchDatabase(string database)
        {
            return _databaseRegex != null && _databaseRegex.IsMatch(database);
        }

        public string ProcessQuery(string query)
        {
            return _queryMatchRegEx == null ? query : _queryMatchRegEx.Replace(query, QueryReplacementString);
        }
    }
}
