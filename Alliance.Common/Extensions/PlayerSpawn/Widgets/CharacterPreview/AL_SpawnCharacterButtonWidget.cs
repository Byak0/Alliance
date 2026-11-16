#if !SERVER
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

namespace Alliance.Common.Extensions.PlayerSpawn.Widgets.CharacterPreview
{
	/// <summary>
	/// Character button of the spawn menu: raises the hover state to the shared formation tableau
	/// (dim light follows the hovered character, mirroring the button's own text highlight).
	/// </summary>
	public class AL_SpawnCharacterButtonWidget : MultiplayerClassLoadoutTroopSubclassButtonWidget
	{
		public AL_SpawnCharacterButtonWidget(UIContext context)
			: base(context)
		{
		}

		[Editor(false)]
		public float TableauX
		{
			get => _tableauX;
			set
			{
				if (value != _tableauX)
				{
					_tableauX = value;
					OnPropertyChanged(value, nameof(TableauX));
				}
			}
		}

		private float _tableauX;

		protected override void OnHoverBegin()
		{
			base.OnHoverBegin();
			AL_FormationTableau.HoveredX = _tableauX;
		}

		protected override void OnHoverEnd()
		{
			base.OnHoverEnd();
			AL_FormationTableau.HoveredX = -1f;
		}
	}
}
#endif
