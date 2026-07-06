using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;

namespace VP_ExamplePlugin_1.Helpers
{
    /// <summary>
    /// Предоставляет методы для получения валидных помещений из связанных моделей.
    /// </summary>
    public static class ValidRooms
    {
        /// <summary>
        /// Возвращает размещённые помещения с ненулевой площадью,
        /// сгруппированные по документам связанных моделей.
        /// </summary>
        /// <param name="linkDocs">
        /// Документы связанных моделей.
        /// </param>
        /// <returns>
        /// Словарь, где ключ - документ связи, значение - список помещений в нём.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Возникает, если linkDocs равен null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Возникает, если не найдено ни одного валидного помещения.
        /// </exception>
        public static Dictionary<Document, List<Room>> GetRoomsByDocument(
            IEnumerable<Document> linkDocs)
        {
            if (linkDocs == null)
            {
                throw new ArgumentNullException(nameof(linkDocs));
            }

            var roomsByDoc = new Dictionary<Document, List<Room>>();

            foreach (var doc in linkDocs)
            {
                if (doc == null)
                {
                    continue;
                }

                var rooms = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .Cast<Room>()
                    .Where(room => room.Location != null)
                    .Where(room =>
                        room.get_Parameter(BuiltInParameter.ROOM_AREA)
                            .AsDouble() > 0)
                    .ToList();

                if (rooms.Any())
                {
                    roomsByDoc[doc] = rooms;
                }
            }

            if (!roomsByDoc.Any())
            {
                throw new InvalidOperationException(
                    "В связанных моделях не найдены размещённые помещения " +
                    "с ненулевой площадью.");
            }

            return roomsByDoc;
        }
    }
}