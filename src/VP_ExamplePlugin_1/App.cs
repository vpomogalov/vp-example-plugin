using System.Reflection;
using Autodesk.Revit.UI;
using System.Windows.Media.Imaging;

namespace VP_ExamplePlugin_1
{
    internal class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            application.CreateRibbonTab("VP_ExamplePlugin");
            RibbonPanel panelTH = application.CreateRibbonPanel("VP_ExamplePlugin", "ТХ");
            RibbonPanel panelBIM = application.CreateRibbonPanel("VP_ExamplePlugin", "BIM");


            // Кнопка "Записать помещения"
            PushButtonData buttonFindRoom = new PushButtonData(
                "FindRoom",
                "Записать\nпомещения",
                assemblyPath,
                "VP_ExamplePlugin_1.Commands.FindRoom"
            );

            // Кнопка "Создать рабочие наборы"
            PushButtonData buttonCreateWorksets = new PushButtonData(
                "CreateWorksets",
                "Создать\nРН",
                assemblyPath,
                "VP_ExamplePlugin_1.Commands.CreateWorksets"
            );

            // Загружаем иконки
            string iconsFolder = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(assemblyPath), "Icons");

            // Иконка "Записать помещения"
            string iconPathFindRoom32 = System.IO.Path.Combine(iconsFolder, "FindRoom_32.png");
            string iconPathFindRoom16 = System.IO.Path.Combine(iconsFolder, "FindRoom_16.png");

            if (System.IO.File.Exists(iconPathFindRoom32))
            {
                buttonFindRoom.LargeImage = new BitmapImage(new System.Uri(iconPathFindRoom32));
            }
            if (System.IO.File.Exists(iconPathFindRoom16))
            {
                buttonFindRoom.Image = new BitmapImage(new System.Uri(iconPathFindRoom16));
            }

            // Иконка "Создать рабочие наборы"
            string iconPathCreateWorksets32 = System.IO.Path.Combine(iconsFolder, "CreateWorksets_32.png");
            string iconPathCreateWorksets16 = System.IO.Path.Combine(iconsFolder, "CreateWorksets_16.png");

            if (System.IO.File.Exists(iconPathCreateWorksets32))
            {
                buttonCreateWorksets.LargeImage = new BitmapImage(new System.Uri(iconPathCreateWorksets32));
            }
            if (System.IO.File.Exists(iconPathCreateWorksets16))
            {
                buttonCreateWorksets.Image = new BitmapImage(new System.Uri(iconPathCreateWorksets16));
            }

            // Добавляем на панель кнопку Записать помещения (Технологические решения)
            PushButton buttonFindRoomToPanel = panelTH.AddItem(buttonFindRoom) as PushButton;

            if (buttonFindRoomToPanel != null)
            {
                buttonFindRoomToPanel.ToolTip = "Определяет местоположение оборудования и мебели по помещениям из связанных моделей АР.";
                buttonFindRoomToPanel.LongDescription = "Команда заполняет параметр экземпляров " +
                    "\"Номер помещения\" для категорий оборудование и мебель.\n\nПеред использованием проверьте " +
                    "подружена ли актуальная связь АР.";
            }

            // Добавляем на панель кнопку Создать РН (BIM)
            PushButton buttonCreateWorksetsToPanel = panelBIM.AddItem(buttonCreateWorksets) as PushButton;

            if (buttonCreateWorksetsToPanel != null)
            {
                buttonCreateWorksetsToPanel.ToolTip = "Создать РН по выбранному .txt файлу";
                buttonCreateWorksetsToPanel.LongDescription = "Файл .txt должен содержать названия рабочих наборов.\n" +
                    "Название РН начинается с новой строки.";
            }

            return Result.Succeeded;
        }
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }
    }
}
