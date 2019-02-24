using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using FastFluidSolverMT;
using System.Drawing;

/*
 * GHGetPressure.cs
 * Copyright 2017 Christoph Waibel <chwaibel@student.ethz.ch>
 * Written by Erron Estrado <erron_e@hotmail.com> (2019) based on GHVisualizeCp by Christoph Waibel
 * 
 * This work is licensed under the GNU GPL license version 3.
*/

namespace GHWind
{
    public class GHGetPressure : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GHGetPressure class.
        /// </summary>
        public GHGetPressure()
            : base("Get Pressure", "Get Pressure",
                            "Gets Pressure values in Pascals on a given mesh.",
                            "EnergyHubs", "Wind Simulation")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("origin", "origin", "origin", GH_ParamAccess.item);

            pManager.AddMeshParameter("mesh", "mesh", "Mesh surfaces to display Pressure on", GH_ParamAccess.list);

            pManager.AddGenericParameter("DE", "DE", "data extractor, from FFD solver", GH_ParamAccess.item);

            //pManager.AddNumberParameter("ρ", "ρ", "air density in [kg/m^3]. Default is 1.2041", GH_ParamAccess.item);
            //pManager[3].Optional = true;

            //pManager.AddNumberParameter("min", "min", "min for colour scale. default is -1", GH_ParamAccess.item);
            //pManager.AddNumberParameter("max", "max", "max for colour scale. detault is 1", GH_ParamAccess.item);
            //pManager[4].Optional = true;
            //pManager[5].Optional = true;

            pManager.AddIntegerParameter("colour sheme", "colours", "Colour sheme. 0: Blue (min) - Red - Yellow (max); 1: Blue (min) - Green - Red (max); 2: Black only.", GH_ParamAccess.item);
            pManager[3].Optional = true;

            pManager.AddNumberParameter("font size", "font size", "font size", GH_ParamAccess.item);
            pManager[4].Optional = true;

            //pManager.AddPointParameter("ref z", "ref z", "reference height. usually should be the building height. Make sure, the point is somehow in the Y-middle of the domain. Point3d", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("PressureMesh", "PressureMesh", "Coloured Mesh with Pressure values", GH_ParamAccess.item);

            pManager.AddCurveParameter("txt", "txt", "txt", GH_ParamAccess.list);

            pManager.AddGenericParameter("Pressure", "P", "Pressure in Pascals", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Point3d origin = Point3d.Unset;
            if (!DA.GetData(0, ref origin)) { origin = new Point3d(0, 0, 0); }

            List<Mesh> mshP = new List<Mesh>();
            if (!DA.GetDataList(1, mshP)) { return; }

            DataExtractor de = null;
            if (!DA.GetData(2, ref de)) { return; }

            //double roh = 1.2041;
            //DA.GetData(3, ref roh);

            //double min = -1;
            //double max = 1;
            //DA.GetData(4, ref min);
            //DA.GetData(5, ref max);

            int colourSheme = 1;
            DA.GetData(3, ref colourSheme);

            double fontsize = 0.5;
            DA.GetData(4, ref fontsize);

            //Point3d zrefp = new Point3d();
            //if (!DA.GetData(8, ref zrefp)) { return; }

            string face = "Baskerville";
            bool bold = false;
            bool italics = true;


            List<Mesh> mshPOUT = new List<Mesh>();
            Grasshopper.DataTree<double> Ptree = new Grasshopper.DataTree<double>();
            int branch = 0;

            //vref and pdyn should be measured at the building height... 
            //double[] vref = de.get_velocity(0, zrefp[1] - origin[1], zrefp[2] - origin[2]);
            //Vector3d vrefv = new Vector3d(vref[0], vref[1], vref[2]);
            //double vrefl = vrefv.Length;
            //double pref = de.get_pressure(0, zrefp[1] - origin[1], zrefp[2] - origin[2]);
            //double pdyn = 0.5 * roh * Math.Pow(vrefl, 2);

            foreach (Mesh msh in mshP)
            {
                double[] Press = new double[msh.Vertices.Count];
                Color[] Cols = new Color[msh.Vertices.Count];
                Mesh mshcol = new Mesh();
                List<Curve> lst = new List<Curve>();

                for (int u = 0; u < msh.Vertices.Count; u++)
                {
                    //double[] vref = de.get_velocity(0 - origin[0], msh.Vertices[u].Y - origin[1], msh.Vertices[u].Z - origin[2]);
                    //Vector3d vrefv = new Vector3d(vref[0], vref[1], vref[2]);
                    //double vrefl = vrefv.Length;
                    //double pref = de.get_pressure(0 - origin[0], msh.Vertices[u].Y - origin[1], msh.Vertices[u].Z - origin[2]);
                    //double pdyn = 0.5 * roh * Math.Pow(vrefl, 2);
                    Press[u] = de.get_pressure(msh.Vertices[u].X - origin[0], msh.Vertices[u].Y - origin[1], msh.Vertices[u].Z - origin[2]);


                    //Cp[u] = (px - pref) / pdyn;
                    Ptree.Add(Press[u], new Grasshopper.Kernel.Data.GH_Path(branch));
                    //Cols[u] = Utilities.GetRGB(colourSheme, Cp[u], max, min);
                    Cols[u] = Utilities.GetAbsoluteRGB(colourSheme, Press[u]);
                    mshcol.Vertices.Add(msh.Vertices[u]);
                    mshcol.VertexColors.SetColor(u, Cols[u]);

                    //string strval = Math.Round(Cp[u], 2).ToString();
                    string strval = Math.Round(Press[u], 2).ToString();
                    Point3d plp = new Point3d(msh.Vertices[u].X, msh.Vertices[u].Y, msh.Vertices[u].Z);
                    Vector3d vec = new Vector3d(-1, 0, 0);
                    Plane pl = new Plane(plp, vec);

                    var te = Rhino.RhinoDoc.ActiveDoc.Objects.AddText(strval, pl, fontsize, face, bold, italics);
                    Rhino.DocObjects.TextObject txt = Rhino.RhinoDoc.ActiveDoc.Objects.Find(te) as Rhino.DocObjects.TextObject;

                    if (txt != null)
                    {
                        var tt = txt.Geometry as Rhino.Geometry.TextEntity;
                        Curve[] A = tt.Explode();

                        foreach (Curve crv in A)
                        {
                            lst.Add(crv);
                        }

                    }

                    Rhino.RhinoDoc.ActiveDoc.Objects.Delete(te, true);
                }
                branch++;

                for (int j = 0; j < msh.Faces.Count; j++)
                {
                    mshcol.Faces.AddFace(msh.Faces[j].A, msh.Faces[j].B, msh.Faces[j].C, msh.Faces[j].D);
                }


                // output Pressure numbers as text into rhino viewport
                DA.SetDataList(1, lst);

                // output coloured meshes
                mshPOUT.Add(mshcol);
            }

            DA.SetDataTree(2, Ptree);
            DA.SetDataList(0, mshPOUT);



            //THIS IS FROM GIULIO PIACENTINO's page... txtlines component
            //            private void RunScript(string face, bool bold, bool italics, double size, string content, Plane pl, ref object A)
            //{

            //  if(size == 0)
            //    size = 1;

            //  if(!string.IsNullOrEmpty(face) && size > 0 && !string.IsNullOrEmpty(content) &&
            //    pl.IsValid)

            //    var te = RhinoDoc.ActiveDoc.Objects.AddText(content, pl, size, face, bold, italics);
            //    Rhino.DocObjects.TextObject txt = RhinoDoc.ActiveDoc.Objects.Find(te) as Rhino.DocObjects.TextObject;

            //    if(txt != null)
            //    {
            //      var tt = txt.Geometry as Rhino.Geometry.TextEntity;
            //      A = tt.Explode();
            //    }

            //    RhinoDoc.ActiveDoc.Objects.Delete(txt, true);
            //    RhinoDoc.ActiveDoc.Objects.Delete(te, true);
            //  }

            //}





        }




        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return GHWind.Properties.Resources.cp;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{d25c3404-f736-40c9-994f-c72e523f9d96}"); }
        }
    }
}