import requests
from bs4 import BeautifulSoup

def fetch_link_metadata(url: str):
    """
    Verilen URL'ye gider, sitenin başlığını (title) otomatik çeker.
    """
    # Bazı siteler botları engellediği için kendimizi gerçek bir tarayıcı gibi tanıtıyoruz
    headers = {
        "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36"
    }
    
    try:
        # Siteye istek atıyoruz
        response = requests.get(url, headers=headers, timeout=5)
        
        # Eğer site başarılı bir şekilde açıldıysa (Durum kodu 200 ise)
        if response.status_code == 200:
            soup = BeautifulSoup(response.text, 'html.parser')
            
            # Sitenin <title> etiketini bul ve içindeki metni al
            title_tag = soup.find('title')
            title = title_tag.text.strip() if title_tag else "Başlık Bulunamadı"
            
            return {"success": True, "title": title}
        else:
            return {"success": False, "title": "Siteye erişilemedi"}
            
    except Exception as e:
        return {"success": False, "title": "Hatalı veya bozuk link"}