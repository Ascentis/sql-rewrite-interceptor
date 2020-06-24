using System.Text.RegularExpressions;

namespace Ascentis.Infrastructure
{
    public class SqlRewriteModelBase
    {
        protected virtual Regex BuildRegEx(string pattern, RegexOptions regExOptions)
        {
            return new Regex(pattern, RegexOptions.Compiled | regExOptions);
        }

        protected void SetRegExProperty(string value, ref string patternField, ref Regex regexField, RegexOptions regExOptions = 0)
        {
            if (value == patternField)
                return;
            patternField = value;
            if (value == "")
            {
                regexField = null;
                return;
            }
            regexField = BuildRegEx(patternField, regExOptions);
        }
    }
}
