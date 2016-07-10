﻿//#define DEBUG_SCENE_GRAPH_RENDERING
//#define DEBUG_SCENE_GRAPH_RENDERING_VERBOSE

using System;
using OpenTK.Graphics.OpenGL;

using System.Collections.Generic;
using System.Linq;
using OpenTK;

namespace X3D.Engine
{
    public class Render
    {//:IX3DRenderer {
        //TODO: put all this in X3D.Engine.Renderer
        public static void Scene(SceneManager scene, RenderingContext rc)
        {
#if DEBUG_SCENE_GRAPH_RENDERING
            Console.Clear();
#endif
            Draw(scene.SceneGraph,rc);
        }

        /* There are several ways to perform the rendering..
         * One way is to get the root node of the scene graph and call its rendering methods,
         * then each child X3DNode will need to invoke the rendering methods of its children.
         * This method is simple, but relies heavily on the use of the call stack, and it's algorithm is unchangeable and not relocatable.
         * 
         * Another way is to traverse the scene graph by hand to explicitly call each node's rendering methods.
         * Of course traversing the scene graph by hand can be done recursivily or iterativily, depth first or bredth first.
         * This method allows more control of the algorithm used, is more future proof, 
         * and that is why it is being used here.
         */


        public static void Draw(SceneGraph sg, RenderingContext rc)
        {

            draw_scenegraph_dfs_iterative(sg.GetRoot(),rc);

        }

        private static void draw_scenegraph_dfs_iterative(SceneGraphNode root, RenderingContext context)
        {
            //TODO: make extensive use of GUID lookups from DAG to improve efficiency, instead of copying X3DNodes to and from the stack
            Stack<SceneGraphNode> work_items;
            List<int> nodes_visited;
            IEnumerable<SceneGraphNode> children;
            int num_nodes_visited;
            int num_children_visited;
            SceneGraphNode node;

            nodes_visited = new List<int>(); /* It is required to keep track of all the nodes visited for post rendering 
                                                           * (there is no other way (for dfs algorithm) around this) */
            work_items = new Stack<SceneGraphNode>();

            num_nodes_visited = 0;
            work_items.Push(root);

            while (work_items.Count > 0)
            {
                node = work_items.Peek();
                num_children_visited = 0;

                if (!nodes_visited.Contains(node.id))
                {
                    nodes_visited.Add(node.id);

                    visit(node,context);

                    num_nodes_visited++;
                }
                //if (HasBackEdge(node, work_items) && node.Children.Count > 0)
                //{
                    //TODO: dont like this recursive
                    //draw_scenegraph_dfs_iterative(node.Children[0],context);//,__GetNodeByGUID);
                //}

                if (node.PassthroughAllowed)
                {
                    children = x_xReverse(node.Children);
                    foreach (SceneGraphNode n in children)
                    {
                        if (nodes_visited.Contains(n.id))
                        {
                            num_children_visited++;
                        }
                        else
                        {
                            work_items.Push(n);
                        }
                    }
                }

                if ((node.PassthroughAllowed == false) || (num_children_visited == node.Children.Count))
                {
                    leave(node,context);

                    work_items.Pop();
                }
            }

#if DEBUG_SCENE_GRAPH_RENDERING
            Console.WriteLine("_ visited:"+num_nodes_visited.ToString());
#endif
        }

        private static void visit(SceneGraphNode node, RenderingContext rc)
        {
#if DEBUG_SCENE_GRAPH_RENDERING
#if DEBUG_SCENE_GRAPH_RENDERING_VERBOSE
            Console.WriteLine(node.ToString());
#else
            Console.WriteLine("".PadLeft(node.Depth,'\t')+"<"+node.GetNodeName()+">");
#endif
#endif


            if (!node.HasRendered)
            {
                node.PreRenderOnce(rc);
                node.HasRendered = true;
            }
            node.PreRender();
            node.Render(rc);
        }

        private static void leave(SceneGraphNode node, RenderingContext rc)
        {
#if DEBUG_SCENE_GRAPH_RENDERING
#if DEBUG_SCENE_GRAPH_RENDERING_VERBOSE
            //Console.WriteLine("~"+node.ToString());
#else
            Console.WriteLine("".PadLeft(node.Depth,'\t')+"</"+node.GetNodeName()+">");
#endif
#endif
            node.PostRender(rc);
        }

        private static bool HasBackEdge(SceneGraphNode node, Stack<SceneGraphNode> work_items)
        {
            //This method takes up too much time without my FastStack
            if (node.Parents == null || node.Parents.Count == 0)
            {
                return false;
            }

            /* If the node's parent(s) are still in the work items
               (then we can infer that this node has a back edge to one of its parents) */

            foreach (SceneGraphNode parent in node.Parents)
            {
                if (work_items.Any(n => n.id == parent.id))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// I prefix stupid code with x_x
        /// </summary>
        private static IEnumerable<T> x_xReverse<T>(IEnumerable<T> input)
        {
            return new Stack<T>(input);
        }
    }
}
