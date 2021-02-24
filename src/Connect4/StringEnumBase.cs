#nullable disable
namespace Connect4
{
    public abstract class StringEnumBase
    {
        protected StringEnumBase() { }
        public string Value { get; protected init; }
        public override string ToString() => Value;
    }
}