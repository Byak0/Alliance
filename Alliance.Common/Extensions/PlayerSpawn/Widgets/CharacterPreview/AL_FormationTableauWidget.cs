#if !SERVER
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	/// <summary>
	/// Single shared preview widget for a formation's character row: one scene, one camera, one
	/// render for all the characters (the per-character items overlay their own agent).
	/// Bindings: CharactersData ("id@x;id@x..." from the spawn menu VM) and SelectedX.
	/// </summary>
	public class AL_FormationTableauWidget : TextureWidget
	{
		public AL_FormationTableauWidget(UIContext context)
			: base(context)
		{
			TextureProviderName = "AL_FormationTableauProvider";
		}

		[Editor(false)]
		public string CharactersData
		{
			get => _charactersData;
			set
			{
				if (value != _charactersData)
				{
					_charactersData = value;
					OnPropertyChanged(value, nameof(CharactersData));
					SetTextureProviderProperty(nameof(CharactersData), value);
				}
			}
		}

		[Editor(false)]
		public float SelectedX
		{
			get => _selectedX;
			set
			{
				if (value != _selectedX)
				{
					_selectedX = value;
					OnPropertyChanged(value, nameof(SelectedX));
					SetTextureProviderProperty(nameof(SelectedX), value);
				}
			}
		}

		private string _charactersData;
		private float _selectedX = -1f;
	}
}
#endif
