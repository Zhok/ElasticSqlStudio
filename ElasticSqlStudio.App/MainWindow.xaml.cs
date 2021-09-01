using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using ElasticSqlStudio.App.Annotations;
using ElasticSqlStudio.Data;

using Microsoft.Win32;

namespace ElasticSqlStudio.App
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string url = "http://localhost:9200";

        private string user = "none";

        private string query = "SELECT clientip, request FROM kibana_sample_data_logs";

        private string output;

        private bool isValid;

        private bool hasConnection;

        private ElasticRepository elasticRepository;

        private Visibility progressBarVisibility = Visibility.Collapsed;

        public MainWindow()
        {
            this.DataContext = this;
            InitializeComponent();
        }

        public string Url
        {
            get => this.url;
            set
            {
                if (value == this.url) return;
                this.url = value;
                this.OnPropertyChanged();
            }
        }

        public string User
        {
            get => this.user;
            set
            {
                if (value == this.user) return;
                this.user = value;
                this.OnPropertyChanged();
            }
        }

        public string Password
        {
            get => this.PasswordTextBox.Password;
            set
            {
                if (value == this.PasswordTextBox.Password) return;
                this.PasswordTextBox.Password = value;
                this.OnPropertyChanged();
            }
        }

        public string Query
        {
            get => this.query;
            set
            {
                if (value == this.query) return;
                this.query = value;
                this.OnPropertyChanged();
            }
        }

        public string Output
        {
            get => this.output;
            set
            {
                this.output = $"---------------------- {DateTime.Now} ---------------------- {Environment.NewLine}{value}";
                this.OnPropertyChanged();
            }
        }

        public bool HasConnection
        {
            get => this.hasConnection;
            set
            {
                if (value == this.hasConnection) return;
                this.hasConnection = value;
                this.OnPropertyChanged();
            }
        }

        public bool IsValid
        {
            get => this.isValid;
            set
            {
                if (value == this.isValid) return;
                this.isValid = value;
                this.OnPropertyChanged();
            }
        }

        public Visibility ProgressBarVisibility
        {
            get => this.progressBarVisibility;
            set
            {
                if (value == this.progressBarVisibility) return;
                this.progressBarVisibility = value;
                this.OnPropertyChanged();
            }
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(
                async () =>
                    {
                        this.ProgressBarVisibility = Visibility.Visible;
                        try
                        {
                            this.elasticRepository = new ElasticRepository(this.Url, this.User, this.Password);
                            await this.elasticRepository.PingElastic();
                            this.Output = "Connection valid";
                            this.HasConnection = true;
                        }
                        catch (Exception exception)
                        {
                            this.Output = exception.ToString();
                        }

                        this.ProgressBarVisibility = Visibility.Collapsed;
                    });
        }

        private async void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(
                async () =>
                    {
                        this.ProgressBarVisibility = Visibility.Visible;
                        try
                        {
                            var result = await this.elasticRepository.RunSingleQuery(this.Query);
                            this.Output = $"Valid Request. First row: {result}";
                            this.IsValid = true;
                        }
                        catch (Exception exception)
                        {
                            this.Output = exception.ToString();
                        }

                        this.ProgressBarVisibility = Visibility.Collapsed;
                    });
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(
                async () =>
                    {
                        this.ProgressBarVisibility = Visibility.Visible;
                        try
                        {
                            var saveFileDialog = new SaveFileDialog();
                            saveFileDialog.FileName = $"{DateTime.Now.ToFileTimeUtc()}.csv";
                            if (saveFileDialog.ShowDialog() == true)
                            {
                                var result = await this.elasticRepository.QueryComplete(this.Query);
                                await File.WriteAllTextAsync(saveFileDialog.FileName, result);
                                this.Output = $"Exportet to {saveFileDialog.FileName}";
                            }
                        }
                        catch (Exception exception)
                        {
                            this.Output = exception.ToString();
                        }

                        this.ProgressBarVisibility = Visibility.Collapsed;
                    });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
