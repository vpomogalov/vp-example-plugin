using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace VP_ExamplePlugin_1.Forms
{
    /// <summary>
    /// Окно для отображения элементов
    /// </summary>
    public class FailedElementsForm : System.Windows.Forms.Form
    {
        private readonly UIDocument _uiDoc;
        private readonly List<Element> _elements;
        private readonly string _formTitle;
        private readonly string _headerPrefix;

        private ListView listViewElements;
        private Label lblHeader;
        private Button btnClose;
        private Button btnExport;
        private System.Windows.Forms.Panel topPanel;
        private System.Windows.Forms.Panel bottomPanel;

        public FailedElementsForm(
            UIDocument uiDoc,
            List<Element> elements,
            string formTitle,
            string headerPrefix)
        {
            _uiDoc = uiDoc;
            _elements = elements;
            _formTitle = formTitle;
            _headerPrefix = headerPrefix;

            InitializeComponent();
            FillList();
        }

        private void InitializeComponent()
        {
            // Верхняя панель с заголовком
            topPanel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = System.Drawing.Color.FromArgb(240, 240, 240),
                Padding = new Padding(12, 8, 12, 8)
            };

            lblHeader = new Label
            {
                Text = $"{_headerPrefix}: {_elements.Count}",
                Font = new Font("Microsoft Sans Serif", 8.5F, FontStyle.Bold),
                ForeColor = System.Drawing.Color.FromArgb(30, 30, 30),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            topPanel.Controls.Add(lblHeader);

            // Таблица элементов
            listViewElements = new ListView
            {
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft Sans Serif", 8.5F),
                View = System.Windows.Forms.View.Details,
                FullRowSelect = true,
                MultiSelect = true,
                HideSelection = false,
                GridLines = true,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                OwnerDraw = true
            };

            // Колонки таблицы
            listViewElements.Columns.Add("Категория", 140);
            listViewElements.Columns.Add("Наименование", 350);
            listViewElements.Columns.Add("Уровень", 120);
            listViewElements.Columns.Add("ID", 40);

            // Подписка на события
            listViewElements.DrawColumnHeader += ListViewElements_DrawColumnHeader;
            listViewElements.DrawSubItem += ListViewElements_DrawSubItem;
            listViewElements.DoubleClick += ListViewElements_DoubleClick;
            listViewElements.KeyDown += ListViewElements_KeyDown;

            // Нижняя панель с кнопкой
            bottomPanel = new System.Windows.Forms.Panel
            {
                Dock = DockStyle.Bottom,
                Height = 40,
                BackColor = System.Drawing.Color.FromArgb(240, 240, 240),
                Padding = new Padding(12, 6, 12, 6)
            };

            btnClose = new Button
            {
                Text = "Закрыть",
                Font = new Font("Microsoft Sans Serif", 8.25F),
                Size = new Size(80, 26),
                FlatStyle = FlatStyle.System,
                Dock = DockStyle.Right
            };
            btnClose.Click += (s, e) => this.Close();

            btnExport = new Button
            {
                Text = "Экспорт CSV",
                Font = new Font("Microsoft Sans Serif", 8.25F),
                Size = new Size(110, 26),
                FlatStyle = FlatStyle.System,
                Dock = DockStyle.Right
            };

            btnExport.Click += BtnExport_Click;

            bottomPanel.Controls.Add(btnExport);
            bottomPanel.Controls.Add(btnClose);

            // Настройки формы
            this.Text = _formTitle;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Size = new Size(700, 520);
            this.MinimizeBox = false;
            this.MaximizeBox = true;
            this.BackColor = System.Drawing.Color.White;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            try
            {
                string revitExePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                this.Icon = System.Drawing.Icon.ExtractAssociatedIcon(revitExePath);
            }
            catch
            {
                this.Icon = System.Drawing.SystemIcons.Information;
            }

            this.Controls.Add(listViewElements);
            this.Controls.Add(topPanel);
            this.Controls.Add(bottomPanel);

            // При изменении размера окна перераспределяем ширину колонок
            this.Resize += (s, e) => ResizeColumns();

            // Принудительно корректируем ширину колонок после полной загрузки формы
            this.Shown += (s, e) => ResizeColumns();
        }

        /// <summary>
        /// Заполнение таблицы элементами
        /// </summary>
        private void FillList()
        {
            listViewElements.BeginUpdate();

            foreach (Element elem in _elements)
            {
                // Категория
                string categoryName = elem.Category != null ? elem.Category.Name : "Не удалось определить категорию";

                // Имя элемента
                string elementName = elem.Name;

                // Уровень: пробуем FAMILY_LEVEL_PARAM -> LEVEL_NAME -> LevelId
                string levelName = "";
                Parameter levelParam = elem.get_Parameter(BuiltInParameter.FAMILY_LEVEL_PARAM);
                if (levelParam != null && levelParam.HasValue)
                {
                    levelName = levelParam.AsValueString();
                }

                // ID элемента
                string id = elem.Id.IntegerValue.ToString();

                // Создаём строку таблицы
                ListViewItem item = new ListViewItem(categoryName);
                item.SubItems.Add(elementName);
                item.SubItems.Add(levelName);
                item.SubItems.Add(id);

                item.Tag = elem;

                listViewElements.Items.Add(item);
            }

            // Пропорционально распределяем ширину колонок
            ResizeColumns();

            listViewElements.EndUpdate();
        }

        /// <summary>
        /// Пропорциональное распределение ширины колонок: 23% | 42% | 18% | 17%
        /// </summary>
        private void ResizeColumns()
        {
            if (listViewElements.Columns.Count == 0)
            {
                return;
            }

            int totalWidth = listViewElements.ClientSize.Width - 4;

            double[] ratios = { 0.23, 0.42, 0.18, 0.17 };
            double totalRatio = 0;

            // Считаем сумму всех соотношений
            foreach (double ratio in ratios)
            {
                totalRatio += ratio;
            }

            // Распределяем ширину с нормализацией
            double scale = totalWidth / totalRatio;

            for (int i = 0; i < listViewElements.Columns.Count; i++)
            {
                int colWidth = (int)(ratios[i] * scale);
                if (colWidth < 40)
                {
                    colWidth = 40;
                }
                listViewElements.Columns[i].Width = colWidth;
            }

            // Корректируем последнюю колонку, чтобы точно заполнить всё пространство
            int usedWidth = 0;
            for (int i = 0; i < listViewElements.Columns.Count - 1; i++)
            {
                usedWidth += listViewElements.Columns[i].Width;
            }

            int lastColWidth = totalWidth - usedWidth;
            if (lastColWidth >= 40)
            {
                listViewElements.Columns[listViewElements.Columns.Count - 1].Width = lastColWidth;
            }
        }

        /// <summary>
        /// Отрисовка заголовков колонок
        /// </summary>
        private void ListViewElements_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            // Фон 
            using (SolidBrush headerBrush = new SolidBrush(System.Drawing.Color.FromArgb(30, 135, 241)))
            {
                e.Graphics.FillRectangle(headerBrush, e.Bounds);
            }

            // Рамка
            using (Pen borderPen = new Pen(System.Drawing.Color.FromArgb(0, 0, 0)))
            {
                e.Graphics.DrawRectangle(borderPen, e.Bounds);
            }

            // Текст по центру
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;

            TextRenderer.DrawText(
                e.Graphics,
                e.Header.Text,
                new Font("Microsoft Sans Serif", 8.5F, FontStyle.Bold),
                e.Bounds,
                System.Drawing.Color.FromArgb(255, 255, 255),
                flags);
        }

        /// <summary>
        /// Отрисовка ячеек таблицы (чередование строк, выравнивание)
        /// </summary>
        private void ListViewElements_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            // Чередование фона строк
            System.Drawing.Color bgColor = e.ItemIndex % 2 == 0
                ? System.Drawing.Color.White
                : System.Drawing.Color.FromArgb(240, 240, 240);

            // Подсветка выделенной строки
            if (e.Item.Selected)
            {
                bgColor = System.Drawing.Color.FromArgb(205, 232, 255);
            }

            using (SolidBrush bgBrush = new SolidBrush(bgColor))
            {
                e.Graphics.FillRectangle(bgBrush, e.Bounds);
            }

            // Выравнивание: Уровень и ID — по центру, остальные — влево
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

            if (e.ColumnIndex == 1)
            {
                flags |= TextFormatFlags.Left;
            }
            else
            {
                flags |= TextFormatFlags.HorizontalCenter;
            }

            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem.Text,
                new Font("Microsoft Sans Serif", 8.5F),
                e.Bounds,
                System.Drawing.Color.FromArgb(30, 30, 30),
                flags);
        }

        /// <summary>
        /// Двойной клик — показать элемент в Revit
        /// </summary>
        private void ListViewElements_DoubleClick(object sender, EventArgs e)
        {
            if (listViewElements.SelectedItems.Count == 0) return;

            ListViewItem selectedItem = listViewElements.SelectedItems[0];
            Element element = selectedItem.Tag as Element;

            if (element != null)
                SelectAndShowElement(element.Id);
        }

        /// <summary>
        /// Enter — показать выбранный элемент в Revit
        /// </summary>
        private void ListViewElements_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (listViewElements.SelectedItems.Count == 0) return;

                ListViewItem selectedItem = listViewElements.SelectedItems[0];
                Element element = selectedItem.Tag as Element;

                if (element != null)
                    SelectAndShowElement(element.Id);
            }
        }

        /// <summary>
        /// Выделить и показать элемент в Revit
        /// </summary>
        private void SelectAndShowElement(ElementId elementId)
        {
            try
            {
                _uiDoc.Selection.SetElementIds(
                    new List<ElementId> { elementId });

                _uiDoc.ShowElements(elementId);
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show("Ошибка", ex.Message);
            }
        }

        /// <summary>
        /// Экспорт таблицы в CSV
        /// </summary>
        private void BtnExport_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "CSV (*.csv)|*.csv";
                    string projectName = Path.GetFileNameWithoutExtension(_uiDoc.Document.Title);

                    string currentDate = DateTime.Now.ToString("dd.MM.yyyy");

                    saveDialog.FileName =
                        $"{projectName}_{currentDate}.csv";


                    if (saveDialog.ShowDialog() != DialogResult.OK)
                    {
                        return;
                    }

                    StringBuilder sb = new StringBuilder();

                    // Заголовки
                    sb.AppendLine("Категория;Наименование;Уровень;ID");

                    // Данные
                    foreach (ListViewItem item in listViewElements.Items)
                    {
                        string category = EscapeCsv(item.SubItems[0].Text);
                        string name = EscapeCsv(item.SubItems[1].Text);
                        string level = EscapeCsv(item.SubItems[2].Text);
                        string id = EscapeCsv(item.SubItems[3].Text);

                        sb.AppendLine($"{category};{name};{level};{id}");
                    }

                    // UTF8 BOM для корректного русского текста в Excel
                    File.WriteAllText(
                        saveDialog.FileName,
                        sb.ToString(),
                        new UTF8Encoding(true));
                }
            }
            catch (Exception ex)
            {
                Autodesk.Revit.UI.TaskDialog.Show(
                    "Ошибка экспорта",
                    ex.Message);
            }
        }
        /// <summary>
        /// Экранирование CSV
        /// </summary>
        private string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            value = value.Replace("\"", "\"\"");

            return $"\"{value}\"";
        }
    }
}