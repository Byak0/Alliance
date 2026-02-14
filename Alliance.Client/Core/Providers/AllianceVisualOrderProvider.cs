using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View.VisualOrders.Orders.ToggleOrders;
using TaleWorlds.MountAndBlade.View.VisualOrders.OrderSets;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.FormOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.MovementOrders;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual.Default.Orders.ToggleOrders;

namespace Alliance.Client.Core.Providers
{
	public class AllianceVisualOrderProvider : VisualOrderProvider
	{
		// List of orders comes from DefaultVisualOrderProvider
		public override MBReadOnlyList<VisualOrderSet> GetOrders()
		{
			MBList<VisualOrderSet> orders = new MBList<VisualOrderSet>();

			GenericVisualOrderSet movementOrders = new GenericVisualOrderSet("order_type_movement", new TextObject("{=KiJd6Xik}Movement", null), true, true);
			movementOrders.AddOrder(new MoveVisualOrder("order_movement_move"));
			movementOrders.AddOrder(new FollowMeVisualOrder("order_movement_follow"));
			movementOrders.AddOrder(new ChargeVisualOrder("order_movement_charge"));
			movementOrders.AddOrder(new AdvanceVisualOrder("order_movement_advance"));
			movementOrders.AddOrder(new FallbackVisualOrder("order_movement_fallback"));
			movementOrders.AddOrder(new StopVisualOrder("order_movement_stop"));
			movementOrders.AddOrder(new RetreatVisualOrder("order_movement_retreat"));
			movementOrders.AddOrder(new ReturnVisualOrder());

			GenericVisualOrderSet formationOrders = new GenericVisualOrderSet("order_type_form", new TextObject("{=iBk2wbn3}Form", null), true, true);
			ArrangementVisualOrder lineFormationOrder = new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Line, "order_form_line");
			ArrangementVisualOrder shieldWallFormationOrder = new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.ShieldWall, "order_form_close");
			formationOrders.AddOrder(lineFormationOrder);
			formationOrders.AddOrder(shieldWallFormationOrder);
			formationOrders.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Loose, "order_form_loose"));
			formationOrders.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Circle, "order_form_circular"));
			formationOrders.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Square, "order_form_schiltron"));
			formationOrders.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Skein, "order_form_v"));
			formationOrders.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Column, "order_form_column"));
			formationOrders.AddOrder(new ArrangementVisualOrder(ArrangementOrder.ArrangementOrderEnum.Scatter, "order_form_scatter"));
			formationOrders.AddOrder(new ReturnVisualOrder());
			
			GenericVisualOrderSet toggleOrders = new GenericVisualOrderSet("order_type_toggle", new TextObject("{=0HTNYQz2}Toggle", null), false, false);
			ToggleFacingVisualOrder toggleFaceEnemyOrder = new ToggleFacingVisualOrder("order_toggle_facing");
			GenericToggleVisualOrder toggleFireOrder = new GenericToggleVisualOrder("order_toggle_fire", OrderType.FireAtWill, OrderType.HoldFire);
			GenericToggleVisualOrder toggleMountOrder = new GenericToggleVisualOrder("order_toggle_mount", OrderType.Mount, OrderType.Dismount);
			
			// These 2 are natively disabled in multiplayer, we reenable them
			GenericToggleVisualOrder toggleAIControlOrder = new GenericToggleVisualOrder("order_toggle_ai", OrderType.AIControlOn, OrderType.AIControlOff);			
			TransferTroopsVisualOrder toggleTroopTransferOrder = new TransferTroopsVisualOrder();

			toggleOrders.AddOrder(toggleFaceEnemyOrder);
			toggleOrders.AddOrder(toggleFireOrder);
			toggleOrders.AddOrder(toggleMountOrder);
			toggleOrders.AddOrder(toggleAIControlOrder);
			toggleOrders.AddOrder(toggleTroopTransferOrder);
			toggleOrders.AddOrder(new ReturnVisualOrder());

			orders.Add(movementOrders);
			orders.Add(formationOrders);
			orders.Add(toggleOrders);

			if (!Input.IsGamepadActive)
			{
				orders.Add(new SingleVisualOrderSet(toggleFireOrder));
				orders.Add(new SingleVisualOrderSet(toggleMountOrder));
				if (toggleAIControlOrder != null)
				{
					orders.Add(new SingleVisualOrderSet(toggleAIControlOrder));
				}
				orders.Add(new SingleVisualOrderSet(toggleFaceEnemyOrder));
				orders.Add(new SingleVisualOrderSet(shieldWallFormationOrder));
				orders.Add(new SingleVisualOrderSet(lineFormationOrder));
			}

			return orders;
		}

		public override bool IsAvailable()
		{
			return true;
		}
	}
}