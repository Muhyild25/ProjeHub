using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using System.Net.Http;
using System.Text.Json;

namespace ProjeHub.UI
{
    public partial class AddWindow : Window
    {
        // 💡 İŞTE PROFESYONEL DOKUNUŞ: 
        // Uygulama açık kaldığı sürece sadece 1 tane tarayıcı nesnesi yaşayacak.
        private static readonly HttpClient client = new HttpClient();

        public AddWindow()
        {
            InitializeComponent();
        }

        // İPTAL BUTONU
        private void BtnIptal_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // KAYDET (EKLE) BUTONU - XAML ile uyumlu olması için adı BtnEkle_Click oldu
        private async void BtnEkle_Click(object sender, RoutedEventArgs e)
        {
            string url = TxtUrl.Text; // Tasarımdaki TxtUrl'den alıyoruz
            string baslik = TxtBaslik.Text; // Tasarımdaki başlık kutusu
            string notlar = TxtNotlar.Text; // 📝 YENİ: NOTLAR KUTUSUNU ALIYORUZ!

            if (string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(baslik))
            {
                MessageBox.Show("Lütfen bir başlık veya link girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Button btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content = "Kaydediliyor...";

            try
            {
                // --- 1. AŞAMA: PYTHON'DAN LİNKİ KAZIMASINI İSTİYORUZ ---
                if (!string.IsNullOrWhiteSpace(url))
                {
                    var extractData = new { url = url };
                    string extractJson = JsonSerializer.Serialize(extractData);
                    StringContent extractIcerik = new StringContent(extractJson, Encoding.UTF8, "application/json");

                    // Kazıma kapısını çalıyoruz
                    HttpResponseMessage kazimaCevabi = await client.PostAsync("http://127.0.0.1:8000/extract-link/", extractIcerik);

                    if (kazimaCevabi.IsSuccessStatusCode)
                    {
                        string gelenJson = await kazimaCevabi.Content.ReadAsStringAsync();
                        using (JsonDocument doc = JsonDocument.Parse(gelenJson))
                        {
                            JsonElement root = doc.RootElement;
                            if (root.TryGetProperty("title", out JsonElement titleElement) && titleElement.ValueKind == JsonValueKind.String)
                            {
                                string cikanBaslik = titleElement.GetString();

                                // Eğer kullanıcı başlık yazmadıysa, kazınan başlığı kullan!
                                if (!string.IsNullOrEmpty(cikanBaslik) && string.IsNullOrWhiteSpace(baslik))
                                {
                                    baslik = cikanBaslik;
                                }
                            }
                        }
                    }
                }

                // Hâlâ boşsa varsayılan başlık ata
                if (string.IsNullOrWhiteSpace(baslik)) baslik = "Başlıksız Proje";

                string secilenKategori = ((ComboBoxItem)CmbKategori.SelectedItem).Content.ToString();

                // --- 2. AŞAMA: KAZINAN VERİYİ VERİTABANINA KAYDEDİYORUZ ---
                var kayitVerisi = new
                {
                    title = baslik,
                    url = url,
                    category = secilenKategori,
                    priority = "Orta",
                    status = "Yapılacak",
                    notes = notlar // 📝 NOTLAR BURADAN VERİTABANINA GİDİYOR!
                };

                string kayitJson = JsonSerializer.Serialize(kayitVerisi);
                StringContent kayitIcerik = new StringContent(kayitJson, Encoding.UTF8, "application/json");

                // Veritabanına yazması için /items/ kapısını çalıyoruz
                HttpResponseMessage kayitCevabi = await client.PostAsync("http://127.0.0.1:8000/items/", kayitIcerik);

                if (kayitCevabi.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Başlık: {baslik}\n\nKayıt başarıyla işlendi!", "Harika Haber", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show($"Kayıt Başarısız: {kayitCevabi.StatusCode}", "Veritabanı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Python sunucusuna ulaşılamadı. Motor açık mı?\nHata: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content = "Ekle";
            }
        }
    }
}