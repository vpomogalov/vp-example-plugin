using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using VP_ExamplePlugin_1.Forms;
using VP_ExamplePlugin_1.Helpers;

namespace VP_ExamplePlugin_1.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class FindRoom : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Получение связанных моделей АР
                List<Document> linkDocs;
                List<RevitLinkInstance> targetLinks;

                try
                {
                    targetLinks = LinksDocument.GetLinks(
                        doc,
                        mustContain: new List<string> { "АР" },
                        mustNotContain: new List<string> { "Фасад", "фасад" });

                    linkDocs = LinksDocument.GetDocuments(
                        doc,
                        mustContain: new List<string> { "АР" },
                        mustNotContain: new List<string> { "Фасад", "фасад" });
                }
                catch (InvalidOperationException ex)
                {
                    TaskDialog.Show("Ошибка", ex.Message);
                    return Result.Failed;
                }

                // Получение помещений, сгруппированных по документам
                Dictionary<Document, List<Room>> roomsByDoc;

                try
                {
                    roomsByDoc = ValidRooms.GetRoomsByDocument(linkDocs);
                }
                catch (Exception ex) when (ex is InvalidOperationException || ex is ArgumentNullException)
                {
                    TaskDialog.Show("Ошибка", ex.Message);
                    return Result.Failed;
                }

                // Получение семейств для обработки
                List<BuiltInCategory> categories = new List<BuiltInCategory>
                {
                    BuiltInCategory.OST_Furniture,
                    BuiltInCategory.OST_MechanicalEquipment
                };

                ElementMulticategoryFilter multiCategoryFilter =
                    new ElementMulticategoryFilter(categories);

                List<FamilyInstance> familyInstances = new FilteredElementCollector(doc)
                    .WherePasses(multiCategoryFilter)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .ToList();

                if (!familyInstances.Any())
                {
                    TaskDialog.Show(
                        "Ошибка",
                        "В модели отсутствуют размещённые экземпляры мебели и оборудования.");
                    return Result.Failed;
                }

                // Проверка наличия параметра "Номер помещения"
                List<FamilyInstance> instancesWithoutParameter = familyInstances
                    .Where(fi =>
                    {
                        Parameter param = fi.LookupParameter("Номер помещения");
                        return param == null || param.IsReadOnly;
                    })
                    .ToList();

                if (instancesWithoutParameter.Any())
                {
                    TaskDialog.Show("Ошибка",
                        $"У {instancesWithoutParameter.Count} экземпляров отсутствует " +
                        "или недоступен для записи параметр \"Номер помещения\".\n\n" +
                        "Добавьте параметр в семейства и повторите попытку.");

                    FailedElementsForm form = new FailedElementsForm(
                        uiDoc,
                        instancesWithoutParameter.Cast<Element>().ToList(),
                        "Экземпляры без параметра \"Номер помещения\"",
                        "Всего экземпляров");

                    form.Show();

                    return Result.Failed;
                }

                // Основная обработка
                List<Element> elementsWithoutRoom = new List<Element>();
                int processedCount = 0;

                using (Transaction trans = new Transaction(doc, "Присвоение номеров помещений"))
                {
                    trans.Start();

                    foreach (FamilyInstance fi in familyInstances)
                    {
                        XYZ point = FamilyInstancePoint.GetPoint(fi);

                        if (point == null)
                        {
                            elementsWithoutRoom.Add(fi);
                            continue;
                        }

                        // Поиск помещения по всем связям
                        Room foundRoom = null;

                        foreach (RevitLinkInstance link in targetLinks)
                        {
                            Document linkDoc = link.GetLinkDocument();

                            if (linkDoc == null || !roomsByDoc.ContainsKey(linkDoc))
                                continue;

                            Transform inverseTransform = link.GetTransform().Inverse;
                            XYZ linkedPoint = inverseTransform.OfPoint(point);

                            foundRoom = roomsByDoc[linkDoc]
                                .FirstOrDefault(r => r.IsPointInRoom(linkedPoint));

                            if (foundRoom != null)
                                break;
                        }

                        if (foundRoom == null)
                        {
                            elementsWithoutRoom.Add(fi);
                            continue;
                        }

                        // Получаем параметр (теперь точно существует)
                        Parameter param = fi.LookupParameter("Номер помещения");

                        // Запись значения
                        string roomNumber = foundRoom.get_Parameter(
                            BuiltInParameter.ROOM_NUMBER)?.AsString() ?? "";
                        string roomName = foundRoom.get_Parameter(
                            BuiltInParameter.ROOM_NAME)?.AsString() ?? "";

                        param.Set(roomNumber + " " + roomName);
                        processedCount++;
                    }

                    trans.Commit();
                }

                // Результат
                if (elementsWithoutRoom.Count > 0)
                {
                    string resultMessage = $"Обработано экземпляров: {processedCount}\n\n" +
                        $"Не удалось определить помещение для {elementsWithoutRoom.Count} экземпляров.";

                    TaskDialog.Show("Результат обработки", resultMessage);

                    FailedElementsForm form = new FailedElementsForm(
                        uiDoc,
                        elementsWithoutRoom,
                        "Не удалось определить положение для следующих экземпляров",
                        "Всего экземпляров");

                    form.Show();
                }
                else
                {
                    TaskDialog.Show(
                        "Готово",
                        $"Все экземпляры успешно обработаны.\n\nОбработано: {processedCount}");
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Критическая ошибка: {ex.Message}";
                TaskDialog.Show("Ошибка", $"Произошла непредвиденная ошибка:\n\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}