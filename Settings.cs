using System;
using PulsarModLoader.CustomGUI;
using static UnityEngine.GUILayout;

namespace LevelScanner
{
	internal class Settings : ModSettingsMenu
	{
		static string _name = "Level Scanner";

		public override string Name() => _name;

		public override void Draw()
		{
			BeginHorizontal();
			Label("HostileHex");
			NewScanner.hostilehex.Value = TextField(NewScanner.hostilehex.Value);
			EndHorizontal();
			BeginHorizontal();
			Label("CrewHex");
			NewScanner.crewhex.Value = TextField(NewScanner.crewhex.Value);
			EndHorizontal();
			BeginHorizontal();
			Label("NPCHex");
			NewScanner.npchex.Value = TextField(NewScanner.npchex.Value);
			EndHorizontal();
			BeginHorizontal();
			Label("DoorHex");
			NewScanner.doorhex.Value = TextField(NewScanner.doorhex.Value);
			EndHorizontal();
			BeginHorizontal();
			Label("TeleporterHex");
			NewScanner.teleporterhex.Value = TextField(NewScanner.teleporterhex.Value);
			EndHorizontal();
			BeginHorizontal();
			Label("ItemHex");
			NewScanner.itemhex.Value = TextField(NewScanner.itemhex.Value);
			EndHorizontal();
			BeginHorizontal();
			Label("ResearchHex");
			NewScanner.researchhex.Value = TextField(NewScanner.researchhex.Value);
			EndHorizontal();
			BeginHorizontal();
			Label("FireHex");
			NewScanner.firehex.Value = TextField(NewScanner.firehex.Value);
			EndHorizontal();
			BeginHorizontal();
			Label("LevelHex");
			NewScanner.interiorhex.Value = TextField(NewScanner.interiorhex.Value);
			EndHorizontal();

			Space(2f);
			NewScanner.ShowInterior.Value = Toggle(NewScanner.ShowInterior.Value, "Show Interior");

			Space(2f);
			if (Button("Apply"))
				UpdateColor();
		}

		public override void OnClose() => UpdateColor();

		public static void UpdateColor()
		{
			NewScanner._hostile = NGUIText.ParseColor24(NewScanner.hostilehex.Value, 0);
			NewScanner._crew = NGUIText.ParseColor24(NewScanner.crewhex.Value, 0);
			NewScanner._npc = NGUIText.ParseColor24(NewScanner.npchex.Value, 0);
			NewScanner._door = NGUIText.ParseColor24(NewScanner.doorhex.Value, 0);
			NewScanner._teleporter = NGUIText.ParseColor24(NewScanner.teleporterhex.Value, 0);
			NewScanner._item = NGUIText.ParseColor24(NewScanner.itemhex.Value, 0);
			NewScanner._research = NGUIText.ParseColor24(NewScanner.researchhex.Value, 0);
			NewScanner._fire = NGUIText.ParseColor24(NewScanner.firehex.Value, 0);
			NewScanner._interior = NGUIText.ParseColor24(NewScanner.interiorhex.Value, 0);
		}
	}
}
