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

        // DİKKAT: Parametrelere 'durum' eklendi!
        public EditWindow(string id, string baslik, string kategori, string url, string notlar, string durum)
        {
            InitializeComponent();
            projeId = id;
            projeUrl = url;

            TxtBaslik.Text = baslik;

            if (!string.IsNullOrEmpty(notlar))
            {
                TxtNotlar.Text = notlar;
            }

            foreach (ComboBoxItem item in CmbKategori.Items)
            {
                if (item.Content.ToString() == kategori)
                {
                    CmbKategori.SelectedItem = item;
                    break;
                }
            }

            // Mevcut durumu kutuda seçili hale getir
            foreach (ComboBoxItem item in CmbDurum.Items)
            {
                if (item.Content.ToString() == durum)
                {
                    CmbDurum.SelectedItem = item;
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
            string yeniDurum = ((ComboBoxItem)CmbDurum.SelectedItem).Content.ToString(); // YENİ SEÇİLEN DURUM
            string yeniNotlar = TxtNotlar.Text;

            var guncelVeri = new
            {
                title = yeniBaslik,
                url = projeUrl,
                category = yeniKategori,
                priority = "Orta",
                status = yeniDurum, // ARTIK DURUM SABİT DEĞİL, KUTUDAN GİDİYOR!
                notes = yeniNotlar
            };

            string json = JsonSerializer.Serialize(guncelVeri);
            StringContent icerik = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                BtnKaydet.IsEnabled = false;
                BtnKaydet.Content = "Kaydediliyor...";

                HttpResponseMessage cevap = await client.PutAsync($"http://127.0.0.1:8000/items/{projeId}", icerik);

                if (cevap.IsSuccessStatusCode)
                {
                    MessageBox.Show("Proje başarıyla güncellendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
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