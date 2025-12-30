namespace LunyScript.Unity.Tests
{
	public abstract class LunyScriptTestBase : LunyScript
	{
		protected void DidRun()
		{
			var name = GetType().Name;
			if (!GlobalVariables[name].Boolean())
				GlobalVariables[name] = true;
		}

		public override void Build()
		{
			Debug.Log($"Building {GetType().Name}");
		}
	}

	public sealed class Assert_Runs_WhenCreated : LunyScriptTestBase
	{
		public override void Build()
		{
			base.Build();
			When.Created(Run(DidRun));
		}
	}

	public sealed class Assert_Runs_WhenDestroyed : LunyScriptTestBase
	{
		public override void Build()
		{
			When.Created(Object.Destroy());
			When.Destroyed(Run(DidRun));
		}
	}

	public sealed class Assert_Runs_WhenEnabled : LunyScriptTestBase
	{
		public override void Build() => When.Enabled(Run(DidRun));
	}

	public sealed class Assert_Runs_WhenDisabledExternally : LunyScriptTestBase
	{
		public override void Build()
		{
			When.Created(Object.SetDisabled());
		}
	}

	public sealed class Assert_Runs_WhenDisabled : LunyScriptTestBase
	{
		public override void Build()
		{
			When.Created(Object.SetDisabled());
			When.Disabled(Run(DidRun));
		}
	}

	public sealed class Assert_Runs_WhenReady : LunyScriptTestBase
	{
		public override void Build() => When.Ready(Run(DidRun));
	}

	public sealed class Assert_Runs_EveryFixedStep : LunyScriptTestBase
	{
		public override void Build() => Every.FixedStep(Run(DidRun));
	}

	public sealed class Assert_Runs_EveryFrame : LunyScriptTestBase
	{
		public override void Build() => Every.Frame(Run(DidRun));
	}

	public sealed class Assert_Runs_EveryFrameEnds : LunyScriptTestBase
	{
		public override void Build() => Every.FrameEnds(Run(DidRun));
	}
}
