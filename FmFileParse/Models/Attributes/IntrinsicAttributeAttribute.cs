namespace FmFileParse.Models.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
internal class IntrinsicAttributeAttribute(IntrinsicType type) : Attribute
{
    public IntrinsicType Type { get; } = type;
}
