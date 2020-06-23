using System;
using System.Text.RegularExpressions;

namespace Ascentis.Infrastructure
{
    public class SqlRewriteRule
    {
        private Regex _databaseRegex;
        private string _databaseRegExPattern;
        private Regex _queryMatchRegEx;
        private string _queryMatchRegExPattern;

        public int Id { get; set; }
        public RegexOptions RegExOptions { get; set;}

        public string QueryReplacementString { get; set; }
 
        private Regex BuildRegEx(string pattern)
        {
            return new Regex(pattern, RegexOptions.Compiled | RegExOptions, new TimeSpan(0, 0, 1));
        }

        public string DatabaseRegEx
        {
            get => _databaseRegExPattern;
            set
            {
                if (value == _databaseRegExPattern)
                    return;
                if (value == "")
                {
                    _databaseRegex = null;
                    return;
                }
                _databaseRegExPattern = value;
                _databaseRegex = BuildRegEx(_databaseRegExPattern);
            }
        }
        
        public string QueryMatchRegEx
        {
            get => _queryMatchRegExPattern;
            set
            {
                if (value == _queryMatchRegExPattern)
                    return;
                if (value == "")
                {
                    _queryMatchRegEx = null;
                    return;
                }
                _queryMatchRegExPattern = value;
                _queryMatchRegEx = BuildRegEx(_queryMatchRegExPattern);
            }
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
