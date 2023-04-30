using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LevelScanner
{
	internal class MeshTarget : PLScanner.TargetScannerInfo
	{
		public Mesh link;
		public int subhub;

		public override bool IsStillValid()
		{
			return false;
		}
	}
}
