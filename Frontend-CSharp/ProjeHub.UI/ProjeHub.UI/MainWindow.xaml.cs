using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;

namespace ProjeHub.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // 1. AddWindow'da yaptığımız gibi tek ve kalıcı bir internet tarayıcısı tanımlıyoruz
        private static readonly HttpClient client = new HttpClient();
        public MainWindow()
        {
            InitializeComponent();

            // 2. Pencere açılır açılmaz verileri çekme motorunu çalıştırıyoruz
            VerileriGetir();

        }


        // 3. PYTHON'DAN VERİLERİ ÇEKEN O SİHİRLİ METOT
        private async void VerileriGetir()
        {
            try
            {
                // Python'daki listeleme kapımıza (GET /items/) istek atıyoruz
                HttpResponseMessage cevap = await client.GetAsync("http://127.0.0.1:8000/items/");

                if (cevap.IsSuccessStatusCode)
                {
                    // Python'dan gelen JSON metnini alıyoruz
                    string jsonVeri = await cevap.Content.ReadAsStringAsync();

                    // JSON metnini bizim oluşturduğumuz 'HubItem' listesine dönüştürüyoruz
                    List<HubItem> projeler = JsonSerializer.Deserialize<List<HubItem>>(jsonVeri);

                    // VE BÜYÜLÜ AN: Listeyi arayüzdeki 'ProjeListesi' isimli tabloya bağlıyoruz!
                    ProjeListesi.ItemsSource = projeler;
                }
                else
                {
                    MessageBox.Show($"Veriler çekilemedi: {cevap.StatusCode}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Python sunucusuna bağlanılamadı. Terminalde motor (uvicorn) açık mı?\nHata: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void BtnYeniEkle_Click(object sender, RoutedEventArgs e)
        {
            // Tasarladığımız pencereden yeni bir nesne (örnek) oluşturuyoruz
            AddWindow eklemePenceresi = new AddWindow();



            // ShowDialog() kullanarak pencereyi açıyoruz. 
            // ShowDialog'un farkı: Bu küçük pencere kapanmadan arkadaki ana ekrana tıklanmasına izin vermez.
            eklemePenceresi.ShowDialog();

            // Kullanıcı pencereyi (ve mesaj kutusunu) kapattığı anda listeyi otomatik yenile!
            VerileriGetir();

        }




    }
}