public static class ExtensionMethods
{
	public static int IndexOf(this string[] values, string value)
	{
		for (var index = 0; index < values.Length; index++)
		{
			if (values[index] == value)
			{
				return index;
			}
		}

		return -1;
	}
}
