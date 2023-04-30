using System;
using PulsarModLoader;

namespace LevelScanner
{
    public class Mod : PulsarMod
    {
		public override string HarmonyIdentifier() => "BadRyuner.LevelScanner";
		public override string Author => "Badryuner";
		public override string Name => "Level Scanner";
		public override string Version => "1.0";

		public Mod()
		{
			PLScanner.Instance.transform.GetChild(0).gameObject.AddComponent<NewScanner>();
		}

		public override void Unload()
		{
			NewScanner.Instance.enabled = false;
		}
	}
}
