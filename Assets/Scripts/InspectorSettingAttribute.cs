using UnityEngine;

public class InspectorSettingAttribute : PropertyAttribute
{
    public string newName { get ; private set; }	
    public bool isExpand { get ; private set; }
    public bool isLock { get ; private set; }	
    
    public InspectorSettingAttribute(string name = "", bool isExpand = true, bool isLock = false)
    {
        newName = name;
        this.isExpand = isExpand;
        this.isLock = isLock;
    }
}