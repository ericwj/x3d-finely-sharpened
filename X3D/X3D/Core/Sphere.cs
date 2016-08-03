﻿// TODO: calculate texcoords using spherical equation in shader

using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL4;
using X3D.Core;
using OpenTK.Input;
using X3D.Core.Shading.DefaultUniforms;
using X3D.Core.Shading;
using X3D.Parser;

namespace X3D
{
    public partial class Sphere
    {
        //private int vbo, NumVerticies;
        //private int vbo4, NumVerticies4; // not used
        //private readonly float tessLevelInner = 137; // 3
        //private readonly float tessLevelOuter = 115; // 2
        internal PackedGeometry _pack;

        private Shape parentShape;

        public override void CollectGeometry(
                            RenderingContext rc,
                            out GeometryHandle handle,
                            out BoundingBox bbox,
                            out bool coloring,
                            out bool texturing)
        {
            handle = GeometryHandle.Zero;
            bbox = BoundingBox.Zero;
            coloring = false;
            texturing = false;

            parentShape = GetParent<Shape>();

            _pack = new PackedGeometry();
            _pack.restartIndex = -1;
            _pack._indices = Faces;
            _pack._coords = Verts;
            _pack.bbox = BoundingBox.CalculateBoundingBox(Verts);
            
            _pack.Interleave();

            // BUFFER GEOMETRY
            handle = Buffering.BufferShaderGeometry(_pack);
        }

        #region Rendering Methods

        //public override void Load()
        //{
        //    base.Load();

        //    parentShape = GetParent<Shape>();

        //    // should be able to access shader here

        //    if (parentShape != null)
        //    {
        //        // TESSELLATION
        //        parentShape.IncludeTesselationShaders(TriangleTessShader.tessControlShader, 
        //                                              TriangleTessShader.tessEvalShader, 
        //                                              TriangleTessShader.geometryShaderSource);
        //    }

            

        //    Buffering.Interleave(null, out vbo, out NumVerticies,
        //        out vbo4, out NumVerticies4,
        //        Faces, null, Verts, null, null, null, null,
        //        restartIndex: -1, genTexCoordPerVertex: true);
        //}

        //public override void PreRenderOnce(RenderingContext rc)
        //{
        //    base.PreRenderOnce(rc);
        //}

        //public override void Render(RenderingContext rc)
        //{
        //    base.Render(rc);

        //    var size = new Vector3(1, 1, 1);
        //    //var scale = new Vector3(1, 1, 1);
        //    var scale = new Vector3(0.04f, 0.04f, 0.04f);

        //    // Refactor tessellation 

        //    if (parentShape.ComposedShaders.Any(s => s.Linked))
        //    {
        //        if (parentShape.CurrentShader != null)
        //        {
        //            parentShape.CurrentShader.Use();

        //            parentShape.CurrentShader.SetFieldValue("size", size);
        //            parentShape.CurrentShader.SetFieldValue("scale", scale);

        //            parentShape.CurrentShader.SetFieldValue("spherical", 1);

        //            if (parentShape.CurrentShader.IsTessellator)
        //            {
        //                Vector4 lightPosition = new Vector4(0.25f, 0.25f, 1f, 0f);

        //                if (parentShape.CurrentShader.IsBuiltIn)
        //                {
        //                    // its a built in system shader so we are using the the fixed variable inbuilt tesselator
        //                    parentShape.CurrentShader.SetFieldValue("TessLevelInner", this.tessLevelInner);
        //                    parentShape.CurrentShader.SetFieldValue("TessLevelOuter", this.tessLevelOuter);
        //                }
        //                else
        //                {
        //                    //parentShape.CurrentShader.SetFieldValue("TessLevelInner", parentShape.TessLevelInner);
        //                    //parentShape.CurrentShader.SetFieldValue("TessLevelOuter", parentShape.TessLevelOuter);
        //                }

        //                parentShape.CurrentShader.SetFieldValue("normalmatrix", ref parentShape.NormalMatrix);
        //                //GL.UniformMatrix3(parentShape.Uniforms.NormalMatrix, false, ref parentShape.NormalMatrix);
        //                GL.Uniform3(parentShape.Uniforms.LightPosition, 1, ref lightPosition.X);
        //                GL.Uniform3(parentShape.Uniforms.AmbientMaterial, X3DTypeConverters.ToVec3(OpenTK.Graphics.Color4.Aqua)); // 0.04f, 0.04f, 0.04f
        //                GL.Uniform3(parentShape.Uniforms.DiffuseMaterial, 0.0f, 0.75f, 0.75f);


        //                GL.PatchParameter(PatchParameterInt.PatchVertices, 3);
        //                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        //                Buffering.ApplyBufferPointers(parentShape.CurrentShader);
        //                //Buffering.ApplyBufferPointers(parentShape.uniforms);
        //                GL.DrawArrays(PrimitiveType.Patches, 0, NumVerticies);
        //            }
        //            else
        //            {
        //                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        //                Buffering.ApplyBufferPointers(parentShape.CurrentShader);
        //                //Buffering.ApplyBufferPointers(parentShape.uniforms);
        //                GL.DrawArrays(PrimitiveType.Triangles, 0, NumVerticies);
        //            }
        //        }
        //    }
        //}

        #endregion

        #region Icosahedron Geometry

        int[] Faces = new int[] 
        {
            2, 1, 0, -1, 
            3, 2, 0, -1,
            4, 3, 0, -1,
            5, 4, 0, -1,
            1, 5, 0, -1,
            11, 6,  7, -1,
            11, 7,  8, -1,
            11, 8,  9, -1,
            11, 9,  10, -1,
            11, 10, 6, -1,
            1, 2, 6, -1,
            2, 3, 7, -1,
            3, 4, 8, -1,
            4, 5, 9, -1,
            5, 1, 10, -1,
            2,  7, 6, -1,
            3,  8, 7, -1,
            4,  9, 8, -1,
            5, 10, 9, -1,
            1, 6, 10, -1,
        };
 
        //TODO: scale values correctly
        Vector3[] Verts = new Vector3[] 
        {
             new Vector3(0.000f,  0.000f,  1.000f),
             new Vector3(0.894f,  0.000f,  0.447f),
             new Vector3(0.276f,  0.851f,  0.447f),
            new Vector3(-0.724f,  0.526f,  0.447f),
            new Vector3(-0.724f, -0.526f,  0.447f),
             new Vector3(0.276f, -0.851f,  0.447f),
             new Vector3(0.724f,  0.526f, -0.447f),
            new Vector3(-0.276f,  0.851f, -0.447f),
            new Vector3(-0.894f,  0.000f, -0.447f),
            new Vector3(-0.276f, -0.851f, -0.447f),
             new Vector3(0.724f, -0.526f, -0.447f),
             new Vector3(0.000f,  0.000f, -1.000f)
        };
        #endregion
    }
}
