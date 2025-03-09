namespace FmFileParse.SaveImport;

public class AttributeGroupAttribute : Attribute
{
    public AttributeGroup Grouping { get; set; }

    public AttributeGroupAttribute(AttributeGroup grouping)
    {
        Grouping = grouping;
    }
}
