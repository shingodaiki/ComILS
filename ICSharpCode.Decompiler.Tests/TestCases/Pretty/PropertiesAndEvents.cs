﻿using System;

namespace ICSharpCode.Decompiler.Tests.TestCases.Pretty
{
	internal class PropertiesAndEvents
	{
		private interface IBase
		{
			int Test {
				get;
				set;
			}

			event Action Event;
		}

		private abstract class BaseClass
		{
			public abstract event EventHandler ThisIsAnAbstractEvent;
		}

		private class OtherClass : BaseClass
		{
			public override event EventHandler ThisIsAnAbstractEvent;
		}

		private class Impl : IBase
		{
			int IBase.Test {
				get {
					throw new NotImplementedException();
				}
				set {
				}
			}

			event Action IBase.Event {
				add {
				}
				remove {
				}
			}
		}

		[NonSerialized]
		private int someField;

		public int Value {
			get;
			private set;
		}

#if ROSLYN
		public int NotAnAutoProperty => someField;
#else
		public int NotAnAutoProperty {
			get {
				return someField;
			}
		}
#endif

		public event EventHandler AutomaticEvent;

		[field: NonSerialized]
		public event EventHandler AutomaticEventWithInitializer = delegate {
		};

		public event EventHandler CustomEvent {
			add {
				AutomaticEvent += value;
			}
			remove {
				AutomaticEvent -= value;
			}
		}
	}
}
