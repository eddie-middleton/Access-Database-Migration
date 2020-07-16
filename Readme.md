# Microsoft Access Database Migration Utility

This Microsoft Access Database Migration tool is a `<utility>` that allows users to create SQL scripts of existing databases for import into other database engines. The utility is fairly crude and has limitations, but should provide a good basis for anyone wanting to migrate a legacy Access database.

The target for my implementation was SQLite, so this may need some tweaking to fit with the peculiarities of other database engines. Nonetheless any changes required should be largely cosmetic. 

## Guidelines

The following comments mat be helpful in troubleshooting: 
* The build targets .NET 4.7.2. This is a 32-bit application, targeting a <Win-X86> runtime.
* To read the Access files a Microsoft Access Runtime. This build targets the 32-bit 2010 runtime.
* The output SQL script has the potential to destroy existing information if table names are the same. **Care and appropriate backups are recommended. **

## Running the Application

This is a command line project that takes a filename (including path) as input:

```
<AccessDatabaseMigration [FilePath\Filename.extension]>
```

Output, in the form of a log and SQL script, are written to the same location.

## License
This utility is free to use and adapt. Now warrenty for use is provided, it is used 'as is' at the users risk and no support is offered. 
