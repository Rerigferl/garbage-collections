using System;
using System.Text.RegularExpressions;
using nadena.dev.ndmf;
using VRC.SDK3.Avatars.Components;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.CompilerServices;
using System.Linq;
using LipSyncStyle = VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle;
using Object = UnityEngine.Object;
using NUnit.Framework;
using System.Collections.Generic;

namespace Numeira;

internal sealed class LipSyncFix : SubPlugin<LipSyncFix>
{
    public override string QualifiedName => "lip-sync-fix";

    public override bool IsEnabled { get; set; } = true;

    public override void Configure(SubPluginConfigureContext context)
    {
        context.InPhase(BuildPhase.Generating).Run(QualifiedName, Run);
    }

    private void Run(BuildContext context)
    {
        var descriptor = context.AvatarDescriptor;
        var component = descriptor.GetComponent<LipSyncFixComponent>();
        if (descriptor.lipSync is not (LipSyncStyle.JawFlapBlendShape or LipSyncStyle.VisemeBlendShape) ||
            component == null || component.Face == null)
            return;

        Regex shapeSelector;
        try
        {
           shapeSelector = new Regex(component.MouthBlendShapeSelector);
        }
        catch (ArgumentException)
        {
            return;
        }

        var originalMesh = new ReadOnlyMesh(component.Face.sharedMesh);
        if (originalMesh.IsEmpty)
            return;

        var smr = component.Face;
        var mesh = originalMesh.Clone();
        mesh.name = $"{originalMesh.Name} (LipSyncFix)";
        AssetDatabase.AddObjectToAsset(mesh, context.AssetContainer);
        mesh.ClearBlendShapes();

        var buffer = originalMesh.GetBlendShapeBuffer(BlendShapeBufferLayout.PerShape);
        var count = originalMesh.BlendshapeCount;

        string[] blendShapeNames = new string[count];
        var lipSyncBlendshapes = descriptor.VisemeBlendShapes.Where(x => !component.LipSyncBlendShapeBlacklist.Contains(x, StringComparer.OrdinalIgnoreCase)).ToHashSet();
        List<(int Index, string Name)> mouthBlendShapes;
        {
            mouthBlendShapes = new();
            for (int i = 0; i < blendShapeNames.Length; i++)
            {
                var name = originalMesh.GetBlendShapeName(i);
                blendShapeNames[i] = name;
                if (lipSyncBlendshapes.Contains(name) || !shapeSelector.IsMatch(name))
                    continue;

                mouthBlendShapes.Add((i, name));
            }
        }

        int vertexCount = mesh.vertexCount;

        var deltaVerticies = new Vector3[vertexCount];
        var deltaNormals = new Vector3[vertexCount];
        var deltaTangents = new Vector3[vertexCount];

        var deltaVerticies2 = new Vector3[vertexCount];
        var deltaNormals2 = new Vector3[vertexCount];
        var deltaTangents2 = new Vector3[vertexCount];

        for (int i = 0; i < blendShapeNames.Length; i++)
        {
            var name = blendShapeNames[i];
            var weight = originalMesh.GetBlendShapeFrameWeight(i, 0);
            originalMesh.GetBlendShapeFrameVertices(i, 0, deltaVerticies, deltaNormals, deltaTangents);

            if (lipSyncBlendshapes.Contains(name))
            {
                foreach(var mouth in mouthBlendShapes)
                {
                    var mouthWeight = smr.GetBlendShapeWeight(mouth.Index) / 100f;
                    if (mouthWeight == 0)
                        continue;
                    mouthWeight = -mouthWeight;

                    originalMesh.GetBlendShapeFrameVertices(mouth.Index, 0, deltaVerticies2, deltaNormals2, deltaTangents2);
                    Debug.Assert(deltaVerticies2.Length == deltaNormals2.Length && deltaVerticies2.Length == deltaTangents2.Length);
                    for (int i2 = 0; i2 < deltaVerticies2.Length; i2++)
                    {
                        deltaVerticies[i2] = deltaVerticies[i2] + deltaVerticies2[i2] * mouthWeight;
                        deltaNormals[i2] = deltaNormals[i2] + deltaNormals2[i2] * mouthWeight;
                        deltaTangents[i2] = deltaTangents[i2] + deltaTangents2[i2] * mouthWeight;
                    }
                }
            }

            mesh.AddBlendShapeFrame(name, weight, deltaVerticies, deltaNormals, deltaTangents);
        }

        component.Face.sharedMesh = mesh;
    }

    private readonly struct ReadOnlyMesh
    {
        private readonly Mesh mesh;

        public ReadOnlyMesh(Mesh mesh)
        {
            this.mesh = mesh;
        }
        public bool IsEmpty => mesh == null;

        public string Name => mesh.name;
        public int BlendshapeCount => mesh.blendShapeCount;

        public string GetBlendShapeName(int index) => mesh.GetBlendShapeName(index);
        public GraphicsBuffer GetBlendShapeBuffer(BlendShapeBufferLayout layout) => mesh.GetBlendShapeBuffer(layout);
        public BlendShapeBufferRange GetBlendShapeBufferRange(int index) => mesh.GetBlendShapeBufferRange(index);
        public float GetBlendShapeFrameWeight(int shapeIndex, int frameIndex) => mesh.GetBlendShapeFrameWeight(shapeIndex, frameIndex);
        public void GetBlendShapeFrameVertices(int shapeIndex, int frameIndex, Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents) => mesh.GetBlendShapeFrameVertices(shapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);

        public Mesh Clone() => Object.Instantiate(mesh);
    }
}