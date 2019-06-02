namespace DigitService.Impl.Logging
{
    public class UserLogQueryResponse
    {
        public Table[] tables { get; set; }
    }

    public class Table
    {
        public string name { get; set; }
        public Column[] columns { get; set; }
        public object[][] rows { get; set; }
    }

    public class Column
    {
        public string name { get; set; }
        public string type { get; set; }
    }
}