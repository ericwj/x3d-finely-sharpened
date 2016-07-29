﻿using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using X3D.Core;
using X3D.Core.Shading;
using X3D.Core.Shading.DefaultUniforms;

namespace X3D
{
    public partial class Shape : X3DShapeNode
    {
        private bool hasShaders;
        private List<X3DShaderNode> shaders;

        [XmlIgnore]
        public List<ComposedShader> ComposedShaders = new List<ComposedShader>();

        #region Test Shader


        [XmlIgnore]
        public bool texturingEnabled;

        [XmlIgnore]
        public int uniformModelview, uniformProjection;

        [XmlIgnore]
        public ShaderUniformsPNCT uniforms = new ShaderUniformsPNCT();

        [XmlIgnore]
        public ShaderMaterialUniforms Materials = new ShaderMaterialUniforms();

        [XmlIgnore]
        public TessShaderUniforms Uniforms = new TessShaderUniforms();

        [XmlIgnore]
        public float TessLevelInner = 137; // 3

        [XmlIgnore]
        public float TessLevelOuter = 115; // 2

        [XmlIgnore]
        public Matrix3 NormalMatrix = Matrix3.Identity;

        [XmlIgnore]
        public ComposedShader CurrentShader = null;

        #endregion

        #region Render Methods

        public void ApplyGeometricTransformations(RenderingContext rc, ComposedShader shader, SceneGraphNode context)
        {
            shader.Use();

            RefreshDefaultUniforms(shader);
            //RefreshMaterialUniforms();

            if (shader.IsTessellator)
                RefreshTessUniforms(shader);


            Matrix4 view = Matrix4.LookAt(new Vector3(4, 3, 3),  // Camera is at (4,3,3), in World Space
                new Vector3(0, 0, 0),  // and looks at the origin
                new Vector3(0, 1, 0) // Head is up (set to 0,-1,0 to look upside-down)
            );

            Matrix4 model; // applied transformation hierarchy

            SceneGraphNode transform_context = context == null ? this : context;

            List<Transform> transformationHierarchy = transform_context.AscendantByType<Transform>().Select(t => (Transform)t).ToList();
            Matrix4 modelview = Matrix4.Identity * rc.matricies.worldview;

            // using Def_Use/Figure02.1Hut.x3d Cone and Cylinder 
            Vector3 x3dScale = new Vector3(0.06f, 0.06f, 0.06f); // scaling down to conform with X3D standard (note this was done manually and might need tweaking)

            //x3dScale = Vector3.One;

            Quaternion modelrotation = Quaternion.Identity;
            Matrix4 modelrotations = Matrix4.Identity;

            foreach (Transform transform in transformationHierarchy)
            {
                modelview *= Matrix4.CreateTranslation(transform.Translation * x3dScale);
                //modelrotation = new Quaternion(transform.Rotation.X, transform.Rotation.Y, transform.Rotation.Z, transform.Rotation.W);
                //modelrotations *= Matrix4.CreateFromQuaternion(modelrotation);

                //modelrotations *= MathHelpers.CreateRotation(ref modelrotation);
            }

            model = modelview;

            Matrix4 cameraTransl = Matrix4.CreateTranslation(rc.cam.Position);

            Quaternion q = rc.cam.Orientation;

            Matrix4 cameraRot;

            cameraRot = Matrix4.CreateFromQuaternion(q); // cameraRot = MathHelpers.CreateRotation(ref q);


            Matrix4 MVP = modelrotations * (model * cameraTransl) * (cameraRot) // position and orient the Shape relative to the world and camera

                ; // this is the MVP matrix


            shader.SetFieldValue("modelview", ref MVP); //GL.UniformMatrix4(uniformModelview, false, ref rc.matricies.modelview);
            shader.SetFieldValue("projection", ref rc.matricies.projection);
            shader.SetFieldValue("camscale", rc.cam.Scale.X); //GL.Uniform1(uniformCameraScale, rc.cam.Scale.X);
            shader.SetFieldValue("X3DScale", rc.matricies.Scale); //GL.Uniform3(uniformX3DScale, rc.matricies.Scale);
            shader.SetFieldValue("coloringEnabled", 0); //GL.Uniform1(uniforms.a_coloringEnabled, 0);
            shader.SetFieldValue("texturingEnabled", this.texturingEnabled ? 1 : 0); //GL.Uniform1(uniforms.a_texturingEnabled, this.texturingEnabled ? 1 : 0);

            if (shader.IsBuiltIn == false)
            {
                shader.ApplyFieldsAsUniforms(rc);
            }
        }

        public void IncludeDefaultShader(string vertexShaderSource, string fragmentShaderSource)
        {
            CurrentShader = ShaderCompiler.ApplyShader(vertexShaderSource, fragmentShaderSource);

            IncludeComposedShader(CurrentShader);
        }

        public void IncludeComposedShader(ComposedShader shader)
        {
            shader.Link();
            shader.Use();

            RefreshDefaultUniforms();
            //RefreshMaterialUniforms();
            if (shader.IsTessellator)
            {
                RefreshTessUniforms();
            }

            ComposedShaders.Add(shader);
        }

        public void IncludeTesselationShaders(string tessControlShaderSource, string tessEvalShaderSource,
                                              string geometryShaderSource)
        {
            CurrentShader = ShaderCompiler.ApplyShader(DefaultShader.vertexShaderSource, 
                                                       DefaultShader.fragmentShaderSource,
                                                       tessControlShaderSource, 
                                                       tessEvalShaderSource, 
                                                       geometryShaderSource);



            IncludeComposedShader(CurrentShader);


        }

        public void RefreshTessUniforms(ComposedShader shader = null)
        {
            if (shader == null) shader = CurrentShader;
            if (CurrentShader.HasErrors) return;

            Uniforms.Modelview = GL.GetUniformLocation(shader.ShaderHandle, "modelview");
            Uniforms.Projection = GL.GetUniformLocation(shader.ShaderHandle, "projection");
            Uniforms.NormalMatrix = GL.GetUniformLocation(shader.ShaderHandle, "normalmatrix");
            Uniforms.LightPosition = GL.GetUniformLocation(shader.ShaderHandle, "LightPosition");
            Uniforms.AmbientMaterial = GL.GetUniformLocation(shader.ShaderHandle, "AmbientMaterial");
            Uniforms.DiffuseMaterial = GL.GetUniformLocation(shader.ShaderHandle, "DiffuseMaterial");
            //Uniforms.TessLevelInner = GL.GetUniformLocation(shader.ShaderHandle, "TessLevelInner");
            //Uniforms.TessLevelOuter = GL.GetUniformLocation(shader.ShaderHandle, "TessLevelOuter");
        }

        public void RefreshDefaultUniforms(ComposedShader shader = null)
        {
            if (shader == null) shader = CurrentShader;
            if (shader.HasErrors) return;

            //uniforms.a_position = GL.GetAttribLocation(shader.ShaderHandle, "position");
            //uniforms.a_normal = GL.GetAttribLocation(shader.ShaderHandle, "normal");
            //uniforms.a_color = GL.GetAttribLocation(shader.ShaderHandle, "color");
            //uniforms.a_texcoord = GL.GetAttribLocation(shader.ShaderHandle, "texcoord");

            uniforms.a_coloringEnabled = GL.GetUniformLocation(shader.ShaderHandle, "coloringEnabled");
            uniforms.a_texturingEnabled = GL.GetUniformLocation(shader.ShaderHandle, "texturingEnabled");
            uniforms.sampler = GL.GetUniformLocation(shader.ShaderHandle, "_MainTex");
        }

        //public void RefreshMaterialUniforms()
        //{
        //    if (CurrentShader.HasErrors) return;
        //
        //    Materials.ambientIntensity = GL.GetUniformLocation(CurrentShader.ShaderHandle, "ambientIntensity");
        //    Materials.diffuseColor = GL.GetUniformLocation(CurrentShader.ShaderHandle, "diffuseColor");
        //    Materials.emissiveColor = GL.GetUniformLocation(CurrentShader.ShaderHandle, "emissiveColor");
        //    Materials.shininess = GL.GetUniformLocation(CurrentShader.ShaderHandle, "shininess");
        //    Materials.specularColor = GL.GetUniformLocation(CurrentShader.ShaderHandle, "specularColor");
        //    Materials.transparency = GL.GetUniformLocation(CurrentShader.ShaderHandle, "transparency");
        //}

        //public void SetSampler(int sampler)
        //{
        //    GL.Uniform1(uniforms.sampler, sampler);
        //}

        public override void Load()
        {
            base.Load();

            var @default = ShaderCompiler.BuildDefaultShader();
            @default.Link();
            @default.Use();
            CurrentShader = @default;
            IncludeComposedShader(@default);

            RefreshDefaultUniforms();
            //RefreshMaterialUniforms();
        }

        public override void PreRender()
        {
            base.PreRender();

            texturingEnabled = GL.IsEnabled(EnableCap.Texture2D);

            shaders = this.DecendantsByType(typeof(X3DShaderNode)).Select(n => (X3DShaderNode)n).ToList();
            hasShaders = shaders.Any();

            
        }

        public override void Render(RenderingContext rc)
        {
            base.Render(rc);

            rc.PushMatricies();

            NormalMatrix = new Matrix3(rc.matricies.modelview); // NormalMatrix = M4GetUpper3x3(ModelviewMatrix);

            var linkedShaders = ComposedShaders.Last(s => s.Linked);

            if (linkedShaders != null)
            {
                CurrentShader = linkedShaders;

                ApplyGeometricTransformations(rc, CurrentShader, this);
            }
        }

        public override void PostRender(RenderingContext rc)
        {
            base.PostRender(rc);

            CurrentShader.Deactivate();
            GL.BindTexture(TextureTarget.Texture2D, 0);

            rc.PopMatricies();
        }

        #endregion
    }
}
