using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
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


            transaction.Commit();

            return Result.Succeeded;
        }
    }
}
