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

using System.Linq;

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

            // UYGULAMA AÇILIRKEN KAYITLI TEMAYI YÜKLE
            try
            {
                if (System.IO.File.Exists("tema.txt"))
                {
                    string kayitliRenk = System.IO.File.ReadAllText("tema.txt");
                    var cevirici = new System.Windows.Media.BrushConverter();
                    BtnYeniEkle.Background = (System.Windows.Media.Brush)cevirici.ConvertFromString(kayitliRenk);
                }
            }
            catch { /* Dosya bozuksa veya hata varsa varsayılan Mavi renkte kalır, sorun yok */ }

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



        // ZİYARET ET BUTONU TIKLANINCA ÇALIŞACAK KOD
        private void BtnZiyaretEt_Click(object sender, RoutedEventArgs e)
        {
            // Hangi butona tıklandığını bul
            Button btn = sender as Button;

            // Eğer butonun cebinde (Tag) bir link varsa...
            if (btn != null && btn.Tag != null)
            {
                string url = btn.Tag.ToString();

                try
                {
                    // Modern .NET uygulamalarında varsayılan tarayıcıyı açma kodu
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Link açılamadı: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        // SİL BUTONU TIKLANINCA ÇALIŞACAK KOD
        private async void BtnSil_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            if (btn != null && btn.Tag != null)
            {
                string id = btn.Tag.ToString();

                MessageBoxResult sonuc = MessageBox.Show("Bu projeyi silmek istediğinize emin misiniz?", "Silme Onayı", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (sonuc == MessageBoxResult.Yes)
                {
                    try
                    {
                        HttpResponseMessage cevap = await client.DeleteAsync($"http://127.0.0.1:8000/items/{id}");

                        if (cevap.IsSuccessStatusCode)
                        {
                            VerileriGetir(); // Listeyi yenile
                        }
                        else
                        {
                            MessageBox.Show($"Silme başarısız oldu: {cevap.StatusCode}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Sunucuya bağlanılamadı:\n{ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        // DÜZENLE BUTONUNA TIKLANINCA ÇALIŞACAK KOD
        private void BtnDuzenle_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            if (btn != null && btn.DataContext != null)
            {
                // Tıklanan kartın içindeki veriyi (HubItem) komple alıyoruz! (WPF Büyüsü ✨)
                HubItem secilenProje = btn.DataContext as HubItem;

                if (secilenProje != null)
                {
                    // Düzenleme penceresini açarken mevcut bilgileri içine gönderiyoruz
                    EditWindow duzenlePenceresi = new EditWindow(
                        secilenProje.id.ToString(),
                        secilenProje.title,
                        secilenProje.category,
                        secilenProje.url
                    );

                    duzenlePenceresi.ShowDialog();

                    // Düzenleme penceresi kapanınca ana listeyi otomatik yenile
                    VerileriGetir();
                }
            }
        }

        // SOL MENÜ FİLTRELEME İŞLEMİ
        private async void BtnFiltre_Click(object sender, RoutedEventArgs e)
        {

            AnaEkranPaneli.Visibility = Visibility.Visible;
            AyarlarPaneli.Visibility = Visibility.Collapsed;


            Button btn = sender as Button;

            if (btn != null && btn.Tag != null)
            {
                string secilenKategori = btn.Tag.ToString(); // "Hepsi", "Proje" veya "Link" gelecek

                try
                {
                    // Önce veritabanındaki en güncel listeyi çekiyoruz
                    HttpResponseMessage cevap = await client.GetAsync("http://127.0.0.1:8000/items/");

                    if (cevap.IsSuccessStatusCode)
                    {
                        string jsonVeri = await cevap.Content.ReadAsStringAsync();
                        List<HubItem> tumKayitlar = JsonSerializer.Deserialize<List<HubItem>>(jsonVeri);

                        // Eğer "Ana Sayfa" butonuna (Tag="Hepsi") basıldıysa tüm listeyi ver
                        if (secilenKategori == "Hepsi")
                        {
                            ProjeListesi.ItemsSource = tumKayitlar;
                        }
                        else
                        {
                            // "Projelerim" veya "Linkler" butonuna basıldıysa listeyi o kelimeye göre filtrele!
                            // LINQ'in Where metodu ile saniyeler içinde ayıklıyoruz
                            var filtrelenmisListe = tumKayitlar.Where(x => x.category != null && x.category.Contains(secilenKategori)).ToList();

                            ProjeListesi.ItemsSource = filtrelenmisListe;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Filtreleme sırasında hata oluştu: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        // AYARLAR BUTONUNA TIKLANINCA
        private void BtnAyarlar_Click(object sender, RoutedEventArgs e)
        {
            // Ana ekranı gizle, ayarlar panelini göster
            AnaEkranPaneli.Visibility = Visibility.Collapsed;
            AyarlarPaneli.Visibility = Visibility.Visible;
        }

        // TEMA RENGİ DEĞİŞTİRME İŞLEMİ
        private void BtnTema_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                string secilenRenkHex = btn.Tag.ToString(); // Örneğin: "#2EA043"

                try
                {
                    // Hex kodunu C#'ın anlayacağı fırçaya (Brush) çeviriyoruz
                    var cevirici = new System.Windows.Media.BrushConverter();
                    var firca = (System.Windows.Media.Brush)cevirici.ConvertFromString(secilenRenkHex);

                    // Yeni Ekle butonunun rengini değiştiriyoruz
                    BtnYeniEkle.Background = firca;

                    System.IO.File.WriteAllText("tema.txt", secilenRenkHex);

                    // İstersen burada uygulamanın genel arka planını veya menü yazılarını da değiştirebilirsin
                    // Şimdilik sadece vurgu rengimiz olan Yeni Ekle butonunu değiştirdik.

                    MessageBox.Show("Tema rengi başarıyla güncellendi!", "Tema Değişti", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Renk değiştirilirken hata oluştu: {ex.Message}");
                }
            }
        }



    }
}