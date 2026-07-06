using Autodesk.Revit.DB;
using System;

namespace VP_ExamplePlugin_1.Helpers
{
    /// <summary>
    /// Предоставляет методы для определения характерной точки экземпляра семейства.
    /// </summary>
    public static class FamilyInstancePoint
    {
        /// <summary>
        /// Возвращает характерную точку экземпляра семейства.
        /// Поиск точки выполняется в следующем порядке:
        /// 1. Точка принадлежности помещению.
        /// 2. Точка вставки экземпляра (LocationPoint).
        /// 3. Средняя точка кривой расположения (LocationCurve).
        /// </summary>
        /// <param name="fi">Экземпляр семейства.</param>
        /// <exception cref="ArgumentNullException">
        /// Возникает, если передан null.
        /// </exception>
        public static XYZ GetPoint(FamilyInstance fi)
        {
            if (fi == null)
            {
                throw new ArgumentNullException(nameof(fi));
            }

            // Точка принадлежности помещению (наивысший приоритет)
            if (fi.HasSpatialElementCalculationPoint)
            {
                try
                {
                    return fi.GetSpatialElementCalculationPoint();
                }
                catch (Autodesk.Revit.Exceptions.ApplicationException)
                {
                    // Не удалось получить SpatialElementCalculationPoint,
                    // переходим к следующему способу
                }
            }

            // Точка вставки экземпляра
            if (fi.Location is LocationPoint locationPoint)
            {
                return locationPoint.Point;
            }

            // Средняя точка кривой расположения
            if (fi.Location is LocationCurve locationCurve)
            {
                return GetCurveMidpoint(locationCurve.Curve);
            }

            // Теоретически недостижимо, но для безопасности
            throw new InvalidOperationException(
                $"Неожиданный тип Location у элемента {fi.Id}: {fi.Location.GetType()}");
        }

        /// <summary>
        /// Возвращает среднюю точку кривой.
        /// </summary>
        /// <param name="curve">Кривая Revit.</param>
        /// <returns>Средняя точка между началом и концом кривой.</returns>
        private static XYZ GetCurveMidpoint(Curve curve)
        {
            XYZ startPoint = curve.GetEndPoint(0);
            XYZ endPoint = curve.GetEndPoint(1);
            return (startPoint + endPoint) * 0.5;
        }
    }
}