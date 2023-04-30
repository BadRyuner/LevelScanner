using JetBrains.Annotations;
using ProFlares;
using PulsarModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
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
		public static Dictionary<int /* hub id */, Vector3[][]> meshes = new Dictionary<int, Vector3[][]>();

		internal static SaveValue<bool> ShowInterior = new SaveValue<bool>("ShowInterior", false);

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
			mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			mat.SetInt("_ZWrite", 0);
			GameObject.DontDestroyOnLoad(mat);
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

			if (PLNetworkManager.Instance?.ViewedPawn != null)
			{
				PawnY = PLNetworkManager.Instance.ViewedPawn.transform.position.y;
				if (ShowInterior.Value && !meshes.ContainsKey(PLNetworkManager.Instance.LocalPlayer.SubHubID))
				{
					MeshCollider[] targets;
					var shipinfo = PLNetworkManager.Instance.LocalPlayer.MyCurrentTLI.MyShipInfo;
					var astar = PLNetworkManager.Instance.LocalPlayer.MyCurrentTLI.OptionalPathNetwork;
					if (shipinfo != null)
					{
						targets = shipinfo.InteriorStatic.GetComponentsInChildren<MeshCollider>();
					}
					else if (astar != null)
					{
						targets = astar.GetComponentsInChildren<MeshCollider>();
					}
					else
					{
						PulsarModLoader.Utilities.Messaging.Notification($"Bad level - {PLNetworkManager.Instance.LocalPlayer.SubHubID}");
						meshes.Add(PLNetworkManager.Instance.LocalPlayer.SubHubID, new Vector3[0][]);
						return;
					}

					Vector3[][] vectors = new Vector3[targets.Length][];
					int count = 0;
					foreach (var v in targets)
					{
						var verts = v.sharedMesh.vertices;
						Vector3[] vecs = new Vector3[verts.Length];

						for(int i = 0; i < verts.Length; i++)
							vecs[i] = v.transform.TransformPoint(verts[i]);

						vectors[count] = vecs;
						count++;
					}

					meshes.Add(PLNetworkManager.Instance.LocalPlayer.SubHubID, vectors);
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

				GL.Color(new Color32(135, 135, 135, 80));

				if (meshes.TryGetValue(PLNetworkManager.Instance.LocalPlayer.SubHubID, out var floor))
				{
					for (int i = 0; i < floor.Length; i++)
					{
						var obj = floor[i];
						if (obj.Length == 0) continue;
						var last = obj[0];
						for (int x = 1; x < obj.Length; x++)
						{
							var in3d = obj[x];
							if (Mathf.Abs(in3d.y - PawnY) > 1.3f) continue;
							GL.Vertex(new Vector3(last.x, last.z, 0) * zoom);
							var now = scannerTransform.InverseTransformPoint(in3d);
							GL.Vertex(new Vector3(now.x, now.z, 0) * zoom);
							last = now;
						}
					}
				}

				GL.End();
			}
			GL.Begin(GL.TRIANGLES);

			var clr = Color.white;

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

			clr = new Color32(0x66, 0x66, 0xff, 255);
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

			clr = new Color32(0xff, 0x00, 0xff, 255);
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
				clr = new Color32(0xff, 0xff, 0, 255);
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
				clr = new Color32(0x44, 0xff, 0xff, 255);
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

			clr = Color.yellow;

			var fires = PLNetworkManager.Instance.ViewedPawn.CurrentShip?.AllFires;
			if (fires != null)
			foreach(var fire in fires.Values)
			{
				if (fire == null) continue;

				var vec = scannerTransform.InverseTransformPoint(fire.transform.position);
				var y = vec.y;
				clr.a = fire.Intensity;
				GL.Color(clr);
				GL.Vertex(new Vector3(vec.x, vec.z + 3) * zoom);
				GL.Vertex(new Vector3(vec.x + 3, vec.z) * zoom);
				GL.Vertex(new Vector3(vec.x - 3, vec.z) * zoom);
			}

			clr = Color.red;

			for (int i = 0; i < PLGameStatic.Instance.AllPawnBases.Count; i++)
			{
				var pawn = PLGameStatic.Instance.AllPawnBases[i];
				if (PLScanner.TargetScannerInfo_PawnBase.CheckIfValid(pawn))
				{
					var vec = scannerTransform.InverseTransformPoint(pawn.transform.position);
					var y = vec.y;
					if (pawn.GetIsFriendly())
					{
						var green = Color.green;
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
