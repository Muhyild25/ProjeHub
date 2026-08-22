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

        // 💡 HAFIZA LİSTESİ BURADA
        private List<HubItem> tumProjeler = new List<HubItem>();

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

        // 📊 İSTATİSTİKLERİ GÜNCELLEYEN METOT
        private void IstatistikleriGuncelle(List<HubItem> gosterilenListe)
        {
            if (gosterilenListe == null) return;

            int toplam = gosterilenListe.Count;
            int biten = gosterilenListe.Count(x => x.status == "Bitti");
            int devamEden = gosterilenListe.Count(x => x.status == "Devam Ediyor");

            TxtToplam.Text = toplam.ToString();
            TxtBiten.Text = biten.ToString();
            TxtDevam.Text = devamEden.ToString();
        }

        private async void VerileriGetir()
        {
            try
            {
                HttpResponseMessage cevap = await client.GetAsync("http://127.0.0.1:8000/items/");

                if (cevap.IsSuccessStatusCode)
                {
                    string jsonVeri = await cevap.Content.ReadAsStringAsync();

                    // Veriyi hafızaya al
                    tumProjeler = JsonSerializer.Deserialize<List<HubItem>>(jsonVeri);

                    // Ekrana bas
                    ProjeListesi.ItemsSource = tumProjeler;

                    // İstatistikleri güncelle
                    IstatistikleriGuncelle(tumProjeler);
                }
                else
                {
                    MessageBox.Show($"Veriler çekilemedi: {cevap.StatusCode}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Python sunucusuna bağlanılamadı.\nHata: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 🔍 CANLI ARAMA (LIVE SEARCH)
        private void TxtArama_TextChanged(object sender, TextChangedEventArgs e)
        {
            string arananKelime = TxtArama.Text.ToLower();

            if (string.IsNullOrWhiteSpace(arananKelime))
            {
                ProjeListesi.ItemsSource = tumProjeler;
                IstatistikleriGuncelle(tumProjeler);
            }
            else
            {
                var filtrelenmis = tumProjeler.Where(p =>
                    (p.title != null && p.title.ToLower().Contains(arananKelime)) ||
                    (p.notes != null && p.notes.ToLower().Contains(arananKelime))
                ).ToList();

                ProjeListesi.ItemsSource = filtrelenmis;
                IstatistikleriGuncelle(filtrelenmis);
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
                    MessageBox.Show($"Link açılamadı: {ex.Message}");
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
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Hata: {ex.Message}");
                    }
                }
            }
        }

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
                        secilenProje.status
                    );

                    duzenlePenceresi.ShowDialog();
                    VerileriGetir();
                }
            }
        }

        private void BtnFiltre_Click(object sender, RoutedEventArgs e)
        {
            AnaEkranPaneli.Visibility = Visibility.Visible;
            AyarlarPaneli.Visibility = Visibility.Collapsed;

            Button btn = sender as Button;
            if (btn != null && btn.Tag != null)
            {
                string secilenKategori = btn.Tag.ToString();

                if (secilenKategori == "Hepsi")
                {
                    ProjeListesi.ItemsSource = tumProjeler;
                    IstatistikleriGuncelle(tumProjeler);
                }
                else
                {
                    var filtrelenmisListe = tumProjeler.Where(x => x.category != null && x.category.Contains(secilenKategori)).ToList();
                    ProjeListesi.ItemsSource = filtrelenmisListe;
                    IstatistikleriGuncelle(filtrelenmisListe);
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
                catch { }
            }
        }
    }
}