using LunyScript;

public partial class NewLunyScript : Script
{
	public override void Build(ScriptBuildContext context)
	{
		On.Ready(Debug.Log($"Hello, {nameof(NewLunyScript)}"));
	}
}
