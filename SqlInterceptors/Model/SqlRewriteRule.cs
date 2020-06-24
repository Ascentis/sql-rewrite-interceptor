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
        public RegexOptions RegExOptions { get; set;}

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
