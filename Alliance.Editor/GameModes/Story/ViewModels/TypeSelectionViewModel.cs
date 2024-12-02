using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Alliance.Editor.GameModes.Story.ViewModels
{
	/// <summary>
	/// VM for the dialog allowing user to select a type.
	/// </summary>
	public class TypeSelectionViewModel : INotifyPropertyChanged
	{
		public List<Type> ConcreteTypes { get; }
		public Type SelectedType { get; set; }

		public ICommand OKCommand { get; }

		public TypeSelectionViewModel(List<Type> concreteTypes)
		{
			ConcreteTypes = concreteTypes;
			OKCommand = new RelayCommand(OnOK, CanSelect);
		}

		private void OnOK(object parameter)
		{
			if (parameter is Window window)
			{
				window.DialogResult = true;
				window.Close();
			}
		}

		private bool CanSelect(object parameter)
		{
			return SelectedType != null;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
