using BeatThat.Panels;
using BeatThat.SafeRefs;
using BeatThat.Transitions;
using UnityEngine;

namespace BeatThat.Controllers.Panels
{
    class StubPanel : Panel
	{
		public StubPanel(Transform t, bool warnOnUse = true) 
		{ 
			this.transform = t; 
			this.warnOnUse = warnOnUse;
		}

		public Transition PrepareTransition (PanelTransition t, OnTransitionFrameDelegate onFrameDel = null)
		{
			#if BT_DEBUG_UNSTRIP
			if(this.warnOnUse) {
				Debug.LogWarning("[" + Time.frameCount + "][" + this.transform.Path() + "] PrepareTransition called on stub panel");
			}
			#endif
			return new InstantActionTransition(() => {});
		}
		public void StartTransition (PanelTransition t)
		{
			#if BT_DEBUG_UNSTRIP
			if(this.warnOnUse) {
				Debug.LogWarning("[" + Time.frameCount + "][" + this.transform.Path() + "] StartTransition called on stub panel");
			}
			#endif
		}
		public void BringInImmediate ()
		{
			#if BT_DEBUG_UNSTRIP
			if(this.warnOnUse) {
				Debug.LogWarning("[" + Time.frameCount + "][" + this.transform.Path() + "] BringInImmediate called on stub panel");
			}
			#endif
		}
		public void DismissImmediate ()
		{
			#if BT_DEBUG_UNSTRIP
			if(this.warnOnUse) {
				Debug.LogWarning("[" + Time.frameCount + "][" + this.transform.Path() + "] DismissImmediate called on stub panel");
			}
			#endif
		}
			
		public Transform transform { get { return m_transform.value; } set { m_transform = new SafeRef<Transform>(value); } }
		private SafeRef<Transform> m_transform;

		public bool warnOnUse { get; set; }
	}
}



