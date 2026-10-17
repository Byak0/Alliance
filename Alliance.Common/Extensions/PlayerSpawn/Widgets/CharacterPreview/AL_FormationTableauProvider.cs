#if !SERVER
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI;
using TaleWorlds.TwoDimension;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	/// <summary>
	/// Texture provider of the shared formation tableau: ONE texture, ONE camera and ONE render
	/// per frame for the whole character row of a formation (instead of one per character).
	/// </summary>
	public class AL_FormationTableauProvider : TextureProvider
	{
		private AL_FormationTableau _formationTableau;
		private TaleWorlds.Engine.Texture _texture;
		private Texture _providedTexture;

		public AL_FormationTableauProvider()
		{
			_formationTableau = new AL_FormationTableau();
		}

		/// <summary>"id@x;id@x..." - character ids with their normalized row position (0..1).</summary>
		public string CharactersData
		{
			set => _formationTableau.SetCharacters(AL_FormationTableau.ParseCharactersData(value));
		}

		/// <summary>Normalized row position of the selected character (-1 = none).</summary>
		public float SelectedX
		{
			set => _formationTableau.SetSelected(value);
		}

		protected override Texture OnGetTextureForRender(TwoDimensionContext twoDimensionContext, string name)
		{
			CheckTexture();
			return _providedTexture;
		}

		private void CheckTexture()
		{
			if (_texture != _formationTableau.Texture)
			{
				_texture = _formationTableau.Texture;
				if (_texture != null)
				{
					EngineTexture platformTexture = new EngineTexture(_texture);
					_providedTexture = new Texture(platformTexture);
				}
				else
				{
					_providedTexture = null;
				}
			}
		}

		public override void SetTargetSize(int width, int height)
		{
			base.SetTargetSize(width, height);
			_formationTableau.SetTargetSize(width, height);
		}

		public override void Tick(float dt)
		{
			base.Tick(dt);
			_formationTableau.Tick(dt);
		}

		public override void Clear(bool clearNextFrame)
		{
			_formationTableau.OnFinalize();
			base.Clear(clearNextFrame);
		}
	}
}
#endif
