using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using UnityEditor;

[assembly: ExportsPlugin(typeof(Numeira.GarbageCollections))]

namespace Numeira;


internal sealed class GarbageCollections : Plugin<GarbageCollections>
{
    public override string DisplayName => "Numeira's Garbage Collections";
    public override string QualifiedName => "numeira.garbage-collections";

    public static Configurations Configurations => Configurations.instance;

    static GarbageCollections()
    {
        CollectSubPlugins();
        EditorApplication.quitting += () => Configurations.Save();
    }

    private static Dictionary<Type, SubPlugin> subPlugins = new();

    private static void CollectSubPlugins()
    {
        Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(SubPlugin).IsAssignableFrom(x)).Select(type =>
        {
            var genericType = typeof(SubPlugin<>).MakeGenericType(type);
            if (genericType.IsAssignableFrom(type))
            {
                return genericType.GetField("Default").GetValue(null) as SubPlugin;
            }
            return Activator.CreateInstance(type) as SubPlugin;
        });

        foreach(var type in Assembly.GetExecutingAssembly().GetTypes().Where(x => typeof(SubPlugin).IsAssignableFrom(x)))
        {
            if (type.IsAbstract || subPlugins.ContainsKey(type))
                continue;

            SubPlugin? instance;

            var genericType = typeof(SubPlugin<>).MakeGenericType(type);
            if (genericType.IsAssignableFrom(type))
            {
                var property = genericType.GetProperty("Default", BindingFlags.Static | BindingFlags.Public);
                instance = property.GetValue(null) as SubPlugin;

            }
            else
            {
                instance = Activator.CreateInstance(type) as SubPlugin;
            }

            if (instance != null)
            {
                subPlugins.Add(type, instance);
            }
        }
    }

    protected override void Configure()
    {
        var context = new SubPluginConfigureContext(this);
        foreach(var subPlugin in subPlugins.Values)
        {
            try
            {
                if (subPlugin.IsEnabled)
                    subPlugin.Configure(context);
            }
            // catch () { }
            finally
            {

            }
        }
    }

    internal new Sequence InPhase(BuildPhase phase) => base.InPhase(phase);
}

[FilePath("numeira/garbage-collections", FilePathAttribute.Location.ProjectFolder)]

internal sealed class Configurations : ScriptableSingleton<Configurations>
{
    public HashSet<string> EnabledPlugins = new();

    public void Save() => Save(false);
}
