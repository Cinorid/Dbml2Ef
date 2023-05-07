# dbml2ef
Convert linq2sql or Visual Studio DBML file to Entity Framework models.

``` dotnet tool install -g Cinorid.dbml2ef ```

and then use it:

```
Description:
  Convert linq2sql or Visual Studio DBML file to Entity Framework models.

Usage:
  dbml2ef [options]

Options:
  -d, --dbml <file.dbml> (REQUIRED)  dbml input file
  -o, --outfolder <MyFolder>         Output models path
  -n, --namespace <MyNamespace>      Namespace of generated code (default: no namespace).
  -c, --context <MyDbContext>        Name of data context class (default: derived from database name).
  -e, --entitybase <MyBaseClass>     Base class of entity classes in the generated code (default: entities have no base class).
  --version                          Show version information
  -?, -h, --help                     Show help and usage information
```
