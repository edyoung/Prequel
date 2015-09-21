
### Warning 1 : Undeclared Variable used
Critical

A variable which was not declared was referenced or set. Declare it before use, for example 'DECLARE @variable AS INT'


### Warning 2 : Unused Variable declared
Minor

A variable or parameter was declared, but never referenced. It could be removed without affecting the procedure's logic, or this could indicate a typo or logical error


### Warning 3 : Procedure without SET NOCOUNT ON
Minor

Performance for stored procedures can be increased with the SET NOCOUNT ON option. The difference can range from tiny to substantial depending on the nature of the sproc. 
Some SQL tools require the rowcount to be returned - if you use one of those, suppress this warning.


### Warning 4 : Procedure name begins with sp_
Serious

sp_ is a reserved prefix in SQL server. Even a sproc which does not clash with any system procedure incurs a performance penalty when using this prefix. Rename the procedure

