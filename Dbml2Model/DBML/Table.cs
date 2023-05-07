namespace Dbml2Model.DBML;

public class Table
{
	public string? Type { get; set; }
	public string? Name { get; set; }
	
	public List<Column> Columns { get; set; }
	public List<Association> Associations { get; set; }
	
	public override string ToString()
	{
		return Name??base.ToString();
	}
}
