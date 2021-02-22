#nullable disable
namespace Connect4
{
    public abstract record StringEnumBase
    {
        protected StringEnumBase() { }
        public string Value { get; protected init; }
    }
}