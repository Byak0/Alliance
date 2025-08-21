using Alliance.Common.Extensions.CustomScripts.Scripts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using static Alliance.Common.Utilities.Logger;

namespace Alliance.Common.Core.Utils
{
	/// <summary>
	/// Helpers for manipulating game entities, generating meshes, etc.
	/// Uses reflection to access internal engine methods.
	/// </summary>
	public static class EntityUtils
	{
		// Default glyph map for manipulating ASCII characters (0-127).
		public static readonly FixedGridGlyphMap DefaultAsciiGrid = new FixedGridGlyphMap(16, 16, 0, 127, 1f, true);

		private const int MAX_TEXT_PANEL_BUILT_PER_TICK = 32;

		private static readonly ConcurrentQueue<CS_TextPanel> _textPanelQueue = new();
		private static readonly ConcurrentDictionary<CS_TextPanel, byte> _dedupe = new();

		private static bool _initOk;
		private static object _iGameEntityInstance;
		private static MethodInfo _miCreateEmpty;
		private static PropertyInfo _piScenePtr;
		private static PropertyInfo _piEntityPtr;

		public static void Tick(float dt)
		{
			ProcessTextPanelQueue();
		}

		public static void EnqueueTextPanel(CS_TextPanel panel)
		{
			if (panel == null || panel.GameEntity == null) return;
			if (_dedupe.TryAdd(panel, 0)) _textPanelQueue.Enqueue(panel);
		}

		private static void ProcessTextPanelQueue()
		{
			int built = 0;
			while (built < MAX_TEXT_PANEL_BUILT_PER_TICK && _textPanelQueue.TryDequeue(out var panel))
			{
				_dedupe.TryRemove(panel, out _);

				try
				{
					if (panel == null || panel.GameEntity == null)
						continue;

					// Material resolution (by name) with fallback
					string materialName = panel.ResolveMaterialName();

					// Build mesh (triangles = white; no vertex color)
					var mesh = CreateTextMesh(
						text: panel.Text ?? string.Empty,
						fontSizeMeters: Math.Max(0.01f, panel.FontSize),
						panelMaxWidthMeters: panel.PanelMaxWidth,
						alignment: panel.TextAlignment,
						glyphMap: DefaultAsciiGrid,
						materialName: materialName,
						customColor: panel.GameEntity.GetFactorColor(),
						letterSpacingEm: panel.LetterSpacing,
						lineSpacingMult: panel.LineSpacing
					);

					MetaMesh oldMM = panel.GameEntity.GetMetaMesh(0);

					MetaMesh newMM = MetaMesh.CreateMetaMesh("text_panel_mesh");
					newMM.AddMesh(mesh);
					panel.GameEntity.AddComponent(newMM);

					if (oldMM != null) panel.GameEntity.RemoveComponent(oldMM);
				}
				catch (Exception e)
				{
					Log($"[CS_TextPanelQueue] build error: {e}", LogLevel.Error);
				}

				built++;
			}
		}

		/// <summary>
		/// Try to get access to the Engine's GameEntity creation method (through internal EngineApplicationInterface)
		/// </summary>
		private static bool InitReflection()
		{
			if (_initOk) return true;

			try
			{
				Assembly engineAsm = typeof(GameEntity).Assembly;

				Type eaiType = engineAsm.GetType("TaleWorlds.Engine.EngineApplicationInterface", false)
							   ?? engineAsm.GetTypes().FirstOrDefault(t => t.Name == "EngineApplicationInterface")
							   ?? throw new InvalidOperationException("EngineApplicationInterface type not found.");

				FieldInfo fiIGameEntity = eaiType.GetField("IGameEntity", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
									   ?? throw new MissingFieldException("IGameEntity field not found.");

				_iGameEntityInstance = fiIGameEntity.GetValue(null)
									   ?? throw new NullReferenceException("IGameEntity instance is null.");

				Type implType = _iGameEntityInstance.GetType();

				_miCreateEmpty = implType
					.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.FirstOrDefault(m =>
					{
						if (!m.Name.Contains("CreateEmpty")) return false;
						var ps = m.GetParameters();
						return ps.Length == 3
							   && ps[0].ParameterType == typeof(UIntPtr)
							   && ps[1].ParameterType == typeof(bool)
							   && ps[2].ParameterType == typeof(UIntPtr);
					})
					?? throw new MissingMethodException("CreateEmpty method not found.");

				_piScenePtr = typeof(Scene).GetProperty("Pointer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
							  ?? throw new MissingMemberException("Scene.Pointer property not found.");
				_piEntityPtr = typeof(GameEntity).GetProperty("Pointer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
							   ?? throw new MissingMemberException("GameEntity.Pointer property not found.");

				_initOk = true;
				return true;
			}
			catch (Exception ex)
			{
				_initOk = false;
				Log($"[EditorUtils] Reflection init failed: {ex.Message}", LogLevel.Error);
				return false;
			}
		}

		/// <summary>
		/// Try to create an editable copy of a Game Entity.
		/// </summary>
		public static bool TryCreateEditableCopy(Scene scene, GameEntity source, out GameEntity copy)
		{
			copy = null;
			if (!InitReflection())
			{
				Log($"[EditorUtils] Failed to access Engine copy method.", LogLevel.Error);
				return false;
			}

			try
			{
				Dictionary<string, Vec3?> vec2args = new();
				foreach (GameEntity child in source.GetChildren())
				{
					if (!vec2args.ContainsKey(child.Name)) vec2args.Add(child.Name, child.GetMetaMesh(0)?.GetVectorArgument2());
				}

				UIntPtr scenePtr = (UIntPtr)_piScenePtr.GetValue(scene);
				UIntPtr srcPtr = (UIntPtr)_piEntityPtr.GetValue(source);

				copy = _miCreateEmpty.Invoke(_iGameEntityInstance, new object[] { scenePtr, true, srcPtr }) as GameEntity;
				copy.ValidateBoundingBox();

				foreach (GameEntity child in copy.GetChildren())
				{
					if (vec2args.TryGetValue(child.Name, out Vec3? vec2) && vec2.HasValue)
					{
						child.GetMetaMesh(0)?.SetVectorArgument2(vec2.Value.X, vec2.Value.Y, vec2.Value.Z, vec2.Value.w);
					}
				}

				return copy != null;
			}
			catch (Exception ex)
			{
				Log($"[EditorUtils] Editable copy failed: {ex.Message}.", LogLevel.Error);
				return false;
			}
		}

		/// <summary>
		/// Return entity local size (max - min); falls back to (1,1,1).
		/// </summary>
		public static Vec3 GetLocalSizeOrFallback(GameEntity entity)
		{
			try
			{
				Vec3 min = entity.GetBoundingBoxMin();
				Vec3 max = entity.GetBoundingBoxMax();
				Vec3 s = max - min;
				return new Vec3(
					Math.Max(s.X, 1e-4f),
					Math.Max(s.Y, 1e-4f),
					Math.Max(s.Z, 1e-4f)
				);
			}
			catch (Exception ex)
			{
				Log($"[EditorUtils] Failed to get local size of entity: {ex.Message}. Using default size.", LogLevel.Error);
				return new Vec3(1f, 1f, 1f);
			}
		}

		/// <summary>
		/// Create a text mesh using a glyph map.
		/// The material must have an atlas texture with glyphs.
		///	</summary>
		public static Mesh CreateTextMesh(
			string text,
			float fontSizeMeters,
			float panelMaxWidthMeters,
			TextHorizontalAlignment alignment,
			FixedGridGlyphMap glyphMap,
			string materialName,
			uint customColor = 0xFFFFFFFF,
			float letterSpacingEm = 0f,
			float lineSpacingMult = 1f
		)
		{
			if (string.IsNullOrEmpty(text)) text = "";
			glyphMap ??= DefaultAsciiGrid;

			float glyphH = Math.Max(0.001f, fontSizeMeters);
			float glyphW = glyphH * 0.6f;
			float spacing = letterSpacingEm * glyphW;
			float lineH = glyphH * Math.Max(0.5f, lineSpacingMult);

			var lines = BreakIntoLines(text, panelMaxWidthMeters, glyphMap, glyphW, spacing);

			var mesh = Mesh.CreateMesh(editable: true);
			mesh.CullingMode = MBMeshCullingMode.None;

			UIntPtr handle = UIntPtr.Zero;
			bool locked = false;

			try
			{
				mesh.AddEditDataUser();
				mesh.SetEditDataPolicy(EditDataPolicy.Keep_until_first_render);
				handle = mesh.LockEditDataWrite();
				locked = true;

				const uint White = 0xFFFFFFFF;

				for (int li = 0; li < lines.Count; li++)
				{
					var L = lines[li];
					float alignOffsetX = alignment switch
					{
						TextHorizontalAlignment.Left => 0f,
						TextHorizontalAlignment.Center => -L.width * 0.5f,
						TextHorizontalAlignment.Right => -L.width,
						_ => 0f
					};

					float penX = 0f;
					float z0Line = -li * lineH; // grow downward

					for (int i = L.start; i < L.end; i++)
					{
						char ch = L.text[i];
						if (!glyphMap.TryGetUV(ch, out var uvMin, out var uvMax))
							glyphMap.TryGetUV('?', out uvMin, out uvMax);

						float adv = glyphMap.AdvanceFor(ch, glyphW);

						float x0 = alignOffsetX + penX;
						float x1 = x0 + (ch == ' ' ? adv : glyphW);
						float z0 = z0Line;
						float z1 = z0 + glyphH;

						var p00 = new Vec3(x0, 0f, z0);
						var p10 = new Vec3(x1, 0f, z0);
						var p11 = new Vec3(x1, 0f, z1);
						var p01 = new Vec3(x0, 0f, z1);

						var t00 = new Vec2(uvMin.x, uvMin.y);
						var t10 = new Vec2(uvMax.x, uvMin.y);
						var t11 = new Vec2(uvMax.x, uvMax.y);
						var t01 = new Vec2(uvMin.x, uvMax.y);

						mesh.AddTriangle(p00, p10, p11, t00, t10, t11, White, handle);
						mesh.AddTriangle(p00, p11, p01, t00, t11, t01, White, handle);

						bool hasNext = (i + 1) < L.end && L.text[i + 1] != '\n';
						penX += adv + (hasNext ? spacing : 0f);
					}
				}
			}
			finally
			{
				if (locked) mesh.UnlockEditDataWrite(handle);
			}

			mesh.RecomputeBoundingBox();

			if (!string.IsNullOrEmpty(materialName))
			{
				mesh.SetMaterial(materialName);
				mesh.Color = customColor;
			}

			mesh.ReleaseEditDataUser();
			return mesh;
		}

		/// <summary>
		/// Break a raw string into lines based on max width and glyph map.
		/// </summary>
		private static List<Line> BreakIntoLines(string raw, float maxWidth, FixedGridGlyphMap glyphMap, float glyphW, float spacing)
		{
			List<Line> lines = new List<Line>();
			if (raw.Length == 0) { lines.Add(new Line(raw, 0, 0, 0f)); return lines; }

			int start = 0;
			float penX = 0f;
			int countInLine = 0;

			for (int i = 0; i < raw.Length; i++)
			{
				char ch = raw[i];

				if (ch == '\n')
				{
					float width = penX - (countInLine > 0 ? spacing : 0f);
					lines.Add(new Line(raw, start, i, Math.Max(0f, width)));
					start = i + 1; penX = 0f; countInLine = 0;
					continue;
				}

				float adv = glyphMap.AdvanceFor(ch, glyphW);
				float prospective = penX + adv + (countInLine > 0 ? spacing : 0f);

				bool wrap = maxWidth > 0f && countInLine > 0 && prospective > maxWidth;
				if (wrap)
				{
					float width = penX - (countInLine > 0 ? spacing : 0f);
					lines.Add(new Line(raw, start, i, Math.Max(0f, width)));
					start = i; penX = 0f; countInLine = 0;
				}

				penX += adv + (countInLine > 0 ? spacing : 0f);
				countInLine++;
			}

			if (start <= raw.Length)
			{
				float width = penX - (countInLine > 0 ? spacing : 0f);
				lines.Add(new Line(raw, start, raw.Length, Math.Max(0f, width)));
			}

			return lines;
		}

		/// <summary>
		/// A glyph map gives UV coordinates for characters in a fixed grid.
		/// </summary>
		public class FixedGridGlyphMap
		{
			public readonly int Columns, Rows, StartChar, EndChar;
			public readonly float SpaceWidthFactor;
			public readonly bool FlipV;

			public FixedGridGlyphMap(int columns = 16, int rows = 16, int startChar = 0, int endChar = 127, float spaceWidthFactor = 1f, bool flipV = true)
			{
				Columns = columns;
				Rows = rows;
				StartChar = startChar;
				EndChar = endChar;
				SpaceWidthFactor = spaceWidthFactor;
				FlipV = flipV;
			}

			public bool TryGetUV(char ch, out Vec2 uvMin, out Vec2 uvMax)
			{
				int code = ch;
				if (code < StartChar || code >= EndChar) code = '?';
				if (code < StartChar || code >= EndChar) { uvMin = new Vec2(0, 0); uvMax = new Vec2(0, 0); return false; }

				int idx = code - StartChar; int col = idx % Columns; int row = idx / Columns;
				float u0 = (float)col / Columns, v0 = (float)row / Rows;
				float u1 = (float)(col + 1) / Columns, v1 = (float)(row + 1) / Rows;
				if (FlipV) { float nv0 = 1f - v1, nv1 = 1f - v0; v0 = nv0; v1 = nv1; }
				uvMin = new Vec2(u0, v0); uvMax = new Vec2(u1, v1);
				return true;
			}

			public float AdvanceFor(char ch, float glyphWidth)
			{
				return ch == ' ' ? glyphWidth * SpaceWidthFactor : glyphWidth;
			}
		}

		// Represents a single line of text with its start and end position in the original string
		private readonly struct Line
		{
			public readonly string text;
			public readonly int start, end;
			public readonly float width;
			public Line(string t, int s, int e, float w) { text = t; start = s; end = e; width = w; }
		}

		public enum TextHorizontalAlignment
		{
			Left,
			Right,
			Center,
			Justify
		}

		public enum AvailableFonts
		{
			Galahad,
			OldLondon,
			AntiqueOlive
		}
	}
}