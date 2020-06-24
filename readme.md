# SQL-Rewrite-Interceptor

## Description

This package provides the ability to instrument a .NET application reliant on ado.net in a way that allows SQL re-write (sql text modification) in
a production environment without requiring applicaiton re-compilation.

This allows for query fast deployment (hot-fixing) of query optimization while working on changing source code application logic.

## Nuget package name

Ascentis.SQLRewriteInterceptor

## Dependencies

- Lib.Harmony
- Ascentis.Infrastructure

## Tables

```SQL
CREATE TABLE [SqlRewriteRegistry](
    [ID] [int] IDENTITY(1,1) NOT NULL,	-- ID field. No need to provide, it will be auto-incremented
    [DatabaseRegEx] [nvarchar](255) NOT NULL,	-- Regex to match against the database name where the query would run
    [QueryMatchRegEx] [nvarchar](max) NOT NULL,	-- Regex to match against the query
    [QueryReplacementString] [nvarchar](max) NOT NULL,	-- Replacement string according to .NET regex specs. Can use $1, $2 to replace group matches
    [RegExOptions] [int] NULL,	-- RegEx options, see RegEx options below
    CONSTRAINT [PK_SqlRewriteRegistry] PRIMARY KEY CLUSTERED 
    (
        [ID] ASC
    )
)
CREATE TABLE [dbo].[SqlRewriteInjectorSettings](
    [Id] [int] IDENTITY(1,1) NOT NULL,	-- ID field. Auto-incremented
    [MachineRegEx] [varchar](max) NOT NULL,	-- RegEx to match against machine name before deciding to update settings
    [ProcessNameRegEx] [varchar](max) NOT NULL,	-- RegEx to match against running process name
    [Enabled] [bit] NOT NULL,	-- Master Enabled switch
    [HashInjectionEnabled] [bit] NOT NULL,	-- Controls if hash injection is performed
    [RegExInjectionEnabled] [bit] NOT NULL,	-- Controls if RegEx injection is performed
    [StackFrameInjectionEnabled] [bit] NOT NULL,	-- Controls if stack frame injection is performed
    [CallStackEntriesToReport] [int] NOT NULL	-- Controls how many stack entries are going to be captured and injected
	CONSTRAINT [PK_SqlRewriteInjectorSettings] PRIMARY KEY CLUSTERED 
    (
	     [Id] ASC
    )
)
```
## RegEx options

### IgnoreCase = 1, 
Specifies case-insensitive matching. For more information, see the "Case-Insensitive Matching " section in the Regular Expression Options topic.
IMPORTANT: IgnoreCase always on for RegEx applied to settings

### Multiline = 2,    
Multiline mode. Changes the meaning of ^ and $ so they match at the beginning and end, respectively, of any line, and not just the beginning and end of the entire string. For more information, see the "Multiline Mode" section in the Regular Expression Options topic.

### ExplicitCapture = 4,    
Specifies that the only valid captures are explicitly named or numbered groups of the form (?&lt;name&gt;…). This allows unnamed parentheses to act as noncapturing groups without the syntactic clumsiness of the expression (?:…). For more information, see the "Explicit Captures Only" section in the Regular Expression Options topic.

### Compiled = 8,
Specifies that the regular expression is compiled to an assembly. This yields faster execution but increases startup time. This value should not be assigned to the <see cref="P:System.Text.RegularExpressions.RegexCompilationInfo.Options" /> property when calling the <see cref="M:System.Text.RegularExpressions.Regex.CompileToAssembly(System.Text.RegularExpressions.RegexCompilationInfo[],System.Reflection.AssemblyName)" /> method. For more information, see the "Compiled Regular Expressions" section in the Regular Expression Options topic.
IMPORTANT: Compiled ALWAYS on. Can't be shut off

### Singleline = 16, // 0x00000010
Specifies single-line mode. Changes the meaning of the dot (.) so it matches every character (instead of every character except \n). For more information, see the "Single-line Mode" section in the Regular Expression Options topic.
IMPORTANT: Singleline always on for regex applied to queries

### IgnorePatternWhitespace = 32, // 0x00000020    
Eliminates unescaped white space from the pattern and enables comments marked with #. However, this value does not affect or eliminate white space in , numeric , or tokens that mark the beginning of individual . For more information, see the "Ignore White Space" section of the Regular Expression Options topic.

### RightToLeft = 64, // 0x00000040 
Specifies that the search will be from right to left instead of from left to right. For more information, see the "Right-to-Left Mode" section in the Regular Expression Options topic.

### ECMAScript = 256, // 0x00000100    
Enables ECMAScript-compliant behavior for the expression. This value can be used only in conjunction with the <see cref="F:System.Text.RegularExpressions.RegexOptions.IgnoreCase" />, <see cref="F:System.Text.RegularExpressions.RegexOptions.Multiline" />, and <see cref="F:System.Text.RegularExpressions.RegexOptions.Compiled" /> values. The use of this value with any other values results in an exception.For more information on the <see cref="F:System.Text.RegularExpressions.RegexOptions.ECMAScript" /> option, see the "ECMAScript Matching Behavior" section in the Regular Expression Options topic.

### CultureInvariant = 512, // 0x00000200    
Specifies that cultural differences in language is ignored. For more information, see the "Comparison Using the Invariant Culture" section in the Regular Expression Options topic.

## Remarks

When doing regex replacements, it's recommended to append at the end of the entire string the following text: /*x*/
When the regex processor detects that string, it will more efficiently cut processing without having the compare the entire result
from the call to regex replacement and the old string before deciding to stop trying patterns.

## Sample usage

```C#

using System;
using System.Data.SqlClient;
using Ascentis.Infrastructure;

namespace ConsoleApp
{
    class Program
    {
        private const string Cs = "Server=<Your Server Name>; Database=<Your Database>; Trusted_Connection=True;";

        static void Main()
        {
            using (var repo = new SqlRewriteDbRepository(Cs))
            using (var svc = new SqlRewriteRuleService(repo))
            {
                var id = svc.AddRule(".*", "(.+ +)@@VERSION", "$1GETDATE()");
                try
                {
                    svc.Enabled = true;
                    using (var conn = new SqlConnection(Cs))
                    using (var cmd = new SqlCommand("SELECT   @@VERSION", conn))
                    {
                        conn.Open();
                        Console.WriteLine($"SQL: {cmd.CommandText}\r\nResult: {cmd.ExecuteScalar()}");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception: {e.Message}");
                }
                finally
                {
                    svc.RemoveRule(id);
                    Console.ReadLine();
                }
            }
        }
    }
}
```