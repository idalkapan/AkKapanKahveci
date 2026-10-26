class StudentDocument
{
    public string Id { get; set; } = "";
    public string Session { get; set; } = "";
    public string Status { get; set; } = "pending";
    public string Extension { get; set; } = "";
    public DateTime Created { get; set; } = DateTime.UtcNow;
}
