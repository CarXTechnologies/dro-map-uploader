using System;
using System.Collections.Generic;

[Serializable]
public struct ValidItemData : ICloneable
{
    public List<ValidItem> data;
    public readonly int vertexCountForDistance;
    public readonly float vertexDistanceForMaxCount;
    public readonly float maxSizeInMb;
    public readonly float maxSizeInMbMeta;

    public ValidItemData(float maxSizeInMb = 512f, float maxSizeInMbMeta = 24f, float vertexDistanceForMaxCount = 1f, 
        int vertexCountForDistance = 10000000, params ValidItem[] data)
    {
        this.vertexCountForDistance = vertexCountForDistance;
        this.vertexDistanceForMaxCount = vertexDistanceForMaxCount;
        this.maxSizeInMb = maxSizeInMb;
        this.maxSizeInMbMeta = maxSizeInMbMeta;
        this.data = new List<ValidItem>(data);
    }

    public override string ToString()
    {
        var result = string.Empty;

        if (data != null)
        {
            foreach (var item in data)
            {
                if (item.current != 0)
                {
                    result += item.ToStat() + "\n";
                }
            }
        }

        return result;
    }

    public object Clone()
    {
        return new ValidItemData(maxSizeInMb, maxSizeInMbMeta, vertexDistanceForMaxCount, vertexCountForDistance, 
            (ValidItem[])data.ToArray().Clone());
    }
}

[Serializable]
public struct ValidItem
{
    public string type;
    public int min;
    public int max;
    public int current;

    public ValidItem(string type, int min, int max, int current = 0)
    {
        this.type = type;
        this.min = min;
        this.max = max;
        this.current = current;
    }

    public string ToStat()
    {
        return $"{type} : {current} count ({min} min - {max} max)";
    }
    
    public override string ToString()
    {
        if (current < min)
        {
            return $"There are less than {min} {type}";
        }
            
        if (current > max)
        {
            return $"There are more than {max} {type}";
        }

        return string.Empty;
    }
}