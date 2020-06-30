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
Specifies case-insensitive matching.
##### IMPORTANT: IgnoreCase always on for RegEx applied to settings

### Multiline = 2,    
Multiline mode. Changes the meaning of ^ and $ so they match at the beginning and end, respectively, of any line, and not just the beginning and end of the entire string.

### ExplicitCapture = 4,    
Specifies that the only valid captures are explicitly named or numbered groups of the form (?\<name>...). This allows unnamed parentheses to act as noncapturing groups without the syntactic clumsiness of the expression (?:…).

### Singleline = 16, // 0x00000010
Specifies single-line mode. Changes the meaning of the dot (.) so it matches every character (instead of every character except \n).
##### IMPORTANT: Singleline always on for regex applied to queries and database name

### IgnorePatternWhitespace = 32, // 0x00000020
Eliminates unescaped white space from the pattern and enables comments marked with #. However, this value does not affect or eliminate white space in character classes, numeric quantifiers, or tokens that mark the beginning of individual regular expression language elements.

### RightToLeft = 64, // 0x00000040 
Specifies that the search will be from right to left instead of from left to right.

### ECMAScript = 256, // 0x00000100    
Enables ECMAScript-compliant behavior for the expression. 

### CultureInvariant = 512, // 0x00000200    
Specifies that cultural differences in language is ignored.

## Remarks

When doing regex replacements, it's recommended to append at the end of the entire string the following text: /\*x*/
When the regex processor detects that string, it will more efficiently cut processing without having the compare the entire result
from the call to regex replacement and the old string before deciding to stop trying patterns.

This is an example on how to achieve this using capturing groups to replace the entire SQL (that's required in order to append at the end the /\*x*/ piece of text):

Given this SQL: "SELECT @@VERSION"

RegEx: "(.\*)SELECT @@VERSION(.*)"

The replacement that will add at the end the mentioned string: "$1SELECT GETDATE()$2\r\n/\*x*/"

In this example $1 replaces all text right before SELECT... and $2 replaces all text after the matched text.
Finally, right after that we append /\*x*/. That signals the code that RegEx has already happened, and due to the nature
of how this library works it will prevent attempting RegEx again (with the corresponding impact on performance).

