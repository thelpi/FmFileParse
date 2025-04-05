using FmFileParse.Models.Attributes;
using FmFileParse.Models.Internal;

namespace FmFileParse.Models;

public abstract class BaseData
{
    [DataPosition(0)]
    public int Id { get; set; }

    public abstract IEnumerable<string> Describe(BaseFileData data);

    protected static IEnumerable<string> SubDescribe<T>(
        BaseFileData data,
        int id,
        Func<BaseFileData, Dictionary<int, T>> getSubData,
        string fromDataType)
        where T : BaseData
    {
        yield return string.Empty;
        yield return $"---- {typeof(T).Name} (from {fromDataType}) details ----";
        getSubData(data).TryGetValue(id, out var subData);
        if (subData is not null)
        {
            foreach (var row in subData.Describe(data))
            {
                yield return row;
            }
        }
        else
        {
            yield return id >= 0
                ? $"No {typeof(T).Name} with id {id} found!"
                : $"{typeof(T).Name} is not set on the {fromDataType}.";
        }
    }
}
