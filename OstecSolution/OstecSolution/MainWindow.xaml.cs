using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OstecSolution
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, Color> _colors = new Dictionary<string, Color>
        {
            { "Белый", Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF) },
            { "Чёрный", Color.FromArgb(0xFF, 0x00, 0x00, 0x00) },
            { "Коричневый", Color.FromArgb(0xFF, 0xA5, 0x72, 0x27) },
            { "Синий", Color.FromArgb(0xFF, 0x33, 0x00, 0xEF) },
            { "Голубой", Color.FromArgb(0xFF, 0x00, 0xD4, 0xFD) },
            { "Красный", Color.FromArgb(0xFF, 0xFF, 0x00, 0x00) },
            { "Жёлтый", Color.FromArgb(0xFF, 0xDE, 0xE0, 0x01) },
            { "Зелёный", Color.FromArgb(0xFF, 0x2B, 0xFF, 0x00) },
            { "Серый", Color.FromArgb(0xFF, 0x6B, 0x6B, 0x6B) },
            { "Розовый", Color.FromArgb(0xFF, 0xFF, 0x9A, 0x9A) }
        };

        private Queue<Color> _availableColors;
        private int _touchCount = 0;
        private Timer _weatherTimer;
        private Timer _timeTimer;
        private DispatcherTimer _loadingTimer;
        private int _loadingStep = 0;

        private List<string> _apiKeys = new List<string> // Сервис нестабильный, хотелось бы использовать Яндекс, но его уже забрала Света:( 
        {
            "2c7a6d33852dd42bb9c5eaf182105696", // Проблему с ошибкой запроса пришлось стабилизировать с помощью нескольких API, так как сервис ограничивает количество запросов
            "9633af91142e93ebac169b0560d54497", // Иногда ошибки все равно возникают, чинится перезагрузкой приложения/компьютера
            "cb978a8f6950e9c501e73872b5c90106"
        };

        private int _currentApiKeyIndex = 0;
        private const string CityId = "524901";

        public MainWindow()
        {
            InitializeComponent();
            InitializeButtonColors();
            InitializeTimers();
            StartLoadingAnimation();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void InitializeButtonColors()
        {
            Random rnd = new Random();
            var buttons = new List<Button> { Button1, Button2, Button3, Button4, Button5, Button6, Button7, Button8, Button9 };
            var colors = _colors.Values.OrderBy(x => rnd.Next()).ToList();
            _availableColors = new Queue<Color>(colors.Skip(buttons.Count));

            foreach (var button in buttons)
            {
                var color = colors.First();
                colors.RemoveAt(0);

                button.Background = new SolidColorBrush(color);
                button.Content = _colors.FirstOrDefault(x => x.Value == color).Key;

                if (color == _colors["Белый"])
                {
                    button.Foreground = new SolidColorBrush(_colors["Чёрный"]);
                }
                else
                {
                    button.Foreground = new SolidColorBrush(Colors.White);
                }

                button.Click += Button_Click;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                var currentColor = ((SolidColorBrush)button.Background).Color;
                _availableColors.Enqueue(currentColor);
                var newColor = _availableColors.Dequeue();
                button.Background = new SolidColorBrush(newColor);
                button.Content = _colors.FirstOrDefault(x => x.Value == newColor).Key;

                if (newColor == _colors["Белый"])
                {
                    button.Foreground = new SolidColorBrush(_colors["Чёрный"]);
                }
                else
                {
                    button.Foreground = new SolidColorBrush(Colors.White);
                }

                _touchCount++;
                touch.Content = $"Количество нажатий: {_touchCount}";
            }
        }

        private void InitializeTimers()
        {
            _weatherTimer = new Timer(2 * 60 * 1000);
            _weatherTimer.Elapsed += async (sender, e) => await UpdateTemperature();
            _weatherTimer.Start();
            UpdateTemperature().ConfigureAwait(false);

            _timeTimer = new Timer(5000);
            _timeTimer.Elapsed += async (sender, e) => await UpdateTime();
            _timeTimer.Start();
            UpdateTime().ConfigureAwait(false);
        }

        private async Task UpdateTime()
        {
            var currentTimeInMoscow = await GetCurrentTimeInMoscow();

            Dispatcher.Invoke(() =>
            {
                time.Content = $"Текущее время по Москве: {currentTimeInMoscow}";
            });
        }

        private async Task UpdateTemperature()
        {
            var result = await GetTemperatureInMoscow();

            Dispatcher.Invoke(() =>
            {
                if (result.StartsWith("Ошибка"))
                {
                    temperature.Content = $"Текущая температура в Москве: N/A";
                    temperatureError.Content = result;
                }
                else
                {
                    temperature.Content = $"Текущая температура в Москве: {result}°C";
                    temperatureError.Content = string.Empty;
                }
            });
        }

        private async Task<string> GetCurrentTimeInMoscow()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string url = "http://worldtimeapi.org/api/timezone/Europe/Moscow";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var timeData = JsonConvert.DeserializeObject<TimeResponse>(json);

                        if (timeData?.Datetime != null)
                        {
                            DateTime time = DateTime.Parse(timeData.Datetime);
                            return time.ToString("HH:mm");
                        }
                        else
                        {
                            return "N/A";
                        }
                    }
                    else
                    {
                        return $"Ошибка: {response.StatusCode}";
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return $"{ex.Message}";
            }
            catch (Exception ex)
            {
                return $"{ex.Message}";
            }
        }

        private async Task<string> GetTemperatureInMoscow()
        {
            for (int attempt = 0; attempt < _apiKeys.Count; attempt++)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        string apiKey = _apiKeys[_currentApiKeyIndex];
                        string url = $"http://api.openweathermap.org/data/2.5/weather?id={CityId}&appid={apiKey}&units=metric";
                        HttpResponseMessage response = await client.GetAsync(url);

                        if (response.IsSuccessStatusCode)
                        {
                            var json = await response.Content.ReadAsStringAsync();
                            var weatherData = JsonConvert.DeserializeObject<WeatherResponse>(json);

                            if (weatherData?.Main?.Temp != null)
                            {
                                return weatherData.Main.Temp.ToString("F1");
                            }
                            else
                            {
                                return "Ошибка: данные температуры отсутствуют.";
                            }
                        }
                        else
                        {
                            _currentApiKeyIndex = (_currentApiKeyIndex + 1) % _apiKeys.Count;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    _currentApiKeyIndex = (_currentApiKeyIndex + 1) % _apiKeys.Count;
                    return $"Ошибка запроса: {ex.Message}";
                }
                catch (Exception ex)
                {
                    return $"Ошибка: {ex.Message}";
                }
            }

            return "Ошибка: Все API ключи не сработали.";
        }

        private class WeatherResponse
        {
            public MainData Main { get; set; }
        }

        private class MainData
        {
            public double Temp { get; set; }
        }

        private class TimeResponse
        {
            public string Datetime { get; set; }
        }

        private void StartLoadingAnimation()
        {
            _loadingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.75)
            };
            _loadingTimer.Tick += OnLoadingTimerTick;
            _loadingTimer.Start();
        }

        private void OnLoadingTimerTick(object sender, EventArgs e)
        {
            _loadingStep++;
            if (_loadingStep > 3)
            {
                _loadingTimer.Stop();
                LoadingScreen.Visibility = Visibility.Collapsed;
                return;
            }

            string baseText = "Ostec-group";
            LoadingLabel.Content = baseText + new string('.', _loadingStep % 4);
        }
    }
}
