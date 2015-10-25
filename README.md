[![Build status](https://ci.appveyor.com/api/projects/status/ebtg15yc3wls89yi/branch/master?svg=true)](https://ci.appveyor.com/project/edyoung/prequel/branch/master)

# Prequel

Prequel was born out of working on a product which contains a large number of database schemas and stored procedures. 
Every so often, a really basic error would be accidentally committed to the repository - sometimes, the SQL had syntax errors.
This wasn't caught until tests were run, which seemed wasteful. So I wanted a tool I could run on plain .sql files 
and integrate into the build system so that it would catch syntax errors and other errors earlier.

## Basic Usage
Say you have a file foo.sql which contains:

    create procedure sp_foo as 
	    declare @myvar as int
	    set @mvar = 2
    go

You can run prequel like so:

    PS C:\> .\Prequel.exe /warn:3 foo.sql

And it will print out

    Prequel version 0.1.0.0
    Warnings:
    c:\temp\foo.sql(1) : WARNING 4 : Procedure sp_foo should not be named with the prefix 'sp_'
    c:\temp\foo.sql(3) : WARNING 1 : Variable @mvar used before being declared
    c:\temp\foo.sql(1) : WARNING 3 : Procedure sp_foo does not SET NOCOUNT ON
    c:\temp\foo.sql(2) : WARNING 2 : Variable @myvar declared but never used

## Installation and prerequisites

Prequel requires the Microsoft SQL Server 2014 Transact-SQL ScriptDom package, which can be found on 
[http://www.microsoft.com/en-us/download/details.aspx?id=42295](http://www.microsoft.com/en-us/download/details.aspx?id=42295).
Click Download and choose the SqlDom.msi installer. To install Prequel itself, just unpack the .zip somewhere on your path. 
Note that even if you want to target an earlier version of SQL, you still want the 2014 ScriptDom package.

## Status
The tool works, but is unpolished and has a very small set of cases that it will detect so far. 
It works only on Windows, and only supports Microsoft SQL Server's T-SQL dialect of SQL.



