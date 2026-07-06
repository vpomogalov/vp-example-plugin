using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace VP_ExamplePlugin_1.Commands
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CreateWorksets : IExternalCommand
    {
        private const int MaxWorksetNameLength = 60;

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Проверка: является ли модель моделью для совместной работы
            if (!doc.IsWorkshared)
            {
                TaskDialog.Show("Ошибка",
                    "В модели не включена функция совместной работы.\n\n" +
                    "Рабочие наборы доступны только в моделях совместной работы.");
                return Result.Failed;
            }

            // Диалог выбора текстового файла
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл с наименованиями рабочих наборов",
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return Result.Cancelled;
            }

            // Чтение рабочих наборов из файла
            string[] worksetNames;
            try
            {
                worksetNames = File.ReadAllLines(openFileDialog.FileName)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToArray();
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Ошибка", $"Не удалось прочитать файл:\n{ex.Message}");
                return Result.Failed;
            }

            if (worksetNames.Length == 0)
            {
                TaskDialog.Show("Ошибка", "Файл не содержит наименований рабочих наборов.");
                return Result.Failed;
            }

            // Проверка на длинные имена
            var tooLongNames = worksetNames
                .Where(name => name.Length > MaxWorksetNameLength)
                .ToList();

            if (tooLongNames.Any())
            {
                TaskDialog.Show("Ошибка",
                    $"Найдены имена длиннее {MaxWorksetNameLength} символов:\n\n" +
                    string.Join("\n", tooLongNames.Select(n => $"• {n}")) +
                    "\n\nСократите имена и повторите попытку.");
                return Result.Failed;
            }

            // Получение существующих рабочих наборов
            var existingWorksetNames = new FilteredWorksetCollector(doc)
                .ToWorksets()
                .Select(w => w.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Фильтрация новых рабочих наборов
            var newWorksets = worksetNames
                .Where(name => !existingWorksetNames.Contains(name))
                .ToList();

            if (newWorksets.Count == 0)
            {
                TaskDialog.Show("Информация",
                    $"Все {worksetNames.Length} рабочих наборов уже существуют в модели.\n\n" +
                    "Новых наборов для создания не найдено.");
                return Result.Cancelled;
            }

            // Создание рабочих наборов
            int createdCount = 0;
            var failedNames = new List<string>();

            using (Transaction trans = new Transaction(doc, "Создание рабочих наборов"))
            {
                trans.Start();

                foreach (string worksetName in newWorksets)
                {
                    try
                    {
                        Workset.Create(doc, worksetName);
                        createdCount++;
                    }
                    catch
                    {
                        failedNames.Add(worksetName);
                    }
                }

                trans.Commit();
            }

            // Итоговое сообщение
            string summary = $"Создано рабочих наборов: {createdCount}\n" +
                            $"Пропущено (уже существуют): {worksetNames.Length - newWorksets.Count}\n" +
                            $"Не удалось создать: {failedNames.Count}";

            if (failedNames.Count > 0)
            {
                string failedList = string.Join("\n", failedNames.Select(n => $"• {n}"));
                TaskDialog.Show("Завершено с ошибками",
                    $"{summary}\n\nНе удалось создать:\n{failedList}");
            }
            else
            {
                TaskDialog.Show("Готово", summary);
            }

            return Result.Succeeded;
        }
    }
}