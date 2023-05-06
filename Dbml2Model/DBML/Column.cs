namespace Dbml2Model.DBML;

public class Column
{
	public string Name { get; set; }
	public string Type { get; set; }
	public bool IsPrimaryKey { get; set; }
	public bool IsDbGenerated { get; set; }
	public bool CanBeNull { get; set; }
	public string DbType { get; set; }
	public string UpdateCheck { get; set; }
	
	public override string ToString()
	{
		return Name??base.ToString();
	}
}
