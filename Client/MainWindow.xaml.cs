
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.IO;
using Contracts;
using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Linq;

namespace WPF
{
    public partial class MainWindow : Window
    {

        private ImageRecognizerVM imageRecognizer;
        private HubConnection connection; 
        
        public MainWindow()
        {
            connection = new HubConnectionBuilder()
                .WithUrl("http://localhost:5000/recognitionhub")
                .Build();
            connection.On<string, string>("RealTimeAdd", (title, path) =>
            {
                Trace.WriteLine($"{title} AAA {path}");
                lock (imageRecognizer.Recognitions)
                {
                    var l = (from pic in imageRecognizer.Recognitions
                             where pic.Title == title
                             select pic).FirstOrDefault();

                    var q = (from pic in imageRecognizer.Photos
                             where path == pic.Path
                             select pic).FirstOrDefault();
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (l == null) //first time 
                        {
                            imageRecognizer.Recognitions.Add(new Recognition
                            {
                                Title = title,
                                Count = 1,
                                Photos = new ObservableCollection<Photo> { q }
                            });
                        }
                        else
                        {
                            int index = imageRecognizer.Recognitions.IndexOf(l);
                            imageRecognizer.Recognitions[index].Count++;
                            imageRecognizer.Recognitions[index].Photos.Add(q);
                        }
                        imageRecognizer.ImagesCounter++;
                    });                    
                }
            });
            connection.StartAsync();

            imageRecognizer = new ImageRecognizerVM();
            InitializeComponent();                   
            DataContext = imageRecognizer;
        }

//===========================================================================================//
        
        private void OpenImages(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            dialog.ShowDialog();
            try
            {
                imageRecognizer.ImagesPath = dialog.FileName ?? imageRecognizer.ImagesPath;
            }
            catch { }
        }

        private void OpenOnnxModel(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Onnx Model (*.onnx)|*.onnx";
            openFileDialog.ShowDialog();
            try
            {
                imageRecognizer.OnnxModelPath = openFileDialog.FileName;
            }
            catch (Exception)
            {
                MessageBox.Show("Файл не выбран.", "Ошибка");
            }
        }

//===========================================================================================//

        private async void Control(object sender, ExecutedRoutedEventArgs e)
        {
            if (!imageRecognizer.IsRunning) //start recognition
            {
                try
                {
                    await imageRecognizer.StartAsync();
                }

                catch (DirectoryNotFoundException s)
                {
                    MessageBox.Show($"{s.Message}", "Ошибка");
                }
                catch (Exception s)
                {
                    MessageBox.Show($"{s.Message}", "Ошибка");
                }
                finally
                {
                    imageRecognizer.IsRunning = false;
                    imageRecognizer.IsStopping = false;
                }

            }
            else //stop recognition
            {
                try
                {
                    await imageRecognizer.StopAsync();
                }
                catch (Exception s)
                {
                    MessageBox.Show($"{s.Message}", "Ошибка");
                }
                finally
                {
                    imageRecognizer.IsRunning = false;
                    imageRecognizer.IsStopping = false;
                }
            }
        }
        private async void ClearStorage(object sender, ExecutedRoutedEventArgs e)
        {
            PictiresPanel.DataContext = null;
            try
            {
                await imageRecognizer.ClearAsync();
            }
            catch (Exception s)
            {
                MessageBox.Show($"{s.Message}", "Ошибка");
            }
            finally
            {
                imageRecognizer.IsRunning = false;
                imageRecognizer.IsStopping = false;
            }
        }
        private async void LoadImages(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                await imageRecognizer.LoadAsync();
            }
            catch (Exception s)
            {
                MessageBox.Show($"{s.Message}", "Ошибка");
            }
            finally
            {
                imageRecognizer.IsRunning = false;
                imageRecognizer.IsStopping = false;
            }
        }
//===========================================================================================//

        private void ListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((Recognition)Labels.SelectedItem != null)
            {
                PictiresPanel.DataContext = (Recognition)Labels.SelectedItem;
            }
        }



//===========================================================================================//
    }
}

