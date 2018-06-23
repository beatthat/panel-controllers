using BeatThat.Panels;

namespace BeatThat.Controllers.Panels
{

    public interface HasPanelView : IView
	{
		Panel panel { get; }
	}
}


