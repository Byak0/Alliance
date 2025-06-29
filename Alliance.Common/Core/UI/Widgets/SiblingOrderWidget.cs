#if !SERVER
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace Alliance.Common.Core.UI.Widgets
{
	/// <summary>
	/// Simple widget that can move among its siblings to control the order of rendering.
	/// </summary>
	public class SiblingOrderWidget : Widget
	{
		private int _siblingOrder = 0;

		[Editor(false)]
		public int SiblingOrder
		{
			get => _siblingOrder;
			set
			{
				// Move widget to the correct position in the sibling order.
				if (ParentWidget == null) return;

				int indexMax = ParentWidget.ChildCount - 1;
				if (value < 0)
				{
					_siblingOrder = 0;
				}
				else if (value > indexMax)
				{
					_siblingOrder = indexMax;
				}
				else
				{
					_siblingOrder = value;
				}

				// Index max gets rendered first, so we subtract the sibling order from it.
				SetSiblingIndex(indexMax - _siblingOrder);
			}
		}

		public SiblingOrderWidget(UIContext context)
			: base(context)
		{
		}
	}
}
#endif