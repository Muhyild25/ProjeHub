using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace ProjeHub.UI
{
    public partial class EditWindow : Window
    {
        private string projeId;
        private string projeUrl;
        private static readonly HttpClient client = new HttpClient();

        // Pencere açılırken ID, Başlık, Kategori ve URL'yi dışarıdan alıyoruz
        public EditWindow(string id, string baslik, string kategori, string url)
        {
            InitializeComponent();
            projeId = id;
            projeUrl = url;

            // Kutulara mevcut bilgileri dolduruyoruz
            TxtBaslik.Text = baslik;

            foreach (ComboBoxItem item in CmbKategori.Items)
            {
                if (item.Content.ToString() == kategori)
                {
                    CmbKategori.SelectedItem = item;
                    break;
                }
            }
        }

        private void BtnIptal_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void BtnKaydet_Click(object sender, RoutedEventArgs e)
        {
            string yeniBaslik = TxtBaslik.Text;
            string yeniKategori = ((ComboBoxItem)CmbKategori.SelectedItem).Content.ToString();

            // Güncellenmiş veri paketi
            var guncelVeri = new
            {
                title = yeniBaslik,
                url = projeUrl,
                category = yeniKategori,
                priority = "Orta",
                status = "Yapılacak",
                notes = ""
            };

            string json = JsonSerializer.Serialize(guncelVeri);
            StringContent icerik = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                BtnKaydet.IsEnabled = false;
                BtnKaydet.Content = "Kaydediliyor...";

                // Python sunucusuna (Veritabanına) PUT isteği (Güncelleme) atıyoruz
                HttpResponseMessage cevap = await client.PutAsync($"http://127.0.0.1:8000/items/{projeId}", icerik);

                if (cevap.IsSuccessStatusCode)
                {
                    MessageBox.Show("Proje başarıyla güncellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close(); // Pencereyi kapat
                }
                else
                {
                    MessageBox.Show($"Güncelleme başarısız: {cevap.StatusCode}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Sunucuya ulaşılamadı: {ex.Message}", "Bağlantı Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnKaydet.IsEnabled = true;
                BtnKaydet.Content = "Kaydet";
            }
        }
    }
}