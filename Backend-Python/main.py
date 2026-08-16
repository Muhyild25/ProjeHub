from fastapi import FastAPI, Depends
from pydantic import BaseModel
from sqlalchemy.orm import Session
from typing import Optional
import datetime

# Veritabanı ve YENİ scraper dosyamızı içeri aktarıyoruz
from database import SessionLocal, HubItem
from scraper import fetch_link_metadata

from fastapi import HTTPException

app = FastAPI(title="ProjeHub API")

def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()

# Linkten otomatik başlık çekmek için gereken veri modeli
class LinkRequest(BaseModel):
    url: str

# Kayıt ekleme ve GÜNCELLEME için kullanılacak veri modeli (Pydantic)
class ItemCreate(BaseModel):
    title: str
    url: Optional[str] = None
    category: str
    priority: Optional[str] = "Orta"
    status: Optional[str] = "Yapılacak"
    notes: Optional[str] = None

# --- 1. YENİ EKLENEN KISIM: Link Kazıyıcı Ucu (Scraper) ---
@app.post("/extract-link/")
def extract_link(request: LinkRequest):
    # Dışarıdan gelen linki alıp scraper botumuza gönderiyoruz
    result = fetch_link_metadata(request.url)
    return result

# --- 2. ESKİ KISIMLAR (Kayıt Ekleme ve Listeleme) ---
@app.post("/items/")
def create_item(item: ItemCreate, db: Session = Depends(get_db)):
    db_item = HubItem(
        title=item.title,
        url=item.url,
        category=item.category,
        priority=item.priority,
        status=item.status,
        notes=item.notes
    )
    db.add(db_item)
    db.commit()
    db.refresh(db_item)
    return {"mesaj": "Kayıt başarıyla eklendi!", "data": db_item}

@app.get("/items/")
def read_items(db: Session = Depends(get_db)):
    items = db.query(HubItem).all()
    return items

@app.delete("/items/{item_id}")
def delete_item(item_id: int, db: Session = Depends(get_db)):
    # Önce veritabanında bu ID'ye sahip kaydı bul
    item = db.query(HubItem).filter(HubItem.id == item_id).first()
    
    # Eğer bulamazsa 404 (Bulunamadı) hatası döndür
    if not item:
        raise HTTPException(status_code=404, detail="Kayıt bulunamadı")
    
    # Bulursa veritabanından sil ve onayla
    db.delete(item)
    db.commit()
    return {"message": "Kayıt başarıyla silindi"}

# --- 3. GÜNCELLEME (UPDATE) İŞLEMİ KAPISI ---
@app.put("/items/{item_id}")
def update_item(item_id: int, item_update: ItemCreate, db: Session = Depends(get_db)):
    # 1. Önce veritabanında o ID'ye sahip projeyi bul
    db_item = db.query(HubItem).filter(HubItem.id == item_id).first()
    
    # 2. Eğer bulamazsa hata ver
    if db_item is None:
        raise HTTPException(status_code=404, detail="Proje bulunamadı")
    
    # 3. Bulursa, C#'tan gelen yeni verileri eski verilerin üzerine yaz
    db_item.title = item_update.title
    db_item.url = item_update.url
    db_item.category = item_update.category
    db_item.priority = item_update.priority
    db_item.status = item_update.status
    db_item.notes = item_update.notes
    
    # 4. Değişiklikleri kaydet
    db.commit()
    db.refresh(db_item)
    
    return db_item