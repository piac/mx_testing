﻿using NUnit.Framework;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace RhinoCommonDelayed
{
    public class MeshIntersectImplementation
        : MeasuredImplementationBase
    {
        static MeshIntersectImplementation() { Instance = new MeshIntersectImplementation(); }
        private MeshIntersectImplementation() { }
        public static MeshIntersectImplementation Instance { get; private set; }

        const string incipitString = "MEASURED INTERSECTIONS";

        public void Model(string filepath)
        {
            ParseAndExecuteNotes(filepath, incipitString);
        }

        internal override bool OperateCommand(IEnumerable<Mesh> all_meshes, double final_tolerance, out List<ResultMetrics> result_ordered, out string log_text)
        {
            Polyline[] intersections;
            Polyline[] overlaps;
            bool rc;

            using (TextLog log = new TextLog())
            {
                rc = Intersection.MeshMesh(all_meshes, final_tolerance,
                    out intersections, true, out overlaps, false, out _, log,
                    System.Threading.CancellationToken.None, null);
                log_text = log.ToString();
            }

            result_ordered = null;
            var results = intersections != null ? intersections.Select(a => new ResultMetrics { Closed = a.IsClosed, Measurement = a.Length, Overlap = false, Polyline = a }) : Array.Empty<ResultMetrics>();
            if (overlaps != null) results = results.Concat(overlaps.Select(a => new ResultMetrics { Closed = a.IsClosed, Measurement = a.Length, Overlap = true, Polyline = a }));
            result_ordered = results.OrderBy(a => a.Measurement).ToList();

            return rc;
        }

        internal override void CheckAssertions(List<ResultMetrics> expected, List<ResultMetrics> result_ordered, bool rv, string log_text)
        {
            Assert.IsTrue(rv, "Return value of Intersection.MeshMesh function");
            Assert.IsEmpty(log_text, "Textlog of function must be empty");

            Assert.AreEqual(expected.Count, result_ordered.Count, "The amount of expected resulting intersections was different.");

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i].Measurement, result_ordered[i].Measurement, 0.002);
                if (expected[i].Closed.HasValue) Assert.AreEqual(expected[i].Closed.Value, result_ordered[i].Closed.Value,
                    $"Curve of length {expected[i].Measurement} was not {(expected[i].Closed.Value ? "closed" : "open")} as expected.");
                if (expected[i].Overlap.HasValue) Assert.AreEqual(expected[i].Overlap.Value, result_ordered[i].Overlap.Value,
                    $"Curve of length {expected[i].Measurement} was not {(expected[i].Overlap.Value ? "ovelapping" : "perforating")} as expected.");
            }
        }
    }
}
