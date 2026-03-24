using Alliance.Common.Extensions.CustomScripts.Scripts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
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
		public static readonly FixedGridGlyphMap DefaultAsciiGrid = new FixedGridGlyphMap(16, 16, 0, 256, 1f, true);

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
						text: panel.CleanedText ?? string.Empty,
						fontSizeMeters: Math.Max(0.01f, panel.FontSize),
						panelMaxWidthMeters: panel.PanelMaxWidth,
						alignment: panel.TextAlignment,
						glyphMap: DefaultAsciiGrid,
						materialName: materialName,
						customColor: ColorFromHex(panel.CustomColor),
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
						return ps.Length == 5
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

				copy = _miCreateEmpty.Invoke(_iGameEntityInstance, new object[] { scenePtr, true, srcPtr, true, true }) as GameEntity;
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
		/// Set the scale of a MatrixFrame directly (instead of multiplying existing scale like native method).
		/// </summary>
		public static MatrixFrame SetScale(MatrixFrame m, Vec3 newScale, bool orthonormalize = true)
		{
			Mat3 rot = m.rotation;

			if (orthonormalize) rot.Orthonormalize();

			Vec3 s = rot.s; s.Normalize();
			Vec3 f = rot.f; f.Normalize();
			Vec3 u = rot.u; u.Normalize();

			// Apply new scale per-axis
			rot.s = s * newScale.x;
			rot.f = f * newScale.y;
			rot.u = u * newScale.z;

			return new MatrixFrame(rot, m.origin);
		}

		/// <summary>
		/// Return the source matrix frame turned forward the target frame, proportionaly to influences (0 to 1f).
		/// </summary>
		public static MatrixFrame BlendEulerRotation(MatrixFrame source, MatrixFrame target, float pitchInfluence, float rollInfluence, float yawInfluence)
		{
			// Convert to quaternions
			Quaternion qSource = source.rotation.ToQuaternion();
			Quaternion qTarget = target.rotation.ToQuaternion();

			// Convert to Euler angles
			Vec3 eulerSource = source.rotation.GetEulerAngles();
			Vec3 eulerTarget = target.rotation.GetEulerAngles();

			// Blend each angle separately
			float pitch = TaleWorlds.Library.MathF.Lerp(eulerSource.x, eulerTarget.x, pitchInfluence); // X axis
			float roll = TaleWorlds.Library.MathF.Lerp(eulerSource.y, eulerTarget.y, rollInfluence);   // Y axis
			float yaw = TaleWorlds.Library.MathF.Lerp(eulerSource.z, eulerTarget.z, yawInfluence);     // Z axis

			// Reconstruct rotation from blended Euler
			Mat3 blendedRot = Mat3.Identity;
			blendedRot.ApplyEulerAngles(new Vec3(pitch, roll, yaw));

			return new MatrixFrame(blendedRot, source.origin);
		}

		/// <summary>
		/// Return the source matrix frame moved toward the target frame, proportionaly to influences (0 to 1f).
		/// </summary>
		public static MatrixFrame BlendPositionTowards(MatrixFrame source, MatrixFrame target, float xInfluence, float yInfluence, float zInfluence)
		{
			Vec3 blendedPos = new Vec3(
				TaleWorlds.Library.MathF.Lerp(source.origin.x, target.origin.x, xInfluence),
				TaleWorlds.Library.MathF.Lerp(source.origin.y, target.origin.y, yInfluence),
				TaleWorlds.Library.MathF.Lerp(source.origin.z, target.origin.z, zInfluence)
			);

			return new MatrixFrame(source.rotation, blendedPos);
		}

		/// <summary>
		/// Return a color from a hex string (#RRGGBB or #RRGGBBAA).
		/// </summary>
		public static uint ColorFromHex(string hex)
		{
			if (!hex.StartsWith("#") || (hex.Length != 7 && hex.Length != 9))
			{
				Log($"[EntityUtils] Color '{hex}' has invalid syntax. Using default white (#ffffffff).", LogLevel.Warning);
				hex = "#ffffffff";
			}
			float red = hex.Length >= 7 ? Convert.ToInt32(hex.Substring(1, 2), 16) / 255f : 1f;
			float green = hex.Length >= 7 ? Convert.ToInt32(hex.Substring(3, 2), 16) / 255f : 1f;
			float blue = hex.Length >= 7 ? Convert.ToInt32(hex.Substring(5, 2), 16) / 255f : 1f;
			float alpha = hex.Length == 9 ? Convert.ToInt32(hex.Substring(7, 2), 16) / 255f : 1f;
			Color color = new Color(red, green, blue, alpha);
			return color.ToUnsignedInteger();
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
				mesh.SetEditDataPolicy(EditDataPolicy.KeepUntilFirstRender);
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
		/// Break a raw string into lines based on max width and glyph map,
		/// preferring word boundaries (space, or after '-').
		/// Won't cut words unless a single word exceeds maxWidth.
		/// </summary>
		private static List<Line> BreakIntoLines(string raw, float maxWidth, FixedGridGlyphMap glyphMap, float glyphW, float spacing)
		{
			var lines = new List<Line>();
			if (raw.Length == 0) { lines.Add(new Line(raw, 0, 0, 0f)); return lines; }

			int start = 0;
			float penX = 0f;
			int countInLine = 0;

			// Track the best soft-wrap spot in the current line.
			int lastBreakCut = -1;          // substring end index (exclusive) to cut at
			float lastBreakWidth = 0f;      // visual width if we cut there

			for (int i = 0; i < raw.Length; i++)
			{
				char ch = raw[i];

				// Hard line break
				if (ch == '\n')
				{
					float width = penX - (countInLine > 0 ? spacing : 0f);
					lines.Add(new Line(raw, start, i, Math.Max(0f, width)));

					start = i + 1;
					penX = 0f;
					countInLine = 0;
					lastBreakCut = -1;
					lastBreakWidth = 0f;
					continue;
				}

				// Measure this glyph
				float adv = glyphMap.AdvanceFor(ch, glyphW);
				float prospective = penX + adv + (countInLine > 0 ? spacing : 0f);

				// Record soft-wrap candidates:
				// - before a space (don't include trailing spaces in the line)
				// - after a hyphen (keep the hyphen at the end of the line)
				if (ch == ' ')
				{
					// break BEFORE this space
					float widthAtSpace = penX - (countInLine > 0 ? spacing : 0f);
					lastBreakCut = i;                  // exclude the space
					lastBreakWidth = Math.Max(0f, widthAtSpace);
				}
				else if (ch == '-')
				{
					// break AFTER this hyphen
					float widthAtHyphen = (penX + adv) - (countInLine > 0 ? spacing : 0f);
					lastBreakCut = i + 1;              // include the hyphen
					lastBreakWidth = Math.Max(0f, widthAtHyphen);
				}

				bool wrap = maxWidth > 0f && countInLine > 0 && prospective > maxWidth;
				if (wrap)
				{
					if (lastBreakCut > start)
					{
						// Soft wrap at last recorded boundary
						lines.Add(new Line(raw, start, lastBreakCut, lastBreakWidth));

						// Start next line after the cut; eat any subsequent spaces
						start = lastBreakCut;
						while (start < raw.Length && raw[start] == ' ') start++;

						// Reset line state and reprocess current char on new line
						penX = 0f;
						countInLine = 0;
						lastBreakCut = -1;
						lastBreakWidth = 0f;

						i = start - 1; // reprocess from new line
						continue;
					}
					else
					{
						// No soft break available -> hard wrap before current char
						float width = penX - (countInLine > 0 ? spacing : 0f);
						lines.Add(new Line(raw, start, i, Math.Max(0f, width)));

						start = i;
						penX = 0f;
						countInLine = 0;
						lastBreakCut = -1;
						lastBreakWidth = 0f;

						i = start - 1; // reprocess current char on the new line
						continue;
					}
				}

				// Accept this glyph
				penX += adv + (countInLine > 0 ? spacing : 0f);
				countInLine++;
			}

			// Flush last line
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
			static readonly Dictionary<int, int> UniToAtlas = BuildCp437Map();
			static Dictionary<int, int> UniToAtlas2 = BuildCp437Map();

			static Dictionary<int, int> BuildCp437Map()
			{
				var enc = Encoding.GetEncoding(437);
				var dict = new Dictionary<int, int>(256);
				for (int i = 0; i < 256; i++)
				{
					string s = enc.GetString(new[] { (byte)i });
					dict[s[0]] = i; // map that Unicode code point to CP437 index
				}
				return dict;
			}

			bool TryGetAtlasIndex(char ch, out int idx)
			{
				UniToAtlas2 ??= BuildCp437Map();
				return UniToAtlas2.TryGetValue(ch, out idx);
			}

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
				if (!TryGetAtlasIndex(ch, out int idx)) idx = '?'; // fallback char index

				int col = idx % Columns, row = idx / Columns;
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