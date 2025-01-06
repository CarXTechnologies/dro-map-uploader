using System;
using System.Collections.Generic;

[Serializable]
public struct DayMonth
{
	public Month mount;
	public int day;

	public static readonly Dictionary<Month, int> maxDays = new()
	{
		{ Month.January , 31},
		{ Month.February, 29 },
		{ Month.March, 31 },
		{ Month.April, 30 },
		{ Month.May, 31 },
		{ Month.June, 30 },
		{ Month.July, 31 },
		{ Month.August, 31 },
		{ Month.September, 30 },
		{ Month.October, 31 },
		{ Month.November, 30 },
		{ Month.December, 31 },
	};
}