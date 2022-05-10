using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreationModelPlugin
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CopyGroup : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            return CreateWalls(doc,10000,5000);

            //var res1 = new FilteredElementCollector(doc)
            //    .OfClass(typeof(WallType))
            //    //.Cast<Wall>()
            //    .OfType<WallType>()
            //    .ToList();

            //var res2 = new FilteredElementCollector(doc)
            //   .OfClass(typeof(FamilyInstance))
            //   .OfCategory(BuiltInCategory.OST_Doors)
            //   //.Cast<Wall>()
            //   .OfType<FamilyInstance>()
            //   .Where(x=>x.Name.Equals("0915 x 2134 мм"))
            //   .ToList();


        }

        private Result CreateWalls(Document doc,  double width_mm, double depth_mm, string top= "Уровень 2", string bottom = "Уровень 1")
        {

            List<Level> levels = new FilteredElementCollector(doc)
                 .OfClass(typeof(Level))
                 .OfType<Level>()
                 .ToList();

            Level level1 = levels
                .Where(x => x.Name.Equals(bottom))
                .FirstOrDefault();

            Level level2 = levels
               .Where(x => x.Name.Equals(top))
               .FirstOrDefault();

            double width = UnitUtils.ConvertToInternalUnits(width_mm, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(depth_mm, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction transaction = new Transaction(doc);
            transaction.Start("Создание стен");

            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }

            AddDoor(doc, level1,walls[0]);
            AddWinwdows(doc, level1,walls);
            AddRoof(doc, level2, walls);
            transaction.Commit();

            return Result.Succeeded;
        }

        private void AddRoof(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                  .OfClass(typeof(RoofType))
                  .OfType<RoofType>()
                  .Where(x => x.Name.Equals("Типовой - 400мм"))
                  .Where(x => x.FamilyName.Equals("Базовая крыша"))
                  .FirstOrDefault();

            //double wallWidth = walls[0].Width;
            //double dt = wallWidth / 2;

            //List<XYZ> points = new List<XYZ>();
            //points.Add(new XYZ(-dt, -dt, 0));
            //points.Add(new XYZ(dt, -dt, 0));
            //points.Add(new XYZ(dt, dt, 0));
            //points.Add(new XYZ(-dt, dt, 0));
            //points.Add(new XYZ(-dt, -dt, 0));


            //var application = doc.Application;
            //CurveArray footprint = application.Create.NewCurveArray();
            //for (int i = 0; i < 4; i++)
            //{
            //    LocationCurve curve = walls[i].Location as LocationCurve;
            //    //footprint.Append(curve.Curve);
            //    XYZ p1 = curve.Curve.GetEndPoint(0);
            //    XYZ p2 = curve.Curve.GetEndPoint(1);
            //    Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
            //    footprint.Append(line);
            //}


            //CurveArray footprint1 = application.Create.NewCurveArray();
            //for (int i = 0; i < 2; i++)
            //{
            //    if (i == 1)
            //        i++;
            //    LocationCurve curve = walls[i].Location as LocationCurve;
            //    //footprint.Append(curve.Curve);
            //    XYZ p1 = curve.Curve.GetEndPoint(0);
            //    XYZ p2 = curve.Curve.GetEndPoint(1);
            //    Line line = Line.CreateBound(p1 + points[i], p2 + points[i + 1]);
            //    footprint1.Append(line);
            //}

            //ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            //FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, out footPrintToModelCurveMapping);

            //foreach (ModelCurve m in footPrintToModelCurveMapping)
            //{
            //    footprintRoof.set_DefinesSlope(m, true);
            //    footprintRoof.set_SlopeAngle(m, 0.5);
            //}

            double depth = walls[0].Width*2;
            LocationCurve curve1 = walls[0].Location as LocationCurve;
            double length = curve1.Curve.Length;
            double halflength = length / 2;
            CurveArray curveArray = new CurveArray();
 
            curveArray.Append(Line.CreateBound(new XYZ (-halflength-3, -halflength/2 - depth, halflength-2), new XYZ(-halflength-3, 0, halflength+4)));
            curveArray.Append(Line.CreateBound(new XYZ(-halflength-3, 0, halflength+4), new XYZ(-halflength-3, halflength/2 + depth, halflength-2)));

            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(-halflength - depth, 0, 0), new XYZ(-halflength - depth, 0, halflength), new XYZ(0, halflength, 0), doc.ActiveView);
 
             doc.Create.NewExtrusionRoof(curveArray, plane, level2, roofType, 0, length + 2* depth);

        }

        private void AddWinwdows(Document doc, Level level1, List<Wall> walls)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 1830 мм"))
                .Where(x => x.FamilyName.Equals("Фиксированные"))
                .FirstOrDefault();


            for (int i = 1; i < 4; i++)
            {
                LocationCurve hostCurve = walls[i].Location as LocationCurve;
                XYZ point1 = hostCurve.Curve.GetEndPoint(0);
                XYZ point2 = hostCurve.Curve.GetEndPoint(1);
                XYZ point = (point1 + point2) / 2;
                if (!windowType.IsActive)
                    windowType.Activate();
                var window = doc.Create.NewFamilyInstance(point, windowType, walls[i], level1, StructuralType.NonStructural);
                window.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM).Set(UnitUtils.ConvertToInternalUnits(800, UnitTypeId.Millimeters));
            }
        }

        private void AddDoor(Document doc, Level level1, Wall wall)
        {
          FamilySymbol doorType = new FilteredElementCollector(doc)
                 .OfClass(typeof(FamilySymbol))
                 .OfCategory(BuiltInCategory.OST_Doors)
                 .OfType<FamilySymbol>()
                 .Where(x => x.Name.Equals("0915 x 2134 мм"))
                 .Where(x=> x.FamilyName.Equals("Одиночные-Щитовые"))
                 .FirstOrDefault();

           LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;
            if (!doorType.IsActive)
                doorType.Activate();
            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);

        }
    }
}
