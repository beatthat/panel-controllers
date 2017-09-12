using System.Collections.Generic;
using BeatThat.Anim;

using System;
using UnityEngine;


namespace BeatThat
{

	/// <summary>
	/// Non-generic marker interface for panel controllers
	/// </summary>
	public interface IPanelController : IController, HasPanelTransitions {}

	public class PanelController<ModelType, ViewType>
		: Controller<ModelType, ViewType>, IPanelController
		where ViewType : class, HasPanelView
		where ModelType : class
	
	{
		/// <summary>
		/// If true will set GameObject.active to true on TransitionIn
		/// </summary>
		[HideInInspector][SerializeField]private bool m_setActiveOnTransitionIn = true;
		
		/// <summary>
		/// If true will set GameObject.active to false on TransitionOut
		/// </summary>
		[HideInInspector][SerializeField]private bool m_setInactiveOnTransitionOut = true;

		/// <summary>
		/// If true will destroy the presenter's GameObject on TransitionOut
		/// </summary>
		[HideInInspector][SerializeField]private bool m_destroyOnTransitionOut = false;


		/// <summary>
		/// If true will check for the panel to it's OUT state before starting an in transition
		/// </summary>
		[HideInInspector][SerializeField]private bool m_ensureOutBeforeTransitionIn = false;

		/// <summary>
		/// If true will check and (if necessary) rebind/go the presenter as part of TransitionIn
		/// </summary>
		[HideInInspector][SerializeField]private bool m_ensureResetBindGoOnTransitionIn = true;

		/// <summary>
		/// If true will check and (if necessary) transition out when the presenter unbinds
		/// </summary>
		[HideInInspector][SerializeField]private bool m_ensureTransitionOutOnUnbind = false;

		/// <summary>
		/// When true, signals transitions managed by this presenter to enable their debug logging
		/// </summary>
		[HideInInspector][SerializeField]private bool m_debugTransitions;

		[HideInInspector][SerializeField]private PanelTransitionState m_panelState = PanelTransitionState.OUT;

		private IDisposable TransitionBlock()
		{
			return m_debugTransitions? Requests.DebugStart(): Requests.DebugDisabled();
		}
		
		override public void Go()
		{
			// do nothing, but by don't call base.Go, which would set gameobject active.
			// better to let transitions control that.
		}
		
		public void ForceCompleteActiveTransitions()
		{
			if (m_activeTrans != null) { 
				m_activeTrans.CompleteEarly(); 
				m_activeTrans = null;
			}
		}

		public bool isInOrTransitioningIn
		{
			get {
				return this.hasView
					&& (this.panelState == PanelTransitionState.IN 
						|| this.panelState == PanelTransitionState.TRANSITIONING_IN);
			}
		}

		public bool isOutOrTransitioningOut
		{
			get {
				return this.hasView
					&& (this.panelState == PanelTransitionState.OUT 
						|| this.panelState == PanelTransitionState.TRANSITIONING_OUT);
			}
		}
		
		public void DismissImmediate()
		{
			this.view.panel.DismissImmediate();
			if(this.destroyOnTransitionOut) {
				Destroy(this.gameObject);
				return;
			}

			if(this.setInactiveOnTransitionOut) {
				this.gameObject.SetActive(false);
			}
			this.panelState = PanelTransitionState.OUT;
		}

		protected bool setActiveOnTransitionIn
		{
			get {
				return m_setActiveOnTransitionIn;
			}
			set {
				m_setActiveOnTransitionIn = value;
			}
		}
		
		protected bool ensureResetBindGoOnTransitionIn
		{
			get {
				return m_ensureResetBindGoOnTransitionIn;
			}
			set {
				m_ensureResetBindGoOnTransitionIn = value;
			}
		}

		protected bool ensureOutBeforeTransitionIn
		{
			get {
				return m_ensureOutBeforeTransitionIn;
			}
			set {
				m_ensureOutBeforeTransitionIn = value;
			}
		}
		
		protected bool setInactiveOnTransitionOut
		{
			get {
				return m_setInactiveOnTransitionOut;
			}
			set {
				m_setInactiveOnTransitionOut = value;
			}
		}

		protected bool destroyOnTransitionOut
		{
			get {
				return m_destroyOnTransitionOut;
			}
			set {
				m_destroyOnTransitionOut = value;
			}
		}

		#if APE_PANEL_CONTROLLER_LEGACY_TRANSITIONS
		virtual protected Transition BeforeTransitionIn()
		{
			return null;
		}
		
		virtual protected  Transition AfterTransitionIn()
		{
			return null;	
		}
		
		virtual protected Transition BeforeTransitionOut()
		{
			return null;
		}
		
		virtual protected Transition AfterTransitionOut()
		{
			return null;
		}
		#endif
		
		public OnTransitionFrameDelegate transitionInFrameDel
		{
			get; set;
		}
		
		public OnTransitionFrameDelegate transitionOutFrameDel
		{
			get; set;
		}

			
		public Transition PrepareGoAndTransitionInWith(ModelType m)
		{
			using(var debug = TransitionBlock()) {
				// TODO: make transition below into a class and then pool instances?
				var ct = new ChainTransition();
				
				ct.AddAction(() => {
					if(this.isBound) {
						Unbind();
					}
					this.model = m;
				});
				
				ct.Add(PrepareTransitionIn(true));
				
				return IfNotDestroyed(ct);
			}
		}
		
		private Transition RunAsActiveTransition(Transition t)
		{
			using(var debug = TransitionBlock()) {
				// TODO: make transition below into a class and then pool instances?

				ForceCompleteActiveTransitions();

				var ct = new ChainTransition().Add(t);
				ct.AddAction(() => {
					if(m_activeTrans == ct) {
						m_activeTrans = null;
					}
				});

				m_activeTrans = IfNotDestroyed(ct);
				m_activeTrans.StartTransition();
				
				return t;
			}
		}

		public void ChangeModel(ModelType m, bool recallGo = true)
		{
			if(m == null) {
				TransitionOut(true);
				return;
			}

			if(!this.isBound) {
				GoAndTransitionInWith(m);
				return;
			}



			var stateBefore = this.panelState;

			Unbind();
			Reset();
			this.model = m;
			Bind();

			if(recallGo) {
				Go();
			}

			if(this.isInOrTransitioningIn) {
				return;
			}

			if(stateBefore == PanelTransitionState.IN) {
				TransitionInImmediate();
			}
			else {
				TransitionIn();
			}
		}
		
		public void GoAndTransitionInWith(ModelType model)
		{
			RunAsActiveTransition(PrepareGoAndTransitionInWith(model));
		}
		
		public void TransitionIn()
		{
			RunAsActiveTransition(PrepareTransitionIn());
		}

		public void TransitionInImmediate()
		{
			RunAsActiveTransition(PrepareTransitionIn()).CompleteEarly();
		}
		
		public void TransitionOut(bool unbind)
		{
			if(!this.isBound) {
				return;
			}

			if(this.isOutOrTransitioningOut) {
				return;
			}

			RunAsActiveTransition(PrepareTransitionOut(unbind));
		}
		
		public void TransitionOutImmediate()
		{
			RunAsActiveTransition(PrepareTransitionOut(false)).CompleteEarly();
		}


		// TODO: either fix or get rid of in/out transitions. Right now allocates a whole bunch of GC objects for every prepare
		public Transition PrepareTransitionIn()
		{
			return PrepareTransitionIn(false);
		}

		public Transition EnsureTransitionIn()
		{
			using(var debug = TransitionBlock()) {
				var t = new ConditionalTransition();
				t.If(() => (this.hasView && !this.isInOrTransitioningIn), PrepareTransitionIn(false));
				return t;
			}
		}

		private Transition PrepareTransitionIn(bool forceEnsureGo)
		{
			using(var debug = TransitionBlock()) {
				// TODO: make transition below into a class and then pool instances?
				var ct = new ChainTransition();

				// make sure game object is active (most transitions won't work if inactive)
				if(this.setActiveOnTransitionIn) {
					ct.AddAction(() => this.gameObject.SetActive (true));
				}

				// make sure we have a view and that it's "off screen"
				ct.AddAction(() => { 
					if(!this.isBound) {
						Reset();
					}
					if(this.panelState != PanelTransitionState.OUT && this.ensureOutBeforeTransitionIn) {
						DismissImmediate();
					}
					this.panelState = PanelTransitionState.TRANSITIONING_IN;
				});

				// now offscreen, make sure the presenter is active
				if(this.ensureResetBindGoOnTransitionIn || forceEnsureGo) {
					ct.AddAction(() => { 
						if(!this.isBound) {
							Bind();
							Go();
						}
					});
				}
				
				if(this.setActiveOnTransitionIn) {
					ct.AddAction(() => this.gameObject.SetActive (true));
				}

				#if APE_PANEL_CONTROLLER_LEGACY_TRANSITIONS
				ct.AddJIT(this.BeforeTransitionIn);
				#endif
				
				ct.AddJIT(this.PreparePanelTransitionIn);

				#if APE_PANEL_CONTROLLER_LEGACY_TRANSITIONS
				ct.AddJIT(this.AfterTransitionIn);
				#endif

				ct.AddAction(() => { 
					this.panelState = PanelTransitionState.IN; 
				});
				
				return IfNotDestroyed(ct);
			}
		}
		
		protected Transition PreparePanelTransitionIn()
		{
			using(var debug = TransitionBlock()) {
				return this.view.panel.PrepareTransition(
					PanelTransition.IN, this.TransitionInFrame);
			}
		}
		
		virtual protected void TransitionInFrame(float transitionTimer, float dTimer)
		{
			if(this.transitionInFrameDel != null) {
				this.transitionInFrameDel(transitionTimer, dTimer);
			}
		}
		
		virtual protected void TransitionOutFrame(float transitionTimer, float dTimer)
		{
			if(this.transitionOutFrameDel != null) {
				this.transitionOutFrameDel(transitionTimer, dTimer);
			}
		}

		public Transition PrepareTransitionOut()
		{
			return PrepareTransitionOut(false);
		}

		// TODO: either fix or get rid of in/out transitions. Right now allocates a whole bunch of GC objects for every prepare
		public Transition PrepareTransitionOut(bool unbind)
		{
			#if BT_DEBUG_UNSTRIP
			if(m_debugTransitions) { Debug.Log("[" + Time.frameCount + "][" + this.Path() + "]::PrepareTransitionOut unbind=" + unbind); }
			#endif

			using(var debug = TransitionBlock()) {
				var tranOut = new ChainTransition(m_debugTransitions? this.name + "-out": "");

				if(this.isValid && this.hasView) {

					tranOut.Add(new ConditionalTransition()
			            .If(this.HasView, 
							new ChainTransition()
								.AddAction(() => { 
									this.panelState = PanelTransitionState.TRANSITIONING_OUT;
								})
//								.AddJIT(this.BeforeTransitionOut)
								.AddJIT(this.PreparePanelTransitionOut)
//								.AddJIT(this.AfterTransitionOut)
								.AddAction(() => {
									if(this.isValid && this.setInactiveOnTransitionOut) {
										this.gameObject.SetActive(false);
									}
								})
								
						))
					.AddAction(() => { 
						#if BT_DEBUG_UNSTRIP
						if(m_debugTransitions) { Debug.Log("[" + Time.frameCount + "][" + this.Path() + "] TRANSITION doing Unbind action... unbind=" + unbind); }
						#endif

						if(unbind) { 
							Unbind();
						}
					})
					.AddAction(() => { 
						#if BT_DEBUG_UNSTRIP
						if(m_debugTransitions) { Debug.Log("[" + Time.frameCount + "][" + this.Path() + "] TRANSITION changing panel state to OUT"); }
						#endif

						this.panelState = PanelTransitionState.OUT;
					})
					.AddAction(() => {
						if(this.isValid && this.destroyOnTransitionOut) {
							Destroy(this.gameObject);
						}
					});
				}
				
				return IfHasView(tranOut);
			}
		}
		
		protected Transition PreparePanelTransitionOut()
		{
			using(var debug = TransitionBlock()) {
				if(!this.isValid || !this.hasView) {
					return InstantActionTransition.DONE;
				}
				else {
					var myOut = this.view.panel.PrepareTransition(
							PanelTransition.OUT, this.TransitionOutFrame);

					if(m_subpanels == null || m_subpanels.Count == 0) {
						return myOut;
					}

					var allOut = new JoinTransition();
					if(myOut != null) {
						allOut.Add(myOut);
					}

					foreach(var p in m_subpanels) {
						allOut.Add(p.PrepareTransitionOut());
					}

					return allOut;
				}
			}
		}

		sealed override protected void BindController()
		{
			BindPanel();
		}

		virtual protected void BindPanel() {}

		sealed override protected void UnbindController()
		{
			if(this.isValid && this.hasView) {

				if(m_ensureTransitionOutOnUnbind && this.panelState != PanelTransitionState.OUT) {
					TransitionOutImmediate();
					ReleaseView();
				}

			}

			if(m_subpanels != null) {
				m_subpanels.Clear();
			}

			if(m_ensureTransitionOutOnUnbind) {
				this.panelState = PanelTransitionState.OUT;
			}

			UnbindPanel();
			base.UnbindController();
		}

		virtual protected void UnbindPanel() {}
		
		public PanelTransitionState panelState
		{
			get {return m_panelState;}
			protected set { m_panelState = value; }
		}

		protected void AddSubpanels(params IController[] subpanels)
		{
			if(subpanels == null || subpanels.Length == 0) {
				return;
			}

			if(m_subpanels == null) {
				m_subpanels = new List<HasPanelTransitions>();
			}

			foreach(var p in subpanels) {
				if(p is HasPanelTransitions) {
					m_subpanels.Add((HasPanelTransitions)p);
				}
				Attach(p);
			}
		}

		private Transition IfNotDestroyed(Transition t)
		{
			return new SafeTransition(t, this.gameObject);
		}

		private Transition IfHasView(Transition t)
		{
			return new SafeTransition(t, this.gameObject, this.HasView);
		}

		private List<HasPanelTransitions> m_subpanels;
		private Transition m_activeTrans;
	}

	/// <summary>
	/// A PanelController that is also its own View.
	/// The main reason to ever have a PanelController with a separate view component
	/// is if you want to instantiate the view on demand.
	/// </summary>
	public class PanelController<ModelType> : PanelController<ModelType, HasPanelView>
		where ModelType : class
	{
		#region View implementation
		
		virtual public void Release() {}


		override protected HasPanelView CreateView()
		{
			var v = FindView () ?? this.AddIfMissing<PanelView>();
			ApplyLayers(v);
			return v;
		}

		sealed override protected void ResetController()
		{
			this.view = null;
			DoPanelReset();
			base.ResetController();
		}
		
		virtual protected void DoPanelReset()
		{
		}
		
		public IController controller { get { return this; } }
	
		#endregion

		
		public Panel panel { get { return m_panel?? (m_panel = this.gameObject.AddIfMissing<Panel, TransitionsPanel>()); } }
		private Panel m_panel;
	}

	public class PanelController : PanelController<NoModel> 
	{
	}


}
