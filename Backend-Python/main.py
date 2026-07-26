from fastapi import FastAPI, Depends
from pydantic import BaseModel
from sqlalchemy.orm import Session
from typing import Optional
import datetime

# Veritabanı ve YENİ scraper dosyamızı içeri aktarıyoruz
from database import SessionLocal, HubItem
from scraper import fetch_link_metadata

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

# Kayıt ekleme veri modeli
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