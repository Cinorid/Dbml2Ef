namespace Dbml2Model.DBML;

public class Database
{
	public string Class { get; set; }
	public string BaseType { get; set; }
	public string xmlns { get; set; }

	public List<Table> Tables { get; set; }

	public override string ToString()
	{
		return Class??base.ToString();
	}
}
