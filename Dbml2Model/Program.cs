using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Dbml2Model;

class Program
{
	static async Task<int> Main(string[] args)
	{
		var inFile = new Option<FileInfo?>(
			new []{"--file", "-i"},
			"DBML file");

		var outPath = new Option<string>(
			new []{"--outDir", "-o"},
			"Output models path");

		var rootCommand = new RootCommand("Convert Visual Studio .DBML(xml) to pure C# models.");
		rootCommand.AddOption(inFile);
		rootCommand.AddOption(outPath);

		// rootCommand.Handler = CommandHandler.Create<FileInfo, string>(ShowOutput);
		
		rootCommand.SetHandler((file, outputPath) =>
			{
				ShowOutput(file!, outputPath);
			},
			inFile,
			outPath);

		return await rootCommand.InvokeAsync(args);
	}
	
	static void ShowOutput(FileInfo dbmlFile, string outPath="")
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