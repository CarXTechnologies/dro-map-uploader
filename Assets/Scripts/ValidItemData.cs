using System;
using System.Collections.Generic;
using UnityEngine;

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
        this.data = data == null ? new List<ValidItem>() : new List<ValidItem>(data);
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
            (ValidItem[])data?.ToArray().Clone());
    }
}

public interface IValidComponentProcess
{
    public bool isSuccess { get;}
    public string processMessage { get;}
    public void ValidProcess(Component comp);

    public void Reset();
}

[Serializable]
public struct ValidItem : IValidComponentProcess
{
    public string type;
    public int min;
    public int max;
    public int current;
    public IValidComponentProcess validComponentProcess;
    public List<Component> components;
    private bool m_isSuccess;
    
    public ValidItem(
        string type, 
        int min, 
        int max, 
        IValidComponentProcess validComponentProcess = null, 
        int current = 0, 
        List<Component> components = null)
    {
        this.type = type;
        this.min = min;
        this.max = max;
        this.current = current;
        this.validComponentProcess = validComponentProcess;
        this.components = components ?? new List<Component>();
        m_isSuccess = true;
    }

    public string ToStat()
    {
        return $"{type} : {current} count ({min} min - {max} max)";
    }
    
    public override string ToString()
    {
        var result = string.Empty;
        if (validComponentProcess is { isSuccess: false })
        {
            result += validComponentProcess.processMessage + "\n";
        }
        
        if (current < min)
        {
            result += $"There are less than {min} {type}\n";
        }
            
        if (current > max)
        {
            result += $"There are more than {max} {type}\n";
        }

        return result;
    }

    public void ValidProcess()
    {
        foreach (var component in components)
        {
            ValidProcess(component);
        }
        
        m_isSuccess = m_isSuccess && (current >= min && current <= max);
    }
    
    public void ValidProcess(Component comp)
    {
        validComponentProcess?.ValidProcess(comp);
        m_isSuccess = m_isSuccess && (validComponentProcess?.isSuccess ?? true);
    }

    public void Reset()
    {
        validComponentProcess?.Reset();
        components.Clear();
        m_isSuccess = true;
    }

    public bool isSuccess => m_isSuccess;
    public string processMessage => ToString();
}