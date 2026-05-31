using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Extended
{
	public static class Data
	{
		public static string Save()
		{
			return "";
		}

		public static void Load(string data)
		{

		}

		public static void ClearAll()
		{
            GlobalVar.playerVars.Clear();

        }

	}
}