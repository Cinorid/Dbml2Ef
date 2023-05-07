namespace Dbml2Ef.DBML;

public class Association
{
	public string? Name { get; set; }
	public string? Type { get; set; }
	public bool IsForeignKey { get; set; }
	public string? Member { get; set; }
	public string? ThisKey { get; set; }
	public string? OtherKey { get; set; }
	
	public override string ToString()
	{
		return Name??base.ToString();
	}
}
