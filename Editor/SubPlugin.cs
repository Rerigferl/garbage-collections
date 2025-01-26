
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;

namespace Numeira;

internal sealed class SubPluginConfigureContext
{
    private GarbageCollections plugin;
    public SubPluginConfigureContext(GarbageCollections plugin) => this.plugin = plugin;

    public Sequence InPhase(BuildPhase phase) => plugin.InPhase(phase);
}

internal interface ISubPlugin
{
    void Configure(SubPluginConfigureContext context);
    string QualifiedName { get; }
}

internal abstract class SubPlugin : ISubPlugin
{
    public abstract void Configure(SubPluginConfigureContext context);

    public abstract string QualifiedName { get; }

    public abstract bool IsEnabled { get; set; }
}


internal abstract class SubPlugin<T> : SubPlugin where T : SubPlugin<T>, new()
{
    public static T Default { get; } = new T();

    public override bool IsEnabled
    {
        get => Configurations.instance.EnabledPlugins.Contains(QualifiedName);
        set => _ = value switch
        {
            true => GarbageCollections.Configurations.EnabledPlugins.Add(QualifiedName),
            false => GarbageCollections.Configurations.EnabledPlugins.Remove(QualifiedName),
        };
    }
}