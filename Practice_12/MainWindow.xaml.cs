/*
Создать программу, помогающую в решении судоку. В судоку используются цифры от 1 до 9-и включительно. Как известно, по правилам судоку, в одной строке длиной девять ячеек, столбце - длиной девять ячеек, и квадрате 3х3 цифры повторяться не могут. Квадрат 9х9 состоит из 9-и квадратов 3х3

У пользователя на экране появляется квадрат 9х9 разделенный на малые квадраты 3х3, в каждую ячейку которого он может вводит любую цифру от 1 до 9-и включительно. Пользователь вводит начальные условия. По нажатию на кнопку "Условие введено" блокируется ввод в ячейки, заполненные пользователем. В остальные ячейки пользователь вводит цифры, чтобы решить судоку. 

Если при заполнении в строке появляются одинаковые числа, то строка окрашивается в красный цвет, то же происходит и со столбцом, и с квадратом 3х3. При условии, что одновременно в строке, столбце и квадрате 3х3 все числа различаются, и все условия выполнены, эти ячейки одновременно окрашиваются в зеленый цвет

Программа должна раз в минуту или принудительно по щелчку запоминать текущее состояние решения и открывать его при следующем запуске программы
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Serialization;
using System.Threading;

namespace Practice_12
{
    // Класс для сериализации (сохранения прогресса)
    [Serializable]
    public class SudokuState
    {
        public char[][] Cells { get; set; }
        public bool[][] ReadOnlyCells { get; set; }
        public string[][] Colors { get; set; }
        public bool IsConditionButtonPressed { get; set; }

        public SudokuState()
        {
        }

        public SudokuState(int size)
        {
            Cells = new char[size][];
            ReadOnlyCells = new bool[size][];
            Colors = new string[size][];
            IsConditionButtonPressed = false;

            for (int i = 0; i < size; i++)
            {
                Cells[i] = new char[size];
                ReadOnlyCells[i] = new bool[size];
                Colors[i] = new string[size];

                for (int j = 0; j < size; j++)
                {
                    Cells[i][j] = '?';
                    ReadOnlyCells[i][j] = false;
                    Colors[i][j] = "white";
                }
            }
        }

        // Заполняет ячейки для экземпляра сохраненного судоку
        public void PopulateCells(char[,] values, bool[,] readOnlyStatus, string[,] colors, bool isConditionButtonPressed)
        {
            for (int i = 0; i < Cells.Length; i++)
            {
                for (int j = 0; j < Cells.Length; j++)
                {
                    Cells[i][j] = values[i, j];
                    ReadOnlyCells[i][j] = readOnlyStatus[i, j];
                    Colors[i][j] = colors[i, j];
                    IsConditionButtonPressed = isConditionButtonPressed;
                }
            }

            SaveToFile();
        }

        // Непосредственно производит сериализацию (сохранение в файл)
        private void SaveToFile()
        {
            string filePath = @"C:\Users\Rulzz\Desktop\шарага\2024\college_practice\Practice_12\sudokuSave.xml";

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(SudokuState));
                xmlSerializer.Serialize(writer, this);
            }
        }

        // Загружает сохраненное судоку
        public static SudokuState LoadSudokuState(string fileName)
        {
            try
            {
                if (!File.Exists(fileName) || new FileInfo(fileName).Length == 0)
                {
                    return null;
                }

                XmlSerializer serializer = new XmlSerializer(typeof(SudokuState));
                using (XmlReader reader = XmlReader.Create(fileName))
                {
                    return (SudokuState)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка загрузки судоку: " + ex.Message);
                return null;
            }
        }
    }

    public partial class MainWindow : Window
    {
        // Таймер для постоянного мониторинга и обновления цветов ячеек
        DispatcherTimer updColorsTimer = new DispatcherTimer();

        // Таймер автосохранения
        DispatcherTimer autoSaveTimer = new DispatcherTimer();

        // Таймер проверки условий победы
        DispatcherTimer winDetectorTimer = new DispatcherTimer();

        public MainWindow()
        {
            InitializeComponent();

            // Загружаем судоку
            string filePath = @"C:\Users\Rulzz\Desktop\шарага\2024\college_practice\Practice_12\sudokuSave.xml";
            SudokuState loadedState = SudokuState.LoadSudokuState(filePath);

            if (loadedState != null)
            {
                if (loadedState.Cells != null && loadedState.ReadOnlyCells != null && loadedState.Colors != null)
                {
                    UpdateUIFromState(loadedState);
                }
                else
                {
                    Debug.WriteLine("Файл сохранения поврежден...");
                }
            }
            else
            {
                Debug.WriteLine("Файл сохранения пустой...");
            }

            // Таймер для обновления цветов ячеек
            updColorsTimer.Interval = TimeSpan.FromSeconds(.5);
            updColorsTimer.Tick += UpdColorsTimer_Tick;
            updColorsTimer.Start();

            // Таймер для автосохранения
            autoSaveTimer.Interval = TimeSpan.FromSeconds(60);
            autoSaveTimer.Tick += AutoSaveTimer_Tick;
            autoSaveTimer.Start();

            // Таймер для проверки победы
            winDetectorTimer.Interval = TimeSpan.FromSeconds(1);
            winDetectorTimer.Tick += CheckForAWin_Tick;
            winDetectorTimer.Start();
        }

        private void CheckForAWin_Tick(object sender, EventArgs e)
        {
            CheckForCompletionAndCongratulate();
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            AutoSave();
        }

        private void UpdColorsTimer_Tick(object sender, EventArgs e)
        {
            UpdateColors();
        }

        int N = 9; // Макс. номер столбцов и строк
        int SN = 3; // Кол-во. строк и столбцов в квадрате
        // Хранит состояние правильности ячеек по правилам судоку (нужно для окрашвания)
        bool[,] columnErrors = new bool[9, 9];
        bool[,] rowErrors = new bool[9, 9];
        bool[,] sqrErrors = new bool[9, 9];
        private bool isGameCompleted = false;


        // 1. Регистрируем нажатие на "Условие введено". Блокируем кнопку.
        private void buttonConditionIsInputted_Click(object sender, RoutedEventArgs e)
        {
            buttonConditionIsInputted.IsEnabled = false;

            // 2. Проверяем, есть ли другие символы кроме 1-9 и ?
            if (ValidateTextBoxes())
            {
                // Если нет,
                // 3. Проверяем корректность по правилам судоку.
                if (!CheckIfSafeAll())
                {
                    // Если неверно, то выводим ошибку "Неверная расстановка". Разблокируем кнопку.
                    MessageBox.Show("Начальные условия противоречат правилам судоку!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    buttonConditionIsInputted.IsEnabled = true;
                }
                else
                {
                    // Если верно,
                    // 4. Проверяем можно ли решить это судоку.
                    if (!IsSudokuSolvable())
                    {
                        // Если нельзя, то выводим ошибку "Судоку нерешаемо с этими условиями" + ПОДСВЕТКА ОШИБКИ. Разблокируем кнопку.
                        MessageBox.Show("Судоку с данными начальными условиями НЕРЕШАЕМ!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        buttonConditionIsInputted.IsEnabled = true;
                    }
                    else
                    {
                        // Если можно,
                        // 5. Блокируем ячейки начальных условий (isReadOnly=true).
                        BlockInitialConds();
                    }
                }
            }
            else
            {
                // Если да, то выводим ошибку "Неправильные символы". Разблокируем кнопку.
                MessageBox.Show("Вы ввели недопустимые символы!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                buttonConditionIsInputted.IsEnabled = true;
            }

        }



        // 8. Реализация Сброса
        private void buttonReset_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult userAnswer = MessageBox.Show("Вы хотите сбросить все поле?", "Подтверждение сброса", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (userAnswer == MessageBoxResult.Yes)
            {
                for (int row = 0; row < N; row++)
                {
                    for (int col = 0; col < N; col++)
                    {
                        string name = $"_{row}_{col}";
                        TextBox textBox = this.FindName(name) as TextBox;
                        BrushConverter brushConverter = new BrushConverter();

                        textBox.Background = (Brush)brushConverter.ConvertFromString("White");
                        SetTextBoxValue(row, col, '?');
                        textBox.IsReadOnly = false;
                        buttonConditionIsInputted.IsEnabled = true;
                    }
                }
            }
        }

        // Ручное сохранение судоку через кнопку
        private void buttonSave_Click(object sender, RoutedEventArgs e)
        {
            var puzzleState = new SudokuState(N);

            bool[,] currentReadOnlyStatus = new bool[N, N];
            char[,] currentValues = new char[N, N];
            string[,] colors = new string[N, N];
            bool isConditionButtonPressed = false;

            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    char num = GetTextBoxValue(row, col);
                    string name = $"_{row}_{col}";
                    TextBox textBox = this.FindName(name) as TextBox;

                    currentValues[row, col] = num;

                    if (textBox.IsReadOnly == true)
                    {
                        currentReadOnlyStatus[row, col] = true;
                        isConditionButtonPressed |= true;
                    }
                    else
                        currentReadOnlyStatus[row, col] = false;

                    BrushConverter brushConverter = new BrushConverter();
                    SolidColorBrush greyBrush = (SolidColorBrush)brushConverter.ConvertFromString("#FFCCCCCC");
                    SolidColorBrush redBrush = (SolidColorBrush)brushConverter.ConvertFromString("#FFDA3636");
                    SolidColorBrush whiteBrush = (SolidColorBrush)brushConverter.ConvertFromString("White");
                    SolidColorBrush greenBrush = (SolidColorBrush)brushConverter.ConvertFromString("#FF4CD84D");

                    SolidColorBrush currentBrush = textBox.Background as SolidColorBrush;

                    if (currentBrush != null)
                    {
                        if (currentBrush.Color == redBrush.Color)
                        {
                            colors[row, col] = "red";
                        }
                        else if (currentBrush.Color == whiteBrush.Color)
                        {
                            colors[row, col] = "white";
                        }
                        else if (currentBrush.Color == greenBrush.Color)
                        {
                            colors[row, col] = "green";
                        }
                        else if (currentBrush.Color == greyBrush.Color)
                        {
                            colors[row, col] = "grey";
                        }
                    }
                }
            }

            puzzleState.PopulateCells(currentValues, currentReadOnlyStatus, colors, isConditionButtonPressed);

            MessageBox.Show("Прогресс успешно сохранен!", "Сохранено", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // 7. Автосейв каждую минуту
        private void AutoSave()
        {
            var puzzleState = new SudokuState(N);

            bool[,] currentReadOnlyStatus = new bool[N, N];
            char[,] currentValues = new char[N, N];
            string[,] colors = new string[N, N];
            bool isConditionButtonPressed = false;

            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    char num = GetTextBoxValue(row, col);
                    string name = $"_{row}_{col}";
                    TextBox textBox = this.FindName(name) as TextBox;

                    currentValues[row, col] = num;

                    if (textBox.IsReadOnly == true)
                    {
                        currentReadOnlyStatus[row, col] = true;
                        isConditionButtonPressed |= true;
                    }
                    else
                        currentReadOnlyStatus[row, col] = false;

                    BrushConverter brushConverter = new BrushConverter();
                    SolidColorBrush greyBrush = (SolidColorBrush)brushConverter.ConvertFromString("#FFCCCCCC");
                    SolidColorBrush redBrush = (SolidColorBrush)brushConverter.ConvertFromString("#FFDA3636");
                    SolidColorBrush whiteBrush = (SolidColorBrush)brushConverter.ConvertFromString("White");
                    SolidColorBrush greenBrush = (SolidColorBrush)brushConverter.ConvertFromString("#FF4CD84D");

                    SolidColorBrush currentBrush = textBox.Background as SolidColorBrush;

                    if (currentBrush != null)
                    {
                        if (currentBrush.Color == redBrush.Color)
                        {
                            colors[row, col] = "red";
                        }
                        else if (currentBrush.Color == whiteBrush.Color)
                        {
                            colors[row, col] = "white";
                        }
                        else if (currentBrush.Color == greenBrush.Color)
                        {
                            colors[row, col] = "green";
                        }
                        else if (currentBrush.Color == greyBrush.Color)
                        {
                            colors[row, col] = "grey";
                        }
                    }
                }
            }

            puzzleState.PopulateCells(currentValues, currentReadOnlyStatus, colors, isConditionButtonPressed);
        }

        // Загрузка сохранения
        private void UpdateUIFromState(SudokuState loadedState)
        {
            for (int row = 0; row < loadedState.Cells.Length; row++)
            {
                if (loadedState.Cells[row] != null && loadedState.ReadOnlyCells[row] != null && loadedState.Colors[row] != null)
                {
                    for (int col = 0; col < loadedState.Cells[row].Length; col++)
                    {
                        string name = $"_{row}_{col}";
                        TextBox textBox = this.FindName(name) as TextBox;

                        if (textBox != null)
                        {
                            textBox.Text = loadedState.Cells[row][col].ToString();

                            switch (loadedState.Colors[row][col])
                            {
                                case "red":
                                    ChangeBGColor(row, col, "red");
                                    break;
                                case "white":
                                    ChangeBGColor(row, col, "white");
                                    break;
                                case "green":
                                    ChangeBGColor(row, col, "green");
                                    break;
                                case "grey":
                                    ChangeBGColor(row, col, "grey");
                                    break;
                            }

                            textBox.IsReadOnly = loadedState.ReadOnlyCells[row][col];
                        }
                    }
                }
            }

            if (loadedState.IsConditionButtonPressed)
                buttonConditionIsInputted.IsEnabled = false;
        }

        // 8. Сообщение о победе, если все правильно
        private void CheckForCompletionAndCongratulate()
        {
            if (isGameCompleted)
                return;

            bool isComplete = true;

            // Проверяем, заполнено ли все
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    if (GetTextBoxValue(row, col) == '?')
                    {
                        isComplete = false;
                        break;
                    }
                }
                if (!isComplete) break;
            }

            // Делаем все проверки isUnusedIn...
            if (isComplete)
            {
                for (int row = 0; row < N; row++)
                {
                    for (int col = 0; col < N; col++)
                    {
                        char num = GetTextBoxValue(row, col);
                        if (!isUnUsedInCol(col, num) || !isUnUsedInRow(row, num) || !isUnUsedInSqr(row - row % SN, col - col % SN, num))
                        {
                            isComplete = false;
                            break;
                        }
                    }
                    if (!isComplete) break;
                }
            }

            // Выводим сообщение о победе, блокируем все ячейки и кнопку задания начальных условий
            if (isComplete)
            {
                MessageBox.Show("Поздравляю! Вы решили судоку.", "ПОБЕДА", MessageBoxButton.OK, MessageBoxImage.Information);
                MakeAllCellsReadOnly();
                buttonConditionIsInputted.IsEnabled = false;
                isGameCompleted = true;
            }
        }

        // Делает все ячейки заблокированными
        private void MakeAllCellsReadOnly()
        {
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    string name = $"_{row}_{col}";
                    TextBox textBox = this.FindName(name) as TextBox;
                    if (textBox != null)
                    {
                        // Debugging statement
                        Debug.WriteLine($"Making cell {name} read-only.");
                        textBox.IsReadOnly = true;
                    }
                }
            }
        }



        // Удаляет ? при фокусировке на ячейку
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Text == "?")
            {
                textBox.Text = "";
            }
        }

        // Ставит ? при потери фокусировки, если не было поставлено ничего
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = "?";
            }
        }

        // 6. Мониторит и меняет цвета клеток
        private void UpdateColors()
        {
            // Обнуляем список ошибок
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    rowErrors[row, col] = false;
                    columnErrors[row, col] = false;
                    sqrErrors[row, col] = false;
                }
            }

            // По новой прогоняем все проверки, которые поставят ошибки
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    char num = GetTextBoxValue(row, col);
                    CheckIfSafe(row, col, num);
                }
            }

            // Обновляем цвета ОШИБОК (КРАСНЫЙ) на основе массивов errors
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    string color = "white";

                    // Там, где стоит ошибка - красим красным
                    if (sqrErrors[row, col] || rowErrors[row, col] || columnErrors[row, col])
                    {
                        color = "red";
                    }

                    ChangeBGColor(row, col, color);
                }
            }

            // Обновляем цвета правильных квадратов, строк и колонок (ЗЕЛЕНЫЙ)
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    bool isRowCorrect = true;
                    bool isColumnCorrect = true;
                    bool isSquareCorrect = true;

                    // Проверяем строку
                    for (int checkCol = 0; checkCol < N; checkCol++)
                    {
                        if (rowErrors[row, checkCol] || GetTextBoxValue(row, checkCol) == '?')
                        {
                            isRowCorrect = false;
                            break;
                        }
                    }

                    // Проверяем столбец
                    for (int checkRow = 0; checkRow < N; checkRow++)
                    {
                        if (columnErrors[checkRow, col] || GetTextBoxValue(checkRow, col) == '?')
                        {
                            isColumnCorrect = false;
                            break;
                        }
                    }

                    // Проверяем квадрат
                    int sqrRowStart = (row / SN) * SN;
                    int sqrColStart = (col / SN) * SN;
                    for (int sqrRow = sqrRowStart; sqrRow < sqrRowStart + SN; sqrRow++)
                    {
                        for (int sqrCol = sqrColStart; sqrCol < sqrColStart + SN; sqrCol++)
                        {
                            if (sqrErrors[sqrRow, sqrCol] || GetTextBoxValue(sqrRow, sqrCol) == '?')
                            {
                                isSquareCorrect = false;
                                break;
                            }
                        }
                        if (!isSquareCorrect) break; // Если квадрат неправильный, выходим до окончания проверки
                    }

                    // Если и квадрат, и строка, и столбец правильные - красим в зеленый
                    if (isRowCorrect && isColumnCorrect && isSquareCorrect)
                    {
                        for (int checkCol = 0; checkCol < N; checkCol++)
                            ChangeBGColor(row, checkCol, "green");
                        for (int checkRow = 0; checkRow < N; checkRow++)
                            ChangeBGColor(checkRow, col, "green");
                        for (int sqrRow = sqrRowStart; sqrRow < sqrRowStart + SN; sqrRow++)
                            for (int sqrCol = sqrColStart; sqrCol < sqrColStart + SN; sqrCol++)
                                ChangeBGColor(sqrRow, sqrCol, "green");
                    }
                }
            }
        }

        // -- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---
        // Получает значение в ячейке по имени TextBox'а
        private char GetTextBoxValue(int row, int col)
        {
            string name = $"_{row}_{col}";
            TextBox textBox = this.FindName(name) as TextBox;

            if (textBox != null && !string.IsNullOrEmpty(textBox.Text))
            {
                return textBox.Text[0];
            }
            else
            {
                return '?';
            }
        }

        // Устанавливает значение TextBox'a
        private void SetTextBoxValue(int row, int col, char value)
        {
            string name = $"_{row}_{col}";
            TextBox textBox = this.FindName(name) as TextBox;
            if (textBox != null)
            {
                textBox.Text = value.ToString();
            }
        }

        // Проверяет корректность введенных символов
        private bool ValidateTextBoxes()
        {
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    char textBoxValue = GetTextBoxValue(row, col);
                    if (textBoxValue != '?' && !(textBoxValue >= '1' && textBoxValue <= '9'))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        // Меняет цвет фона ячейки
        private void ChangeBGColor(int row, int col, string color)
        {
            string name = $"_{row}_{col}";
            TextBox textBox = this.FindName(name) as TextBox;

            if (textBox != null)
            {
                BrushConverter brushConverter = new BrushConverter();
                SolidColorBrush greyBrush = (SolidColorBrush)brushConverter.ConvertFromString("#FFCCCCCC");

                // Если это ячейка начального условия - не перекрашиваем ее
                if (textBox.Background is SolidColorBrush currentBrush && currentBrush.Color == greyBrush.Color)
                    return;

                if (color == "red")
                {
                    textBox.Background = (Brush)brushConverter.ConvertFromString("#FFDA3636");
                }
                else if (color == "white")
                {
                    textBox.Background = (Brush)brushConverter.ConvertFromString("White");
                }
                else if (color == "green")
                {
                    textBox.Background = (Brush)brushConverter.ConvertFromString("#FF4CD84D");
                }
                else if (color == "grey")
                {
                    textBox.Background = (Brush)brushConverter.ConvertFromString("#FFCCCCCC");
                }
            }
        }

        // Блокирует все ячейки, введенные в начале
        private void BlockInitialConds()
        {
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    int num = GetTextBoxValue(row, col);
                    string name = $"_{row}_{col}";
                    TextBox textBox = this.FindName(name) as TextBox;
                    if (num != '?')
                    {
                        textBox.IsReadOnly = true;
                        ChangeBGColor(row, col, "grey"); // Красит в серый
                    }
                }
            }
        }

        // --- ПРОВЕРКИ ---
        // Верны ли начальные условия
        private bool CheckIfSafeAll()
        {
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    char num = GetTextBoxValue(row, col);
                    if (!CheckIfSafe(row, col, num))
                        return false;
                }
            }

            return true;
        }

        // Совмещает все нижеописанные методы проверок на повторы с столбце, строке и квадрате
        private bool CheckIfSafe(int row, int col, char num)
        {
            if (num != '?')
            {
                // Т.к. из return методы не выполняется, выполняем их явно
                isUnUsedInCol(col, num);
                isUnUsedInRow(row, num);
                isUnUsedInSqr(row - row % SN, col - col % SN, num);

                return (isUnUsedInCol(col, num) &&
                        isUnUsedInRow(row, num) &&
                        isUnUsedInSqr(row - row % SN, col - col % SN, num)); // Передаем координаты начальных столбца и строки
            }
            else
            {
                return true;
            }
        }

        // Не использовано ли данное число в квадрате?
        private bool isUnUsedInSqr(int row, int col, char num)
        {
            int count = 0;

            for (int i = 0; i < SN; i++)
            {
                for (int j = 0; j < SN; j++)
                {
                    char cellValue = GetTextBoxValue(row + i, col + j);
                    if (cellValue != '?' && cellValue == num)
                        count++;
                }
            }

            // Доп. цикл чтобы поставить true на все ячейки квадрата, если есть повторка
            if (count > 1)
            {
                for (int i = 0; i < SN; i++)
                {
                    for (int j = 0; j < SN; j++)
                    {
                        sqrErrors[row + i, col + j] = true;
                    }
                }
            }

            if (count > 1)
                return false;

            return true;
        }

        // Не использовано ли данное число в колонке?
        private bool isUnUsedInCol(int col, char num)
        {
            int count = 0;

            for (int row = 0; row < N; row++)
            {
                char cellValue = GetTextBoxValue(row, col);
                if (cellValue != '?' && cellValue == num)
                    count++;
            }

            if (count > 1)
            {
                //Debug.WriteLine("Найдена повторка в столбце");
                for (int row = 0; row < N; row++)
                {
                    columnErrors[row, col] = true;
                    //Debug.WriteLine($"true поставлена в {row} и {col}");
                }
            }

            if (count > 1)
                return false;

            return true;
        }

        // Не использовано ли данное число в строке?
        private bool isUnUsedInRow(int row, char num)
        {
            int count = 0;

            for (int col = 0; col < N; col++)
            {
                char cellValue = GetTextBoxValue(row, col);
                if (cellValue != '?' && cellValue == num)
                    count++;
            }

            if (count > 1)
            {
                //Debug.WriteLine("Найдена повторка в строке");
                for (int col = 0; col < N; col++)
                {
                    rowErrors[row, col] = true;
                    //Debug.WriteLine($"true поставлена в {row} и {col}");
                }
            }

            if (count > 1)
                return false;

            return true;
        }

        // Решаемо ли судоку с данными исходными параметрами?
        private bool IsSudokuSolvable()
        {
            // Если находится ячейка, в которую нельзя подставить ни одну цифру - возвращаем false
            for (int row = 0; row < N; row++)
            {
                for (int col = 0; col < N; col++)
                {
                    char num = GetTextBoxValue(row, col);
                    if (num == '?')
                    {
                        bool isSafe = false;
                        // В ячейку с ? временно подставляются цифры от 1 до 9
                        for (int i = 1; i <= 9; i++)
                        {
                            char newNum = (char)('0' + i); // i -> char
                            SetTextBoxValue(row, col, newNum);
                            if (CheckIfSafe(row, col, newNum))
                            {
                                isSafe = true;
                                SetTextBoxValue(row, col, '?');
                                break; // Когда нашли хоть одную безопасную цифру - выходим из цикла
                            }
                        }
                        // Если не нашли ни одной безопасной цифры - возвращаем false
                        if (!isSafe)
                        {
                            SetTextBoxValue(row, col, '?');
                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}