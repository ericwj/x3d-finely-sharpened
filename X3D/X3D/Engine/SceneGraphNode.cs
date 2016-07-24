﻿using OpenTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using X3D.Parser;

namespace X3D
{
    public abstract partial class SceneGraphNode
    {

        #region Internal Fields

        internal string __id;
        internal int _id = -1;
        internal bool alreadyWarned = false;

        #endregion

        #region Public Fields

        /// <summary>
        /// The line number and column number the element was parsed from in the XML document.
        /// </summary>
        [XmlIgnore]
        public Vector2 XMLDocumentLocation = new Vector2(-1, -1);

        [XmlAttributeAttribute()]
        public string id
        {
            get
            {
                return this.__id;
            }
            set
            {
                this.__id = value;
            }
        }

        [XmlIgnore]
        public bool PassthroughAllowed = true;

        [XmlIgnore]
        public SceneGraphNode Parent = null;

        [XmlIgnore]
        public List<SceneGraphNode> Parents = new List<SceneGraphNode>();

        [XmlIgnore]
        public List<SceneGraphNode> Children = new List<SceneGraphNode>();

        [XmlIgnore]
        public List<SceneGraphNode> Siblings = new List<SceneGraphNode>();

        [XmlIgnore]
        public bool IsLeaf;

        [XmlIgnore]
        public bool HasRendered = false;

        [XmlIgnore]
        public bool? isValid;

        [XmlIgnore]
        public int Depth { get; set; }

        #endregion

        #region Public Methods

        public string ErrorStringWithLineNumbers()
        {
            if (XMLDocumentLocation == null)
            {
                return this.ToString();
            }

            return string.Format("line ({2},{1})", this.ToString(), XMLDocumentLocation.X, XMLDocumentLocation.Y);
        }

        #endregion

        #region Public Virtuals

        public virtual void Load() { }

        public virtual void PostDeserialization() { Load(); }
        public virtual void PostDescendantDeserialization() { }

        public virtual void PreRenderOnce(RenderingContext rc) { }
        public virtual void PreRender() { }
        public virtual void Render(RenderingContext rc) { }
        public virtual void PostRender(RenderingContext rc) { }

        #endregion

        #region Dynamic Validation

        /// <summary>
        /// Validate relationships in scene at runtime and make adjustments instantly as necessary.
        /// </summary>
        /// <returns>
        /// True, if the types of children of the current node are allowable under this type of SceneGraphNode
        /// Otherwise false, if there were any children found that arent valid relationships.
        /// </returns>
        public bool Validate()
        {
            return X3DRuntimeValidator.Validate(this);
        }

        #endregion

        #region Node Search Methods

        public SGn GetParent<SGn>() where SGn : SceneGraphNode
        {
            return (SGn)AscendantByType<SGn>().FirstOrDefault();
        }

        public SceneGraphNode SearchBFS(string id)
        {
            // Breadth first search for decendant node by type
            Queue<SceneGraphNode> work_items;
            SceneGraphNode node;
            SceneGraphNode result = null;

            work_items = new Queue<SceneGraphNode>();
            work_items.Enqueue(this);

            do
            {
                node = work_items.Dequeue();

                if (node.id == id)
                {
                    result = node;

                    break;
                }

                foreach (SceneGraphNode child in node.Children)
                {
                    work_items.Enqueue(child);
                }
            }
            while (work_items.Count > 0);

            return result;
        }

        public List<SceneGraphNode> DecendantsByType(Type t)
        {
            // Breadth first search for decendant node by type
            Queue<SceneGraphNode> work_items;
            SceneGraphNode node;
            List<SceneGraphNode> lst = new List<SceneGraphNode>();

            work_items = new Queue<SceneGraphNode>();
            work_items.Enqueue(this);

            do
            {
                node = work_items.Dequeue();
                
                if (t.IsInstanceOfType(node))
                {
                    lst.Add(node);
                }

                foreach (SceneGraphNode child in node.Children)
                {
                    work_items.Enqueue(child);
                }
            }
            while (work_items.Count > 0);

            return lst;
        }

        public List<SceneGraphNode> AscendantByType<T>()
        {
            SceneGraphNode parent;
            List<SceneGraphNode> lst = new List<SceneGraphNode>();
            Type t = typeof(T);

            parent = this.Parent;
            
            while (parent != null)
            {
                if (t.IsInstanceOfType(parent))
                {
                    lst.Add(parent);
                }

                parent = parent.Parent;
            }

            return lst;
        }

        #endregion

        #region Scene API Methods

        /// <summary>
        /// Callable through ECMAScript V8 Javascript engine
        /// </summary>
        public SceneGraphNode getElementById(string id)
        {
            //TODO: cache nodes and IDs
            return SearchBFS(id);
        }

        /// <summary>
        /// Callable through ECMAScript V8 Javascript engine
        /// </summary>
        public void setAttribute(string name, object value)
        {

            Type type;
            PropertyInfo propertyInfo;
            FieldInfo fieldInfo;

            type = this.GetType();

            try
            {
                propertyInfo = type.GetProperty(name);

                if(propertyInfo != null)
                {
                    propertyInfo.SetValue(this, Convert.ChangeType(value, propertyInfo.PropertyType), null);

                    return;
                }
            }
            catch (Exception ex) { }

            try
            {
                fieldInfo = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);

                if(fieldInfo!= null)
                {
                    fieldInfo.SetValue(this, Convert.ChangeType(value, fieldInfo.FieldType));

                    return;
                }
            }
            catch (Exception ex) { }

        }

        /// <summary>
        /// Callable through ECMAScript V8 Javascript engine
        /// </summary>
        public object getAttribute(string name)
        {
            Type type;
            PropertyInfo propertyInfo;
            FieldInfo fieldInfo;
            object value = null;

            type = this.GetType();

            try
            {
                propertyInfo = type.GetProperty(name);

                if (propertyInfo != null)
                {
                    value = propertyInfo.GetValue(this, null);

                    return value;
                }
            }
            catch (Exception ex) { }

            try
            {
                fieldInfo = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);

                if (fieldInfo != null)
                {
                    value = fieldInfo.GetValue(this);

                    return value;
                }
            }
            catch (Exception ex) { }

            return null;
        }

        #endregion
    }

}