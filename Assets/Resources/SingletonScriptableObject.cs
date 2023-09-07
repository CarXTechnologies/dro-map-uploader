using System;
using UnityEngine;

public class SingletonScriptableObject<T> : ScriptableObject where T : ScriptableObject
{
    private static T m_instance;
    
    public static T instance
    {
        get
        {
            if (m_instance == null)
            {
                var instances = Resources.LoadAll<T>(String.Empty);
                if (instances.Length <= 0)
                {
                    throw new Exception("(Singleton) Not find config for resource : " + typeof(T).Name);
                }

                if (instances.Length > 1)
                {
                    throw new Exception("(Singleton) Multiple config for : " + typeof(T).Name);
                }

                m_instance = instances[0];
            }

            return m_instance;
        }
    }
}