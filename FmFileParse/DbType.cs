namespace FmFileParse;

internal readonly struct DbType<T>
{
    public bool IsDbNull { get; }

    public T Value { get; } = default!;

    public DbType(object value)
    {
        IsDbNull = value == DBNull.Value;
        if (!IsDbNull)
        {
            Value = (T)Convert.ChangeType(value, typeof(T));
        }
    }

    public object GetValue()
        => IsDbNull ? DBNull.Value : Value!;

    public static readonly DbType<T> DbNull = new(DBNull.Value);
}
