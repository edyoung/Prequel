
### Warning PQL0001 : Undeclared Variable used
Critical

A variable which was not declared was referenced or set. Declare it before use, for example 'DECLARE @variable AS INT'


### Warning PQL0002 : Unused Variable declared
Minor

A variable or parameter was declared, but never referenced. It could be removed without affecting the procedure's logic, or this could indicate a typo or logical error


### Warning PQL0003 : Procedure without SET NOCOUNT ON
Minor

Performance for stored procedures can be increased with the SET NOCOUNT ON option. The difference can range from tiny to substantial depending on the nature of the sproc. 
    Some SQL tools require the rowcount to be returned - if you use one of those, suppress this warning.


### Warning PQL0004 : Procedure name begins with sp_
Serious

sp_ is a reserved prefix in SQL server. Even a sproc which does not clash with any system procedure incurs a performance penalty when using this prefix. Rename the procedure.


### Warning PQL0005 : Fixed-length or variable-length variable assigned a value greater than it can hold
Serious

A variable was assigned a string which is too large for it to hold. The string will be truncated, which is probably not desired.


### Warning PQL0006 : 8-bit variable assigned a unicode value
Serious

A variable is of 8-bit (char or varchar) type but is assigned a unicode value. This will mangle the text if it contains characters which can't be represented. Use CONVERT to explicitly indicate how you want this handled.

