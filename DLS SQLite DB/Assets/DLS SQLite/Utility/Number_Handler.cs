
using System;
using UnityEngine;


namespace MDF_EDITOR {

	public static class Number_Handler
	{
		public static int MaxValue(this int value, int MaxValue)
		{
			value = Mathf.Clamp(value,0,MaxValue);
			return value;
		}

	}

}
