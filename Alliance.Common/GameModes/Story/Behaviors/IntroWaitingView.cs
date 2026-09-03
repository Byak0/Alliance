#if !SERVER
using Alliance.Common.GameModes.Story.Models;
using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace Alliance.Common.GameModes.Story.Behaviors
{
	/// <summary>
	/// Full-black waiting screen shown while the scenario waits for players to load:
	/// hides everything behind (all other mission UI layers) and displays the ready/total
	/// player counter driven by WaitingScreenStateMessage (routed through StoryHandler).
	/// Always enabled - the server decides when to show it.
	/// </summary>
	public class IntroWaitingView : MissionView
	{
		private GauntletLayer _layer;
		private ScreenBase _screen;
		private IntroWaitingVM _vm;
		private bool _visible;
		private readonly HashSet<ScreenLayer> _hiddenLayers = new HashSet<ScreenLayer>();

		public IntroWaitingView()
		{
			ViewOrderPriority = 50;
		}

		public bool IsVisible => _visible;

		public void SetState(bool visible, int ready, int total)
		{
			if (visible && !_visible) Show();
			else if (!visible && _visible) Hide();
			if (_visible)
			{
				_vm.ReadyPlayers = ready;
				_vm.TotalPlayers = total;
			}
		}

		private void Show()
		{
			_visible = true;
			try
			{
				_vm = new IntroWaitingVM();
				_screen = MissionScreen ?? ScreenManager.TopScreen;
				if (_screen != null)
				{
					_layer = new GauntletLayer("IntroWaitingScreen", 60);
					_layer.LoadMovie("IntroWaitingScreen", _vm);
					_screen.AddLayer(_layer);
					HideOtherLayers();
				}
			}
			catch (Exception)
			{
				Hide();
			}
		}

		private void Hide()
		{
			_visible = false;
			RestoreLayers();
			if (_layer != null && _screen != null)
			{
				try { _screen.RemoveLayer(_layer); } catch { }
				_layer = null;
				_vm = null;
				_screen = null;
			}
		}

		/// <summary>Same approach as CinematicView: hide every other Gauntlet layer's root widget so
		/// nothing shows through the black screen. Chat stays visible by design.</summary>
		private void HideOtherLayers()
		{
			if (_layer == null || _screen == null) return;
			foreach (ScreenLayer layer in _screen.Layers)
			{
				if (layer == _layer || !(layer is GauntletLayer gauntletLayer)) continue;
				Widget root = gauntletLayer.UIContext?.Root;
				if (root == null || !root.IsVisible) continue;
				root.IsVisible = false;
				_hiddenLayers.Add(layer);
			}
		}

		private void RestoreLayers()
		{
			foreach (ScreenLayer layer in _hiddenLayers)
			{
				if (layer.IsFinalized) continue;
				Widget root = (layer as GauntletLayer)?.UIContext?.Root;
				if (root != null) root.IsVisible = true;
			}
			_hiddenLayers.Clear();
		}

		public override void OnRemoveBehavior()
		{
			base.OnRemoveBehavior();
			Hide();
		}
	}

	public class IntroWaitingVM : ViewModel
	{
		private static readonly LocalizedString WaitingText = new LocalizedString("Waiting for players");

		private int _readyPlayers;
		private int _totalPlayers;

		[DataSourceProperty]
		public string Title => WaitingText.LocalizedText;

		[DataSourceProperty]
		public string CounterText => $"{_readyPlayers} / {_totalPlayers}";

		[DataSourceProperty]
		public int ReadyPlayers
		{
			get => _readyPlayers;
			set { if (value != _readyPlayers) { _readyPlayers = value; OnPropertyChangedWithValue(value); OnPropertyChanged(nameof(CounterText)); } }
		}

		[DataSourceProperty]
		public int TotalPlayers
		{
			get => _totalPlayers;
			set { if (value != _totalPlayers) { _totalPlayers = value; OnPropertyChangedWithValue(value); OnPropertyChanged(nameof(CounterText)); } }
		}
	}
}
#endif
