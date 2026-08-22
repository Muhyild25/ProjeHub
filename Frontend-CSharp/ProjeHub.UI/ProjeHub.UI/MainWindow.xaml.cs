using System;
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
    public partial class MainWindow : Window
    {
        private static readonly HttpClient client = new HttpClient();

        private List<HubItem> tumProjeler = new List<HubItem>(); // YENİ: Hafıza listemiz

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                if (System.IO.File.Exists("tema.txt"))
                {
                    string kayitliRenk = System.IO.File.ReadAllText("tema.txt");
                    var cevirici = new System.Windows.Media.BrushConverter();
                    BtnYeniEkle.Background = (System.Windows.Media.Brush)cevirici.ConvertFromString(kayitliRenk);
                }
            }
            catch { }

            VerileriGetir();
        }

        private async void VerileriGetir()
        {
            try
            {
                HttpResponseMessage cevap = await client.GetAsync("http://127.0.0.1:8000/items/");

                if (cevap.IsSuccessStatusCode)
                {
                    string jsonVeri = await cevap.Content.ReadAsStringAsync();

                    // JSON'dan gelen veriyi ana hafıza listemize atıyoruz
                    tumProjeler = JsonSerializer.Deserialize<List<HubItem>>(jsonVeri);

                    // Listeyi ekrana bağlıyoruz
                    ProjeListesi.ItemsSource = tumProjeler;
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
            AddWindow eklemePenceresi = new AddWindow();
            eklemePenceresi.ShowDialog();
            VerileriGetir();
        }

        private void BtnZiyaretEt_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                string url = btn.Tag.ToString();
                try
                {
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
                            VerileriGetir();
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

        // 💡 DÜZENLE BUTONUNA DURUM (STATUS) PARAMETRESİ EKLENDİ
        private void BtnDuzenle_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.DataContext != null)
            {
                HubItem secilenProje = btn.DataContext as HubItem;
                if (secilenProje != null)
                {
                    EditWindow duzenlePenceresi = new EditWindow(
                        secilenProje.id.ToString(),
                        secilenProje.title,
                        secilenProje.category,
                        secilenProje.url,
                        secilenProje.notes,
                        secilenProje.status // BUNU EKLİYORUZ!
                    );

                    duzenlePenceresi.ShowDialog();
                    VerileriGetir();
                }
            }
        }

        private async void BtnFiltre_Click(object sender, RoutedEventArgs e)
        {
            AnaEkranPaneli.Visibility = Visibility.Visible;
            AyarlarPaneli.Visibility = Visibility.Collapsed;

            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                string secilenKategori = btn.Tag.ToString();
                try
                {
                    HttpResponseMessage cevap = await client.GetAsync("http://127.0.0.1:8000/items/");
                    if (cevap.IsSuccessStatusCode)
                    {
                        string jsonVeri = await cevap.Content.ReadAsStringAsync();
                        List<HubItem> tumKayitlar = JsonSerializer.Deserialize<List<HubItem>>(jsonVeri);

                        if (secilenKategori == "Hepsi")
                        {
                            ProjeListesi.ItemsSource = tumKayitlar;
                        }
                        else
                        {
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

        private void BtnAyarlar_Click(object sender, RoutedEventArgs e)
        {
            AnaEkranPaneli.Visibility = Visibility.Collapsed;
            AyarlarPaneli.Visibility = Visibility.Visible;
        }

        private void BtnTema_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                string secilenRenkHex = btn.Tag.ToString();
                try
                {
                    var cevirici = new System.Windows.Media.BrushConverter();
                    var firca = (System.Windows.Media.Brush)cevirici.ConvertFromString(secilenRenkHex);
                    BtnYeniEkle.Background = firca;
                    System.IO.File.WriteAllText("tema.txt", secilenRenkHex);
                    MessageBox.Show("Tema rengi başarıyla güncellendi!", "Tema Değişti", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Renk değiştirilirken hata oluştu: {ex.Message}");
                }
            }
        }

        // 🔍 CANLI ARAMA (LIVE SEARCH) METODU
        private void TxtArama_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Kullanıcının yazdığı metni alıp küçük harfe çeviriyoruz (Büyük/küçük harf duyarlılığını kaldırmak için)
            string arananKelime = TxtArama.Text.ToLower();

            // Eğer arama kutusu boşsa, hafızadaki tüm listeyi geri yükle
            if (string.IsNullOrWhiteSpace(arananKelime))
            {
                ProjeListesi.ItemsSource = tumProjeler;
            }
            else
            {
                // LINQ büyüsü: Başlığında VEYA Notlarında aranan kelime geçenleri süzgeçten geçir!
                var filtrelenmis = tumProjeler.Where(p =>
                    (p.title != null && p.title.ToLower().Contains(arananKelime)) ||
                    (p.notes != null && p.notes.ToLower().Contains(arananKelime))
                ).ToList();

                // Çıkan sonucu ekrana bas
                ProjeListesi.ItemsSource = filtrelenmis;
            }
        }


    }
}