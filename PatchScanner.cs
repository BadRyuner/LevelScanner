using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using static PLScanner;

namespace LevelScanner
{
	[HarmonyPatch(typeof(PLScanner), "Update")]
	internal static class InjectObject
	{
		internal static bool Prefix(PLScanner __instance)
		{
			//if (NewScanner.ActiveScanner == null)
			//	return false;

			__instance.ActiveScanner = NewScanner.ActiveScanner;

			if (NewScanner.ActiveScanner != null)
			{
				__instance.ZoomText.text = "ZOOM LEVEL: " + (__instance.ActiveScanner.ZoomLevel + 1).ToString() + " / " + __instance.ActiveScanner.MaxZoomLevel.ToString();
			}
			else
			{
				__instance.ZoomText.text = "";
			}
			string text = "[ff0000](*) HOSTILE[-]\n[00ff00](*) CREW[-]\n[ffffff](*) NPC[-]\n[6666ff](*) DOOR[-]\n[ff00ff](*) TELEPORTER[-]";
			if (PLNetworkManager.Instance.ViewedPawn != null && PLNetworkManager.Instance.ViewedPawn.MyPlayer != null)
			{
				if (PLNetworkManager.Instance.ViewedPawn.MyPlayer.Talents[34] > 0)
				{
					text += "\n[FFFF00](*) PICKUPS[-]";
				}
				if (PLNetworkManager.Instance.ViewedPawn.MyPlayer.Talents[33] > 0)
				{
					text += "\n[44FFFF](*) RESEARCH MATS[-]";
				}
			}
			if (PLNetworkManager.Instance.ViewedPawn != null && PLNetworkManager.Instance.ViewedPawn.GetPlayer() != null && PLNetworkManager.Instance.ViewedPawn.GetPlayer().MyInventory != null && PLNetworkManager.Instance.ViewedPawn.GetPlayer().MyInventory.ActiveItem != null && PLNetworkManager.Instance.ViewedPawn.GetPlayer().MyInventory.ActiveItem.PawnItemType == EPawnItemType.E_SCANNER)
			{
				__instance.TargetInfoLabel.enabled = true;
			}
			else
			{
				__instance.TargetInfoLabel.enabled = false;
			}
			PLGlobal.SafeLabelSetText(__instance.TargetInfoLabel, text);
			if (PLNetworkManager.Instance.ViewedPawn != null && NewScanner.ActiveScanner != null)
			{
				var num = NewScanner.ActiveScanner.ZoomLevel;
				float num2 = Time.time * 0.5f % 1f;
				if (PLNetworkManager.Instance.ViewedPawn != null && PLNetworkManager.Instance.ViewedPawn.MyPlayer != null && PLNetworkManager.Instance.ViewedPawn.MyPlayer.MyInventory != null && PLNetworkManager.Instance.ViewedPawn.MyPlayer.MyInventory.ActiveItem != null && PLNetworkManager.Instance.ViewedPawn.MyPlayer.MyInventory.ActiveItem.PawnItemType == EPawnItemType.E_SCANNER && Time.time - __instance.LastBlipTime > 1f && num2 < Time.deltaTime * 3f)
				{
					__instance.LastBlipTime = Time.time;
					PLMusic.PostEvent("play_sx_player_item_scanner_blip", PLNetworkManager.Instance.ViewedPawn.gameObject);
				}
				__instance.PingCircle.transform.localScale = Vector3.one * num2 * (float)(num + 1);
				__instance.PingCircle.alpha = 0.5f * (0.5f - Mathf.Abs(0.5f - num2));
			}
			return false;
		}
	}
}
