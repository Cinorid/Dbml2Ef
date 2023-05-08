using System.CommandLine;
using System.CommandLine.Help;
using System.Text;
using System.Xml.Linq;
using Dbml2Ef.DBML;

namespace Dbml2Ef;

class Program
{
	static async Task<int> Main(string[] args)
	{
		var inDbmlFile = new Option<FileInfo?>(
			new []{"--dbml", "-d"},
			"dbml input file")
		{
			IsRequired = true,
			ArgumentHelpName = "file.dbml"
		};

		var outfolder = new Option<string>(
			new []{"--outfolder", "-o"},
			"Output models path")
		{
			ArgumentHelpName = "MyFolder"
		};
		
		var outNamespace = new Option<string>(
			new []{"--namespace", "-n"},
			"Namespace of generated code (default: no namespace).")
		{
			ArgumentHelpName = "MyNamespace"
		};
		
		var outContext = new Option<string>(
			new []{"--context", "-c"},
			"Name of data context class (default: derived from database name).")
		{
			ArgumentHelpName = "MyDbContext",
		};
		
		var outEntitybase = new Option<string>(
			new []{"--entitybase", "-e"},
			"Base class of entity classes in the generated code (default: entities have no base class).")
		{
			ArgumentHelpName = "MyBaseClass"
		};

		var rootCommand = new RootCommand()
		{
			Name = "dbml2ef",
			Description = "Convert linq2sql or Visual Studio DBML file to Entity Framework models.",
		};
		rootCommand.AddOption(inDbmlFile);
		rootCommand.AddOption(outfolder);
		rootCommand.AddOption(outNamespace);
		rootCommand.AddOption(outContext);
		rootCommand.AddOption(outEntitybase);

		// rootCommand.Handler = CommandHandler.Create<FileInfo, string, string, string, string>(DoConvert);

		rootCommand.SetHandler(
			DoConvert,
			inDbmlFile,
			outfolder,
			outNamespace,
			outContext,
			outEntitybase);

		return await rootCommand.InvokeAsync(args);
	}

	static void DoConvert(FileInfo? dbmlFile, string outPath, string namespaceName, string contextName, string entityBase)
	{
		string? fileContent;
		try
		{
			fileContent = dbmlFile?.OpenText().ReadToEnd();
		}
		catch (Exception e)
		{
			Console.WriteLine(e.Message);
			return;
		}

		if (string.IsNullOrEmpty(outPath))
		{
			outPath = System.Environment.CurrentDirectory;
		}

		if (fileContent is null)
			return;
		
		XElement content = XElement.Parse(fileContent);

		var dbModelDatabase = ConvertXElementToDatabase(content);

		GenerateModelClass(dbModelDatabase, namespaceName, outPath, entityBase);
		GenerateDataContextClass(dbModelDatabase, namespaceName, outPath, contextName);
		
		Console.WriteLine($"{dbModelDatabase.Tables.Count} models Created.");
	}

	private static void GenerateModelClass(Database dbModelDatabase, string namespaceName, string outPath, string entityBase)
	{
		foreach (var table in dbModelDatabase.Tables)
		{
			var fileName = table.Type + ".cs";
			Console.WriteLine($"Generating {fileName}");

			var strBuilder = new StringBuilder();
			strBuilder.AppendLine("using System;");
			strBuilder.AppendLine("using System.Collections.Generic;");
			strBuilder.AppendLine("");
			var namespaceIndentation = "";
			if (!string.IsNullOrEmpty(namespaceName))
			{
				namespaceIndentation = "    ";
				strBuilder.AppendLine($"namespace {namespaceName}");
				strBuilder.AppendLine("{"); // begin namespace
			}

			strBuilder.AppendLine($"{namespaceIndentation}public partial class {table.Type}{(!string.IsNullOrEmpty(entityBase) ? " : " + entityBase : "")}");
			strBuilder.AppendLine($"{namespaceIndentation}{{"); // begin class

			foreach (var column in table.Columns)
			{
				strBuilder.AppendLine($"{namespaceIndentation}    {ColumnToString(column)}");
				strBuilder.AppendLine("");
			}

			foreach (var association in table.Associations)
			{
				if (association.IsForeignKey)
				{
					strBuilder.AppendLine($"{namespaceIndentation}    public virtual {association.Type} {association.Member} {{ get; set; }} = null!;");
				}
				else
				{
					strBuilder.AppendLine($"{namespaceIndentation}    public virtual ICollection<{association.Type}> {association.Member} {{ get; set; }} = new List<{association.Type}>();");
				}

				strBuilder.AppendLine("");
			}

			// remove last empty line
			var last = strBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal);
			if (last >= 0)
			{
				strBuilder.Remove(last, strBuilder.Length - last);
			}

			strBuilder.AppendLine($"{namespaceIndentation}}}"); // end class

			if (!string.IsNullOrEmpty(namespaceName))
			{
				strBuilder.AppendLine("}"); // end namespace
			}

			if (!Directory.Exists(outPath))
			{
				Directory.CreateDirectory(outPath);
			}

			var outFilePath = Path.Combine(outPath, fileName);
			File.WriteAllText(outFilePath, strBuilder.ToString());
		}
	}

	private static void GenerateDataContextClass(Database dbModelDatabase, string namespaceName, string outPath, string? contextName)
	{
		if (string.IsNullOrEmpty(contextName))
		{
			contextName = dbModelDatabase.Class;
		}
		
		var fileName = contextName + ".cs";
		Console.WriteLine($"Generating {fileName}");

		var strBuilder = new StringBuilder();
		strBuilder.AppendLine("using System;");
		strBuilder.AppendLine("using System.Collections.Generic;");
		strBuilder.AppendLine("using Microsoft.EntityFrameworkCore;");
		strBuilder.AppendLine("");
		var namespaceIndentation = "";
		if (!string.IsNullOrEmpty(namespaceName))
		{
			namespaceIndentation = "    ";
			strBuilder.AppendLine($"namespace {namespaceName}");
			strBuilder.AppendLine("{"); // begin namespace
		}

		strBuilder.AppendLine($"{namespaceIndentation}public partial class {contextName} : DbContext");
		strBuilder.AppendLine($"{namespaceIndentation}{{"); // begin class

		strBuilder.AppendLine($"{namespaceIndentation}    public {contextName}()");
		strBuilder.AppendLine($"{namespaceIndentation}    {{");
		strBuilder.AppendLine($"{namespaceIndentation}    }}");
		strBuilder.AppendLine($"{namespaceIndentation}    ");
		strBuilder.AppendLine($"{namespaceIndentation}    public {contextName}(DbContextOptions<{contextName}> options)");
		strBuilder.AppendLine($"{namespaceIndentation}        : base(options)");
		strBuilder.AppendLine($"{namespaceIndentation}    {{");
		strBuilder.AppendLine($"{namespaceIndentation}    }}");
		strBuilder.AppendLine($"{namespaceIndentation}    ");

		foreach (var table in dbModelDatabase.Tables)
		{
			strBuilder.AppendLine($"{namespaceIndentation}    public virtual DbSet<{table.Type}> {table.Name} {{ get; set; }}");
			strBuilder.AppendLine("");
		}

		// remove last empty line
		var last = strBuilder.ToString().LastIndexOf(Environment.NewLine, StringComparison.Ordinal);
		if (last >= 0)
		{
			strBuilder.Remove(last, strBuilder.Length - last);
		}

		strBuilder.AppendLine($"{namespaceIndentation}}}"); // end class

		if (!string.IsNullOrEmpty(namespaceName))
		{
			strBuilder.AppendLine("}"); // end namespace
		}

		if (!Directory.Exists(outPath))
		{
			Directory.CreateDirectory(outPath);
		}

		var outFilePath = Path.Combine(outPath, fileName);
		File.WriteAllText(outFilePath, strBuilder.ToString());
	}

	private static DBML.Database ConvertXElementToDatabase(XElement content)
	{
		if (content.Name.LocalName == "Database")
		{
			var db = new DBML.Database();
			db.Class = content.Attribute("Class")?.Value;
			db.BaseType = content.Attribute("BaseType")?.Value;

			db.Tables = new List<DBML.Table>();
			var tableElements = content?.Elements().ToList();
			if (tableElements == null)
				return db;
			
			foreach (var tableElement in tableElements)
			{
				var table = new DBML.Table();
				table.Name = tableElement?.Attribute("Name")?.Value;

				var typeElement = tableElement?.Elements().FirstOrDefault();
				table.Type = typeElement?.Attribute("Name")?.Value;

				table.Columns = new List<DBML.Column>();
				table.Associations = new List<DBML.Association>();
				var columns = typeElement?.Elements().ToList();
				if (columns != null)
				{
					foreach (var elementColumn in columns)
					{
						var elementName = elementColumn.Name.LocalName;
						if (elementName == "Column")
						{
							var column = new DBML.Column();
							column.Name = elementColumn?.Attribute("Name")?.Value;
							column.Type = elementColumn?.Attribute("Type")?.Value;
							column.DbType = elementColumn?.Attribute("DbType")?.Value;
							column.UpdateCheck = elementColumn?.Attribute("UpdateCheck")?.Value;
							column.AutoSync = elementColumn?.Attribute("AutoSync")?.Value;

							var canBeNull = elementColumn?.Attribute("CanBeNull")?.Value;
							column.CanBeNull = Convert.ToBoolean(canBeNull ?? false.ToString());
							
							var isDbGenerated = elementColumn?.Attribute("IsDbGenerated")?.Value;
							column.IsDbGenerated = Convert.ToBoolean(isDbGenerated ?? false.ToString());
							
							var isPrimaryKey = elementColumn?.Attribute("IsPrimaryKey")?.Value;
							column.IsPrimaryKey = Convert.ToBoolean(isPrimaryKey ?? false.ToString());

							table.Columns.Add(column);
						}
						else if (elementName == "Association")
						{
							var association = new DBML.Association();
							association.Name = elementColumn?.Attribute("Name")?.Value;
							association.Type = elementColumn?.Attribute("Type")?.Value;
							association.ThisKey = elementColumn?.Attribute("ThisKey")?.Value;
							association.OtherKey = elementColumn?.Attribute("OtherKey")?.Value;
							association.Member = elementColumn?.Attribute("Member")?.Value;

							var isForeignKey = elementColumn?.Attribute("IsForeignKey")?.Value;
							association.IsForeignKey = Convert.ToBoolean(isForeignKey ?? false.ToString());

							table.Associations.Add(association);
						}
					}
				}

				db.Tables.Add(table);
			}

			return db;
		}

		throw new FormatException("file format is not valid.");
	}

	private static string ColumnToString(DBML.Column column)
	{
		string type;
		string initStr = "";
		switch (column.Type)
		{
			case "System.Int32":
				type = $"int{(column.CanBeNull ? "?" : "")}";
				break;
			case "System.Int64":
				type = $"long{(column.CanBeNull ? "?" : "")}";
				break;
			case "System.Single":
				type = $"float{(column.CanBeNull ? "?" : "")}";
				break;
			case "System.Double":
				type = $"double{(column.CanBeNull ? "?" : "")}";
				break;
			case "System.Byte[]":
				type = $"byte[]{(column.CanBeNull ? "?" : "")}";
				break;
			case "System.DateTime":
				type = $"DateTime{(column.CanBeNull ? "?" : "")}";
				break;
			case "System.Boolean":
				type = $"bool{(column.CanBeNull ? "?" : "")}";
				break;
			case "System.String":
				type = $"string{(column.CanBeNull ? "?" : "")}";
				initStr = !column.CanBeNull ? " = null!;" : "";
				break;
			default:
				type = $"{column.Type}{(column.CanBeNull ? "?" : "")}";
				break;
		}

		var str = $"public {type} {column.Name} {{ get; set; }}{initStr}";

		return str;
	}
}