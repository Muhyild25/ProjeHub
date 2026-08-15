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

        // KAYDET BUTONU
        private async void BtnKaydet_Click(object sender, RoutedEventArgs e)
        {
            string url = txtUrl.Text;

            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("Lütfen bir link girin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Button btn = (Button)sender;
            btn.IsEnabled = false;
            btn.Content = "Kaydediliyor...";

            try
            {
                // Artık "using (HttpClient...)" satırına ihtiyacımız YOK! 
                // Direkt en yukarıdaki kalıcı 'client' nesnesini kullanıyoruz.

                // --- 1. AŞAMA: PYTHON'DAN LİNKİ KAZIMASINI İSTİYORUZ ---
                var extractData = new { url = url };
                string extractJson = JsonSerializer.Serialize(extractData);
                StringContent extractIcerik = new StringContent(extractJson, Encoding.UTF8, "application/json");

                // Kazıma kapısını çalıyoruz
                HttpResponseMessage kazimaCevabi = await client.PostAsync("http://127.0.0.1:8000/extract-link/", extractIcerik);

                if (kazimaCevabi.IsSuccessStatusCode)
                {
                    string gelenJson = await kazimaCevabi.Content.ReadAsStringAsync();
                    string baslik = "Başlıksız Proje";

                    using (JsonDocument doc = JsonDocument.Parse(gelenJson))
                    {
                        JsonElement root = doc.RootElement;
                        if (root.TryGetProperty("title", out JsonElement titleElement) && titleElement.ValueKind == JsonValueKind.String)
                        {
                            string cikanBaslik = titleElement.GetString();
                            if (!string.IsNullOrEmpty(cikanBaslik)) baslik = cikanBaslik;
                        }
                    }


                    string secilenKategori = ((ComboBoxItem)CmbKategori.SelectedItem).Content.ToString();

                    // --- 2. AŞAMA: KAZINAN VERİYİ VERİTABANINA KAYDEDİYORUZ ---
                    var kayitVerisi = new
                    {
                        title = baslik,
                        url = url,
                        category = secilenKategori, // ARTIK DİNAMİK OLDU!
                        priority = "Orta",
                        status = "Yapılacak",
                        notes = ""
                    };

                    string kayitJson = JsonSerializer.Serialize(kayitVerisi);
                    StringContent kayitIcerik = new StringContent(kayitJson, Encoding.UTF8, "application/json");

                    // Veritabanına yazması için /items/ kapısını çalıyoruz
                    HttpResponseMessage kayitCevabi = await client.PostAsync("http://127.0.0.1:8000/items/", kayitIcerik);

                    if (kayitCevabi.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Başlık: {baslik}\n\nLink başarıyla veritabanına işlendi!", "Harika Haber", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show($"Kayıt Başarısız: {kayitCevabi.StatusCode}", "Veritabanı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Link kazınamadı: {kazimaCevabi.StatusCode}", "Scraper Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Python sunucusuna ulaşılamadı. Motor açık mı?\nHata: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content = "Kaydet";
            }
        }
    }
}