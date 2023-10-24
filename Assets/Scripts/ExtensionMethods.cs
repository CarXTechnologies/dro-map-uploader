using System.Collections;
using System.Threading;
using System.Threading.Tasks;

public static class ExtensionMethods 
{
    public static IEnumerator AsIEnumerator(this Task task)
    {
        while (!task.IsCompleted)
        {
            Thread.Sleep(5000);
            yield return null;
        }

        if (task.IsFaulted && task.Exception != null)
        {
            throw task.Exception;
        }
    }
}