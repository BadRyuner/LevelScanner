using JetBrains.Annotations;
using ProFlares;
using PulsarModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static PLScanner;

namespace LevelScanner
{
	internal class NewScanner : MonoBehaviour
	{
		internal static NewScanner Instance;

		public static PLPawnItem_Scanner ActiveScanner;
		public static Dictionary<PLTeleportationLocationInstance /* hub id */, Vector3[][]> meshes = new Dictionary<PLTeleportationLocationInstance, Vector3[][]>();

		public static SaveValue<bool> ShowInterior = new SaveValue<bool>("ShowInterior", false);

		public static SaveValue<string> interiorhex = new SaveValue<string>("InteriorHex", NGUIText.EncodeColor24(new Color32(135, 135, 135, 80)));
		public static SaveValue<string> hostilehex = new SaveValue<string>("HostileHex", "ff0000");
		public static SaveValue<string> crewhex = new SaveValue<string>("CrewHex", "00ff00");
		public static SaveValue<string> npchex = new SaveValue<string>("NPCHex", "ffffff");
		public static SaveValue<string> doorhex = new SaveValue<string>("DoorHex", "6666ff");
		public static SaveValue<string> teleporterhex = new SaveValue<string>("TeleporterHex", "ff00ff");
		public static SaveValue<string> itemhex = new SaveValue<string>("ItemHex", "FFFF00");
		public static SaveValue<string> researchhex = new SaveValue<string>("ResearchHex", "44FFFF");
		public static SaveValue<string> firehex = new SaveValue<string>("FireHex", "E26822");

		public static Color _interior;
		public static Color _hostile;
		public static Color _crew;
		public static Color _npc;
		public static Color _door;
		public static Color _teleporter;
		public static Color _item;
		public static Color _research;
		public static Color _fire;

		Material mat;
		Camera cam;

		static float PawnY = 0f;

		void Start()
		{
			Instance = this;
			cam = this.GetComponent<Camera>();
			mat = new Material(Shader.Find("Hidden/Internal-Colored"));
			mat.hideFlags = HideFlags.HideAndDontSave;
			//mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			//mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			//mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			mat.SetInt("_ZWrite", 0);
			GameObject.DontDestroyOnLoad(mat);
			Settings.UpdateColor();
		}

		void FixedUpdate()
		{
			if (PLNetworkManager.Instance.ViewedPawn?.MyPlayer?.MyInventory != null)
			{
				ActiveScanner = null;
				foreach(var item in PLNetworkManager.Instance.ViewedPawn.MyPlayer.MyInventory.AllItems)
				{
					if (item.EquipID != -1 && item.PawnItemType == EPawnItemType.E_SCANNER)
					{
						ActiveScanner = item as PLPawnItem_Scanner;
						break;
					}
				}
			}

			if (PLNetworkManager.Instance.LocalPlayer == null && meshes.Count != 0)
				meshes.Clear();

			if (PLNetworkManager.Instance?.ViewedPawn != null)
			{
				PawnY = PLNetworkManager.Instance.ViewedPawn.transform.position.y;
				if (ShowInterior.Value && !meshes.ContainsKey(PLNetworkManager.Instance.LocalPlayer.MyCurrentTLI))
				{
					MeshCollider[] targets;
					var shipinfo = PLNetworkManager.Instance.LocalPlayer.MyCurrentTLI.MyShipInfo;
					var astar = PLNetworkManager.Instance.LocalPlayer.MyCurrentTLI.OptionalPathNetwork;
					var bso = PLNetworkManager.Instance.LocalPlayer.MyCurrentTLI.MyBSO;
					if (shipinfo != null)
					{
						targets = shipinfo.InteriorStatic.GetComponentsInChildren<MeshCollider>();
					}
					else if (astar != null)
					{
						targets = astar.GetComponentsInChildren<MeshCollider>();
					}
					else if (bso != null)
					{
						targets = bso.GetComponentsInChildren<MeshCollider>();
					}
					else
					{
						PulsarModLoader.Utilities.Messaging.Notification($"Bad level - {PLNetworkManager.Instance.LocalPlayer.SubHubID}");
						meshes.Add(PLNetworkManager.Instance.LocalPlayer.MyCurrentTLI, new Vector3[0][]);
						return;
					}

					Vector3[][] vectors = new Vector3[targets.Length][];
					int count = 0;
					foreach (var v in targets)
					{
						var verts = v.sharedMesh?.vertices;
						if (verts == null)
						{
							//PulsarModLoader.Utilities.Logger.Info("NULL");
							vectors[count] = Array.Empty<Vector3>();
							count++;
							continue;
						}

						Vector3[] vecs = new Vector3[verts.Length];

						for(int i = 0; i < verts.Length; i++)
						{
							vecs[i] = v.transform.TransformPoint(verts[i]);
						}

						vectors[count] = vecs;
						count++;
					}

					meshes.Add(PLNetworkManager.Instance.LocalPlayer.MyCurrentTLI, vectors);
				}
			}
		}

		void OnPostRender() // maybe add cache?
		{
			if (ActiveScanner == null) return;
			
			mat.SetPass(0);

			var scannerTransform = PLNetworkManager.Instance.ViewedPawn.HorizontalMouseLook.transform;
			var zoom = ActiveScanner.ZoomLevel + 3;

			GL.PushMatrix();
			GL.MultMatrix(transform.localToWorldMatrix);
			if (ShowInterior.Value)
			{
				GL.Begin(GL.LINES);

				GL.Color(_interior);

				if (meshes.TryGetValue(PLNetworkManager.Instance.LocalPlayer.MyCurrentTLI, out var floor))
				{
					for (int i = 0; i < floor.Length; i++)
					{
						var obj = floor[i];
						if (obj.Length == 0) continue;
						var last = scannerTransform.InverseTransformPoint(obj[0]); // haha, phahtom points go brrrrr
						if (new Vector2(last.x, last.z).sqrMagnitude > 9000f) continue; // is this works???
						for (int x = 1; x < obj.Length; x++)
						{
							var in3d = obj[x];
							if (Mathf.Abs(in3d.y - PawnY) > 3f) // old - 1.6f
							{
								last.y = 0;
								continue;
							}
							var now = scannerTransform.InverseTransformPoint(in3d);
							if (last.y == 0)
							{
								last = now;
								continue;
							}
							GL.Vertex(new Vector3(last.x, last.z, 0) * zoom);
							GL.Vertex(new Vector3(now.x, now.z, 0) * zoom);
							last = now;
						}
					}
				}

				GL.End();
			}
			GL.Begin(GL.TRIANGLES);

			var clr = _npc;

			for(int i = 0; i < PLDialogueActorInstance.AllDialogueActorInstances.Count; i++)
			{
				var actor = PLDialogueActorInstance.AllDialogueActorInstances[i];
				if (PLScanner.TargetScannerInfo_DialogueActor.CheckIfValid(actor))
				{
					var vec = scannerTransform.InverseTransformPoint(actor.transform.position);
					var y = vec.y;
					vec.y = 0;
					if (vec.sqrMagnitude > 40000f) continue;

					if (actor.HasMissionStartAvailable || actor.HasMissionEndAvailable)
					{
						var yelclr = Color.yellow;
						yelclr.a = GetAlpha(y);
						GL.Color(yelclr);
						GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
						GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
						GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
					}
					else
					{
						clr.a = GetAlpha(y);
						GL.Color(clr);
						GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
						GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
						GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
					}
				}
			}

			clr = _hostile;

			for (int i = 0; i < PLPlanetMine.AllPlanetMines.Count; i++)
			{
				var mine = PLPlanetMine.AllPlanetMines[i];
				if (PLScanner.TargetScannerInfo_PlanetMine.CheckIfValid(mine))
				{
					var vec = scannerTransform.InverseTransformPoint(mine.transform.position);
					var y = vec.y;
					clr.a = GetAlpha(y);
					GL.Color(clr);
					GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
					GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
					GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
				}
			}

			clr = _door;
			GL.Color(clr);

			for (int i = 0; i < PLInteriorDoor.AllInteriorDoors.Count; i++)
			{
				var door = PLInteriorDoor.AllInteriorDoors[i];
				if (PLScanner.TargetScannerInfo_Door.CheckIfValid(door))
				{
					var vec = scannerTransform.InverseTransformPoint(door.transform.position);
					var y = vec.y;
					clr.a = GetAlpha(y);
					GL.Color(clr);
					GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
					GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
					GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
				}
			}

			clr = _teleporter;
			GL.Color(clr);

			foreach (PLScreenHubBase plscreenHubBase in PLScreenHubBase.GetAllScreenHubs())
				foreach (PLUIScreen pluiscreen in plscreenHubBase.AllScreens)
				{
					if (pluiscreen is PLTeleportationScreen tele)
						if (PLScanner.TargetScannerInfo_Teleporter.CheckIfValid(tele))
						{
							var vec = scannerTransform.InverseTransformPoint(tele.transform.position);
							var y = vec.y;
							clr.a = GetAlpha(y);
							GL.Color(clr);
							GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
							GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
							GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
						}
				}

			var pickups = PLGameStatic.Instance.m_AllPickupObjects;
			if (PLNetworkManager.Instance.ViewedPawn.MyPlayer.Talents[34] > 0)
			{
				clr = _item;
				GL.Color(clr);
				
				for (int i = 0; i < pickups.Count; i++)
				{
					var p = pickups[i];
					if (PLScanner.TargetScannerInfo_PickupObject.CheckIfValid(p))
					{
						var vec = scannerTransform.InverseTransformPoint(p.transform.position);
						var y = vec.y;
						clr.a = GetAlpha(y);
						GL.Color(clr);
						GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
						GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
						GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
					}
				}

				var pComps = PLGameStatic.Instance.m_AllPickupComponents;

				for (int i = 0; i < pComps.Count; i++)
				{
					var p = pComps[i];
					if (PLScanner.TargetScannerInfo_PickupComp.CheckIfValid(p))
					{
						var vec = scannerTransform.InverseTransformPoint(p.transform.position);
						var y = vec.y;
						clr.a = GetAlpha(y);
						GL.Color(clr);
						GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
						GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
						GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
					}
				}

				var randpComps = PLGameStatic.Instance.m_AllPickupRandomComponents;

				for (int i = 0; i < randpComps.Count; i++)
				{
					var p = randpComps[i];
					if (PLScanner.TargetScannerInfo_PickupRandComp.CheckIfValid(p))
					{
						var vec = scannerTransform.InverseTransformPoint(p.transform.position);
						var y = vec.y;
						clr.a = GetAlpha(y);
						GL.Color(clr);
						GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
						GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
						GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
					}
				}
			}

			if (PLNetworkManager.Instance.ViewedPawn.MyPlayer.Talents[33] > 0)
			{
				clr = _research;
				GL.Color(clr);

				for (int i = 0; i < pickups.Count; i++)
				{
					var p = pickups[i];
					if (PLScanner.TargetScannerInfo_PickupResearchMat.CheckIfValid(p))
					{
						var vec = scannerTransform.InverseTransformPoint(p.transform.position);
						var y = vec.y;
						clr.a = GetAlpha(y);
						GL.Color(clr);
						GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
						GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
						GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
					}
				}
			}

			clr = _fire;

			var fires = PLNetworkManager.Instance.ViewedPawn.CurrentShip?.AllFires;
			if (fires != null)
			foreach(var fire in fires.Values)
			{
				if (fire == null) continue;

				var vec = scannerTransform.InverseTransformPoint(fire.transform.position);
				clr.a = GetAlpha(vec.y);
				GL.Color(clr);
				GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
				GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
				GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
			}

			clr = _hostile;

			for (int i = 0; i < PLGameStatic.Instance.AllPawnBases.Count; i++)
			{
				var pawn = PLGameStatic.Instance.AllPawnBases[i];
				if (PLScanner.TargetScannerInfo_PawnBase.CheckIfValid(pawn))
				{
					var vec = scannerTransform.InverseTransformPoint(pawn.transform.position);
					var y = vec.y;
					if (pawn.GetIsFriendly())
					{
						var green = _crew;
						//green.a = GetAlpha(y);
						GL.Color(green);
						GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
						GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
						GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
					}
					else
					{
						clr.a = GetAlpha(y);
						GL.Color(clr);
						GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
						GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
						GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
					}
				}
			}

			GL.End();
			GL.PopMatrix();
		}

		static float GetAlpha(float y)
		{
			float num3 = Mathf.Abs(PawnY - y);
			if (num3 > 6f)
				return 0.33f;
			else if (num3 > 3f)
				return 0.66f;
			else
				return 1f;
		}
	}

	[HarmonyLib.HarmonyPatch(typeof(PLServer), nameof(PLServer.SafeNetworkBeginWarp))]
	static class OnWarp
	{
		static void Postfix()
		{
			NewScanner.meshes.Clear();
		}
	}
}
