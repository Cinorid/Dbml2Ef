using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Dbml2Model;

class Program
{
	static async Task<int> Main(string[] args)
	{
		var inDbmlFile = new Option<FileInfo?>(
			new []{"--dbml", "-d"},
			"dbml input file");

		var outfolder = new Option<string>(
			new []{"--outfolder", "-o"},
			"Output models path");
		
		var outNamespace = new Option<string>(
			new []{"--namespace", "-n"},
			"Namespace of generated code (default: no namespace).");
		
		var outContext = new Option<string>(
			new []{"--context", "-c"},
			"Name of data context class (default: derived from database name).");
		
		var outEntitybase = new Option<string>(
			new []{"--entitybase", "-e"},
			"Base class of entity classes in the generated code (default: entities have no base class).");

		var rootCommand = new RootCommand("Convert Visual Studio .DBML(xml) to pure C# models.");
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

	static void DoConvert(FileInfo? dbmlFile, string outPath, string namespaceName, string context, string entitybase)
	{
		string fileContent;
		try
		{
			fileContent = dbmlFile.OpenText().ReadToEnd();
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

		XElement content = XElement.Parse(fileContent);

		var dbModelDatabase = ConvertXElementToDatabase(content);

		foreach (var table in dbModelDatabase.Tables)
		{
			var fileName = table.Name + ".cs";
			Console.WriteLine($"Generating {fileName}");
			
			var strBuilder = new StringBuilder();
			strBuilder.AppendLine("using System;");
			strBuilder.AppendLine("");
			var namespaceIndentation = "";
			if (!string.IsNullOrEmpty(namespaceName))
			{
				namespaceIndentation = "    ";
				strBuilder.AppendLine($"namespace +{namespaceName}");
				strBuilder.AppendLine("");
				strBuilder.AppendLine("{");
			}

			strBuilder.AppendLine($"{namespaceIndentation}public partial class {table.Type}");
			strBuilder.AppendLine($"{namespaceIndentation}{{");

			foreach (var column in table.Columns)
			{
				strBuilder.AppendLine($"{namespaceIndentation}    public {column.Type} {column.Name} {{ get; set; }}");
			}

			strBuilder.AppendLine($"{namespaceIndentation}}}");

			if (!string.IsNullOrEmpty(namespaceName))
			{
				strBuilder.AppendLine("}");
			}

			if (!Directory.Exists(outPath))
			{
				Directory.CreateDirectory(outPath);
			}

			var outFilePath = Path.Combine(outPath, fileName);
			File.WriteAllText(outFilePath, strBuilder.ToString());
			
		}
		
		Console.WriteLine($"{dbModelDatabase.Tables.Count} models Created.");
	}

	private static DBML.Database ConvertXElementToDatabase(XElement content)
	{
		if (content.Name.LocalName == "Database")
		{
			var db = new DBML.Database();
			db.Class = content?.Attribute("Class")?.Value;
			db.BaseType = content?.Attribute("BaseType")?.Value;

			db.Tables = new List<DBML.Table>();
			var tableElements = content?.Elements().ToList();
			foreach (var tableElement in tableElements)
			{
				var table = new DBML.Table();
				table.Name = tableElement?.Attribute("Name")?.Value;

				var typeElement = tableElement?.Elements().FirstOrDefault();
				table.Type = typeElement?.Attribute("Name")?.Value;

				table.Columns = new List<DBML.Column>();
				table.Associations = new List<DBML.Association>();
				var columns = typeElement?.Elements().ToList();
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
						// column.IsDbGenerated = elementColumn?.Attribute("IsDbGenerated")?.Value;
						// column.IsPrimaryKey = elementColumn?.Attribute("IsPrimaryKey")?.Value;

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
						// association.IsForeignKey = elementColumn?.Attribute("IsForeignKey")?.Value;

						table.Associations.Add(association);
					}
				}

				db.Tables.Add(table);
			}

			return db;
		}

		return null;
	}
}