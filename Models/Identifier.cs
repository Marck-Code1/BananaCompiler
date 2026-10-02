namespace BananaCompiler.Models
{
    internal class Identifier
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public string? DataType { get; set; }
        public string? Value { get; set; }
        public string Scope { get; set; } = "global";
        public int Address { get; set; }
        public int Size { get; set; }
        public string AddressHex => $"0x{Address:X4}";
    }
}
