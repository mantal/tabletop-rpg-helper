using System;

namespace RPG.Services
{
    public class Events
	{
		public event EventHandler? SheetUpdated;

		public virtual void OnSheetUpdated()
		{
			SheetUpdated?.Invoke(this, EventArgs.Empty);
		}
	}
}
