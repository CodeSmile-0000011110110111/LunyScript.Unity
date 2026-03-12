using LunyScript;

public partial class NewLunyScript1 : Script
{
	public override void Build(ScriptContext context)
	{
		On.Ready(Debug.Log($"Hello, {nameof(NewLunyScript1)}"));
	}
}
