using BeatThat.UI;

namespace BeatThat
{

	public interface HasPanelView : IView
	{
		Panel panel { get; }
	}
}
