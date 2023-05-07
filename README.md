# dbml2ef
Convert Visual Studio DBML (xml) file to Entity Framework models.

``` dotnet tool install -g Cinorid.dbml2ef ```

and then use it:

```
Description:
  Convert Visual Studio .DBML(xml) to pure C# models.

Usage:
  dbml2ef [options]

Options:
  -d, --dbml <dbml>              dbml input file
  -o, --outfolder <outfolder>    Output models path
  -n, --namespace <namespace>    Namespace of generated code (default: no namespace).
  -c, --context <context>        Name of data context class (default: derived from database name).
  -e, --entitybase <entitybase>  Base class of entity classes in the generated code (default: entities have no base class).
  --version                      Show version information
  -?, -h, --help                 Show help and usage information
```
