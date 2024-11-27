namespace Alga.telegram.Models;
public class User
{
    public long id { get; set; }
    public bool? is_bot { get; set; }
    public string? first_name { get; set; }
    public string? last_name { get; set; }
    public string? username { get; set; }
    public string? language_code { get; set; }
    public bool? can_join_groups { get; set; }
}
