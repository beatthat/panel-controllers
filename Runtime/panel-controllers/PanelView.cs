using BeatThat.GetComponentsExt;
using BeatThat.Panels;
using BeatThat.Transitions.Panels;

namespace BeatThat.Controllers.Panels
{
    public class PanelView : View, HasPanelView
	{
		public bool m_canUseParentPanel = true;

		public Panel panel 
		{
			get {
				if(m_panel != null) {
					return m_panel;
				}

				if((m_panel = GetComponent<Panel>()) != null) {
					return m_panel;
				}

				if(m_canUseParentPanel && this.transform.parent != null && (m_panel = this.transform.parent.GetComponent<Panel>()) != null) {
					return m_panel;
				}

				return (m_panel = this.gameObject.AddIfMissing<Panel, TransitionsPanel>());
			}
		}
		
		private Panel m_panel;
	}
}
;



