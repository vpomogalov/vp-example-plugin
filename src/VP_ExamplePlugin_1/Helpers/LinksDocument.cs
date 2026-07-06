using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace VP_ExamplePlugin_1.Helpers
{
    /// <summary>
    /// Предоставляет методы для поиска связанных документов по критериям.
    /// </summary>
    internal static class LinksDocument
    {
        /// <summary>
        /// Возвращает документы связанных моделей, удовлетворяющих условиям поиска.
        /// </summary>
        /// <param name="doc">Текущий документ.</param>
        /// <param name="mustContain">
        /// Список подстрок, которые должны содержаться в имени связи.
        /// Если null или пустой - принимаются все имена.
        /// </param>
        /// <param name="mustNotContain">
        /// Список подстрок, которые не должны содержаться в имени связи.
        /// Если null - исключения не применяются.
        /// </param>
        /// <returns>Список документов загруженных связей.</returns>
        /// <exception cref="ArgumentNullException">
        /// Возникает, если doc равен null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Возникает, если связи не найдены или не загружены.
        /// </exception>
        public static List<Document> GetDocuments(
            Document doc,
            List<string> mustContain = null,
            List<string> mustNotContain = null)
        {
            List<RevitLinkInstance> links = GetLinks(doc, mustContain, mustNotContain);

            var documents = new List<Document>();

            foreach (var link in links)
            {
                Document linkDoc = link.GetLinkDocument();

                if (linkDoc == null)
                {
                    throw new InvalidOperationException(
                        $"Связь \"{link.Name}\" не загружена или находится в закрытом РН.\n\n" +
                        "1. Откройте РН\n" +
                        "2. Загрузите связь\n" +
                        "3. Повторите попытку");
                }

                documents.Add(linkDoc);
            }

            return documents;
        }

        /// <summary>
        /// Возвращает экземпляры связей, удовлетворяющих условиям поиска.
        /// </summary>
        /// <param name="doc">Текущий документ.</param>
        /// <param name="mustContain">
        /// Список подстрок, которые должны содержаться в имени связи.
        /// Если null или пустой - принимаются все имена.
        /// </param>
        /// <param name="mustNotContain">
        /// Список подстрок, которые не должны содержаться в имени связи.
        /// Если null - исключения не применяются.
        /// </param>
        /// <returns>Список найденных экземпляров связей.</returns>
        /// <exception cref="ArgumentNullException">
        /// Возникает, если doc равен null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Возникает, если связи не найдены.
        /// </exception>
        public static List<RevitLinkInstance> GetLinks(
            Document doc,
            List<string> mustContain = null,
            List<string> mustNotContain = null)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            mustContain = mustContain ?? new List<string>();
            mustNotContain = mustNotContain ?? new List<string>();

            var links = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>()
                .Where(link => IsMatch(link, mustContain, mustNotContain))
                .ToList();

            if (!links.Any())
            {
                throw new InvalidOperationException(
                    "В модели не найдены связи, удовлетворяющие условиям поиска");
            }

            return links;
        }

        private static bool IsMatch(
            RevitLinkInstance link,
            List<string> mustContain,
            List<string> mustNotContain)
        {
            string name = link.Name;

            bool containsRequired = mustContain.Count == 0 ||
                mustContain.Any(s => name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);

            if (!containsRequired)
                return false;

            bool containsForbidden = mustNotContain.Any(s =>
                name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0);

            return !containsForbidden;
        }
    }
}