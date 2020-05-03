using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Tesseract;
using Flurl.Http;

namespace TextRecognition
{
    public partial class MainWindow : Window
    {
        private string _filePath;
        private string _lang;

        static readonly HttpClient client = new HttpClient();

        private MediaPlayer mediaPlayer = new MediaPlayer();
        private bool userIsDraggingSlider = false;
        private string _tempFileName;
        private string _tempFilePath;

        private string _recognizedData;

        public string _recivedData { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            _filePath = "Выберите изображение";
            _tempFileName = "";
            _tempFilePath = "";
            DirectoryInfo currDirectory = new DirectoryInfo("./tessdata");
            FileInfo[] langFiles = currDirectory.GetFiles();

            string[] _langs = new string[langFiles.Length];

            for (int i = 0; i < _langs.Length; i++)
            {
                _langs[i] = langFiles[i].Name.Split('.')[0];
            }

            cBoxLangPicker.ItemsSource = _langs;
            cBoxLangPicker.SelectedIndex = 0;
            cbVoiceChoice.SelectedIndex = 0;

            btnPlay.IsEnabled = false;
            btnPause.IsEnabled = false;
            btnStop.IsEnabled = false;

            btnRecognize.IsEnabled = false;
        }

        public async void DoOCR(string path, string language)
        {
            btnRecognize.IsEnabled = false;
            try
            {
                using (var engine = new TesseractEngine(@"./tessdata", language, EngineMode.Default))
                {
                    using (var img = Pix.LoadFromFile(path))
                    {
                        using (var page = engine.Process(img))
                        {
                            string text;
                            await Task.Run(() =>
                            {
                                text = page.GetText();
                                _recognizedData = text;
                            });
                            tbDebug.Text = string.Format("Mean confidence: {0}", page.GetMeanConfidence());
                            
                            tbxOutput.Text = _recognizedData;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                Debug.WriteLine("Unexpected Error: " + e.Message);
                Debug.WriteLine("Details: ");
                Debug.WriteLine(e.ToString());
            }

            btnRecognize.IsEnabled = true;
        }

        private void BtnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = @"c:\";
            openFileDialog.Filter = "Изображения (*.jpg;*.png;*.tif)|*.jpg;*.png;*.tif|Все файлы (*.*)|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                _filePath = openFileDialog.FileName;
                tbxFilePath.Text = openFileDialog.FileName;
                try
                {
                    imgFile.Source = new BitmapImage(new Uri(openFileDialog.FileName, UriKind.Absolute));
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            btnRecognize.IsEnabled = true;
        }

        private void btnRecognize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileInfo file = new FileInfo(_filePath);
                DoOCR(_filePath, _lang);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cBoxLangPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _lang = cBoxLangPicker.SelectedItem.ToString();
        }

        private void tbxFilePath_GotFocus(object sender, RoutedEventArgs e)
        {
            tbxFilePath.Text = null;
        }

        private void tbxFilePath_LostFocus(object sender, RoutedEventArgs e)
        {
            tbxFilePath.Text = _filePath;
        }

        private void btnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (!mediaPlayer.IsBuffering)
            {
                StartPlayback();
            }
            mediaPlayer.Play();

            btnPause.IsEnabled = true;
            btnPlay.IsEnabled = false;
            btnStop.IsEnabled = true;
        }

        private void btnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();

            btnPause.IsEnabled = false;
            btnPlay.IsEnabled = true;
            btnStop.IsEnabled = true;
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            mediaPlayer.Close();

            btnPause.IsEnabled = false;
            btnPlay.IsEnabled = true;
            btnStop.IsEnabled = false;
        }

        private void btnSaveAudio_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "WAV Audio|*.wav";
            dialog.FileName = _tempFileName;
            dialog.Title = "Сохранить аудиозапись";
            if (dialog.ShowDialog() == true)
            {
                File.Copy(_tempFilePath, dialog.FileName);
                MessageBox.Show("Аудиозапись успешно сохранена!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnGetSynthText_Click(object sender, RoutedEventArgs e)
        {
            if (tbxOutput.Text == "")
            {
                MessageBox.Show("Отсутствует текст для синтеза речи", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                btnPause.IsEnabled = true;
                btnPlay.IsEnabled = false;
                btnStop.IsEnabled = true;
             
                StartPlayback();
            }
        }

        private async void StartPlayback()
        {
            if (_lang == "rus" || _lang == "bel" || _lang == "eng")
            {
                btnGetSynthText.IsEnabled = false;
                btnSaveAudio.IsEnabled = false;
                sliderAudioPlayback.IsEnabled = false;
                btnPlay.IsEnabled = false;
                btnPause.IsEnabled = false;
                btnStop.IsEnabled = false;

                string voice = "";
                if (_lang == "bel" || _lang == "eng")
                {
                    switch (cbVoiceChoice.SelectedIndex)
                    {
                        case 0:
                            voice = "AlesiaBel";
                            break;
                        case 1:
                            voice = "BorisBel";
                            break;
                        case 2:
                            voice = "BorisBelHigh";
                            break;
                        default:
                            voice = "";
                            break;
                    }
                }

                if (_lang == "rus")
                {
                    switch (cbVoiceChoice.SelectedIndex)
                    {
                        case 0:
                            voice = "AlesiaRus";
                            break;
                        case 1:
                            voice = "BorisRus";
                            break;
                        case 2:
                            voice = "BorisRusHigh";
                            break;
                        default:
                            voice = "";
                            break;
                    }
                }

                string json = await RequestAudioURL(tbxOutput.Text, _lang, voice);
                ResultJson resultJson = JsonConvert.DeserializeObject<ResultJson>(json);

                await Task.Run(() =>
                {
                    using (WebClient client = new WebClient())
                    {
                        if (resultJson != null)
                        {
                            string[] tempArray = resultJson.audio.Split('/');
                            _tempFileName = tempArray[tempArray.Length - 1];
                            Directory.CreateDirectory("./tempAudio");
                            _tempFilePath = "tempAudio/" + _tempFileName;
                            client.DownloadFile(resultJson.audio, _tempFilePath);
                        }
                    }

                });

                btnGetSynthText.IsEnabled = true;
                btnSaveAudio.IsEnabled = true;
                sliderAudioPlayback.IsEnabled = true;
                btnPlay.IsEnabled = false;
                btnPause.IsEnabled = true;
                btnStop.IsEnabled = true;

                mediaPlayer.Open(new Uri(_tempFilePath, UriKind.RelativeOrAbsolute));
                mediaPlayer.Play();
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;

                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(1);
                timer.Tick += timer_Tick;
                timer.Start();
            }
            else
            {
                MessageBox.Show("Синтез речи поддерживается только на белорусском, русском и английском языках", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            mediaPlayer.Close();
            btnPause.IsEnabled = false;
            btnPlay.IsEnabled = true;
            btnStop.IsEnabled = false;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (mediaPlayer.Source != null && (mediaPlayer.NaturalDuration.HasTimeSpan) && (!userIsDraggingSlider))
            {
                sliderAudioPlayback.Minimum = 0;
                sliderAudioPlayback.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliderAudioPlayback.Value = mediaPlayer.Position.TotalSeconds;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if(mediaPlayer.NaturalDuration.HasTimeSpan)
                tbPlaybackState.Text = string.Format("{0} / {1}", mediaPlayer.Position.ToString(@"mm\:ss"), mediaPlayer.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
        }

        private void sliderAudioPlayback_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void sliderAudioPlayback_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            mediaPlayer.Position = TimeSpan.FromSeconds(sliderAudioPlayback.Value);
        }

        static async Task<string> RequestAudioURL(string textData, string lang, string voice)
        {
            string response;

            var result = await "https://corpus.by/TextToSpeechSynthesizer/api.php".PostUrlEncodedAsync(new
            {
                text = textData,
                language = lang.Remove(2, 1),
                voice = voice
            });

            response = await result.GetStringAsync();

            return response;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            try
            {
                mediaPlayer.Close();
                DirectoryInfo directoryInfo = new DirectoryInfo("./tempAudio");
                foreach (var file in directoryInfo.GetFiles())
                {
                    file.Delete();
                }
                Directory.Delete("./tempAudio");

            }
            catch (Exception)
            {
                //
            }
        }

        private void tbxFilePath_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                try
                {
                    FileInfo file = new FileInfo(tbxFilePath.Text);
                    imgFile.Source = new BitmapImage(new Uri(tbxFilePath.Text, UriKind.Absolute));
                    _filePath = tbxFilePath.Text;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _filePath = "";
                }
            }
            
        }
    }

    public class ResultJson
    {
        public string status { get; set; }
        public string text { get; set; }
        public string charactersText { get; set; }
        public string charactersUrl { get; set; }
        public string tokensText { get; set; }
        public string tokensUrl { get; set; }
        public string stressedText { get; set; }
        public string stressedUrl { get; set; }
        public string unknownText { get; set; }
        public string unknownUrl { get; set; }
        public string homographsText { get; set; }
        public string homographsUrl { get; set; }
        public string prosodicText { get; set; }
        public string prosodicUrl { get; set; }
        public string phonemicText { get; set; }
        public string phonemicUrl { get; set; }
        public string allophonicText { get; set; }
        public string allophonicUrl { get; set; }
        public string allophoneCharacteristicsText { get; set; }
        public string allophoneCharacteristicsUrl { get; set; }
        public string transcriptionCyr { get; set; }
        public string transcriptionLat { get; set; }
        public string transcriptionIPA { get; set; }
        public string transcriptionXSAMPA { get; set; }
        public string audio { get; set; }
    }

}
