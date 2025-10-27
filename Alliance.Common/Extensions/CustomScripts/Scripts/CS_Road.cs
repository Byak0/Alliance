using Alliance.Common.Core.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using static Alliance.Common.Utilities.Logger;
using MathF = TaleWorlds.Library.MathF;

namespace Alliance.Common.Extensions.CustomScripts.Scripts
{
	/// <summary>
	/// Generates a continuous road/river mesh along a path (series of connected quads).
	/// Supports width, elevation offset, tiling/stretch options, flow axis, live updates,
	/// and step curves for acceleration/deceleration.
	/// </summary>
	public class CS_Road : ScriptComponentBehavior
	{
		public string PathName = "";
		public float Width = 4f;
		public float ElevationOffset = 0.1f;
		public string StepCurve = "{0:1},{100:1}"; // percent:step pairs

		public Material Material;        // assign a material
		public string CustomColor = "#ffffffff"; // RGBA - default white

		// UV options
		public float RepeatU = 1f;       // tiling/stretch along path
		public float RepeatV = 1f;       // tiling/stretch across width
		public bool InvertU = false;
		public bool InvertV = false;
		public bool RotateUV = false;

		public enum FlowAxis { AlongU, AlongV }
		public FlowAxis FlowDirection = FlowAxis.AlongU;

		// Faces
		public bool FlipFaces = false;

		// Editor
		public SimpleButton GENERATE;
		public SimpleButton README;
		public bool Live = false;

		private float _lastUpdate = 0f;
		private const float UPDATE_INTERVAL = 0.5f;
		private const float DEFAULT_STEP = 1f;

		private struct StepKey
		{
			public float Percent;
			public float Step;
		}

		private List<StepKey> _parsedCurve = new List<StepKey>();

		protected override void OnInit()
		{
			base.OnInit();
			ParseStepCurve();
			Generate();
		}

		protected override void OnEditorInit()
		{
			base.OnEditorInit();
			ParseStepCurve();
			Generate();
		}

		protected override void OnEditorVariableChanged(string variableName)
		{
			if (variableName == nameof(GENERATE)) Generate();
			if (variableName == nameof(README)) ShowReadme();
			if (variableName == nameof(StepCurve)) ParseStepCurve();

			if (variableName == nameof(Live) && Live) Generate();

			if (Live)
			{
				Generate();
			}
		}

		protected override void OnEditorTick(float dt)
		{
			if (!Live) return;

			_lastUpdate += dt;
			if (_lastUpdate > UPDATE_INTERVAL)
			{
				_lastUpdate = 0f;
				Generate();
			}
		}

		private void ShowReadme()
		{
			string msg =
				"================ CS_Road StepCurve ================\n" +
				"Format: {percent:step},{percent:step},...\n" +
				"Example: {0:1},{50:10},{100:5}\n" +
				"- At 0% path length -> step = 1\n" +
				"- At 50% path length -> step = 10\n" +
				"- At 100% path length -> step = 5\n" +
				"Values are interpolated linearly in-between.\n" +
				"Default: {0:1},{100:1} (constant step = 1).\n" +
				"==================================================";
			Log(msg, LogLevel.Information);
		}

		private void ParseStepCurve()
		{
			_parsedCurve.Clear();

			if (string.IsNullOrEmpty(StepCurve))
			{
				_parsedCurve.Add(new StepKey { Percent = 0f, Step = DEFAULT_STEP });
				_parsedCurve.Add(new StepKey { Percent = 100f, Step = DEFAULT_STEP });
				return;
			}

			try
			{
				string[] pairs = StepCurve.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (string pair in pairs)
				{
					string trimmed = pair.Trim('{', '}', ' ');
					string[] parts = trimmed.Split(':');
					if (parts.Length == 2 &&
						float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float percent) &&
						float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float step))
					{
						_parsedCurve.Add(new StepKey { Percent = percent, Step = step });
					}
				}

				// Ensure at least two keys
				if (_parsedCurve.Count < 2)
				{
					_parsedCurve.Clear();
					_parsedCurve.Add(new StepKey { Percent = 0f, Step = DEFAULT_STEP });
					_parsedCurve.Add(new StepKey { Percent = 100f, Step = DEFAULT_STEP });
				}

				_parsedCurve.Sort((a, b) => a.Percent.CompareTo(b.Percent));
			}
			catch (Exception ex)
			{
				Log($"[CS_Road] Failed to parse StepCurve: {ex.Message}", LogLevel.Warning);
				_parsedCurve.Clear();
				_parsedCurve.Add(new StepKey { Percent = 0f, Step = DEFAULT_STEP });
				_parsedCurve.Add(new StepKey { Percent = 100f, Step = DEFAULT_STEP });
			}
		}

		private float EvaluateStep(float progressPercent)
		{
			if (_parsedCurve.Count == 0) return DEFAULT_STEP;

			if (progressPercent <= _parsedCurve[0].Percent) return _parsedCurve[0].Step;
			if (progressPercent >= _parsedCurve[_parsedCurve.Count - 1].Percent) return _parsedCurve[_parsedCurve.Count - 1].Step;

			for (int i = 0; i < _parsedCurve.Count - 1; i++)
			{
				StepKey a = _parsedCurve[i];
				StepKey b = _parsedCurve[i + 1];

				if (progressPercent >= a.Percent && progressPercent <= b.Percent)
				{
					float t = (progressPercent - a.Percent) / (b.Percent - a.Percent);
					return MathF.Lerp(a.Step, b.Step, t);
				}
			}

			return DEFAULT_STEP;
		}

		private void Generate()
		{
			if (string.IsNullOrEmpty(PathName))
			{
				Log("[CS_Road] Path name is empty.", LogLevel.Warning);
				return;
			}

			Path path = Scene.GetPathWithName(PathName);
			if (path == null)
			{
				Log($"[CS_Road] Path '{PathName}' not found.", LogLevel.Warning);
				return;
			}

			if (Material == null || !Material.IsValid)
			{
				Log("[CS_Road] Material is not assigned or invalid.", LogLevel.Warning);
				return;
			}

			CheckCustomColorSyntax();

			// Create mesh
			var mesh = Mesh.CreateMesh(editable: true);
			mesh.CullingMode = MBMeshCullingMode.None;

			UIntPtr handle = mesh.LockEditDataWrite();
			try
			{
				float total = path.TotalDistance;
				Vec3 up = Vec3.Up;

				List<Vec3> vertsLeft = new List<Vec3>();
				List<Vec3> vertsRight = new List<Vec3>();
				List<float> distances = new List<float>();

				float d = 0f;
				while (d < total)
				{
					float progressPercent = (d / total) * 100f;
					float currentStep = EvaluateStep(progressPercent);

					float next = MathF.Min(d + Math.Max(0.01f, currentStep), total);

					Vec3 pos = path.GetFrameForDistance(d).origin + up * ElevationOffset;
					Vec3 nextPos = path.GetFrameForDistance(next).origin + up * ElevationOffset;

					Vec3 forward = (nextPos - pos).NormalizedCopy();
					Vec3 side = Vec3.CrossProduct(up, forward).NormalizedCopy() * (Width * 0.5f);

					vertsLeft.Add(pos - side);
					vertsRight.Add(pos + side);
					distances.Add(d);

					d = next;
				}

				// Ensure last point is included
				if (distances.Count == 0 || distances[distances.Count - 1] < total)
				{
					Vec3 pos = path.GetFrameForDistance(total).origin + up * ElevationOffset;
					Vec3 forward = Vec3.Forward;
					if (vertsLeft.Count > 0)
						forward = (pos - (vertsLeft[vertsLeft.Count - 1] + vertsRight[vertsRight.Count - 1]) * 0.5f).NormalizedCopy();

					Vec3 side = Vec3.CrossProduct(up, forward).NormalizedCopy() * (Width * 0.5f);

					vertsLeft.Add(pos - side);
					vertsRight.Add(pos + side);
					distances.Add(total);
				}

				// Build connected quads
				float accumulatedAlong = 0f;
				for (int i = 0; i < vertsLeft.Count - 1; i++)
				{
					Vec3 p0 = vertsLeft[i];
					Vec3 p1 = vertsRight[i];
					Vec3 p2 = vertsRight[i + 1];
					Vec3 p3 = vertsLeft[i + 1];

					float d0 = accumulatedAlong;
					float uvAdvance = RepeatU; // constant UV advance
					float d1 = d0 + uvAdvance;
					accumulatedAlong = d1;

					float across0 = 0f;
					float across1 = RepeatV;

					Vec2 t0, t1, t2, t3;

					if (FlowDirection == FlowAxis.AlongU)
					{
						t0 = new Vec2(d0, across0);
						t1 = new Vec2(d1, across0);
						t2 = new Vec2(d1, across1);
						t3 = new Vec2(d0, across1);
					}
					else
					{
						t0 = new Vec2(across0, d0);
						t1 = new Vec2(across1, d0);
						t2 = new Vec2(across1, d1);
						t3 = new Vec2(across0, d1);
					}

					if (InvertU) { t0.x = 1 - t0.x; t1.x = 1 - t1.x; t2.x = 1 - t2.x; t3.x = 1 - t3.x; }
					if (InvertV) { t0.y = 1 - t0.y; t1.y = 1 - t1.y; t2.y = 1 - t2.y; t3.y = 1 - t3.y; }
					if (RotateUV)
					{
						t0 = new Vec2(t0.y, t0.x);
						t1 = new Vec2(t1.y, t1.x);
						t2 = new Vec2(t2.y, t2.x);
						t3 = new Vec2(t3.y, t3.x);
					}

					if (FlipFaces)
					{
						mesh.AddTriangle(p0, p2, p1, t0, t2, t1, 0xFFFFFFFF, handle);
						mesh.AddTriangle(p0, p3, p2, t0, t3, t2, 0xFFFFFFFF, handle);
					}
					else
					{
						mesh.AddTriangle(p0, p1, p2, t0, t1, t2, 0xFFFFFFFF, handle);
						mesh.AddTriangle(p0, p2, p3, t0, t2, t3, 0xFFFFFFFF, handle);
					}
				}
			}
			finally
			{
				mesh.UnlockEditDataWrite(handle);
			}

			mesh.RecomputeBoundingBox();
			mesh.SetMaterial(Material);
			mesh.Color = EntityUtils.ColorFromHex(CustomColor);

			MetaMesh roadMM = MetaMesh.CreateMetaMesh("road_mesh");
			roadMM.AddMesh(mesh);

			MetaMesh oldMM = GameEntity.GetMetaMesh(0);
			if (oldMM != null) GameEntity.RemoveComponent(oldMM);

			GameEntity.AddComponent(roadMM);
			Log("[CS_Road] Road generated successfully.", LogLevel.Information);
		}

		private void CheckCustomColorSyntax()
		{
			if (!CustomColor.StartsWith("#") || (CustomColor.Length != 7 && CustomColor.Length != 9))
			{
				Log($"[CS_TextPanel] CustomColor '{CustomColor}' has invalid syntax. Using default white.", LogLevel.Warning);
				CustomColor = "#ffffffff";
			}
		}
	}
}
