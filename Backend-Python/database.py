from sqlalchemy import create_engine, Column, Integer, String, DateTime, Text
from sqlalchemy.orm import declarative_base, sessionmaker
import datetime

# SQLite veritabanı dosyamızın yolu (Proje çalışınca projehub.db adında bir dosya oluşacak)
SQLALCHEMY_DATABASE_URL = "sqlite:///./projehub.db"

# Veritabanı motorunu (Engine) başlatıyoruz
engine = create_engine(
    SQLALCHEMY_DATABASE_URL, connect_args={"check_same_thread": False}
)

# Veritabanı ile konuşmamızı sağlayacak oturum (Session) yöneticisi
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)

# Tablolarımızı türeteceğimiz temel sınıf
Base = declarative_base()

# Ana Veri Tablomuz: Kaydedeceğin her link, proje fikri veya not bu formata girecek
class HubItem(Base):
    __tablename__ = "hub_items"

    id = Column(Integer, primary_key=True, index=True)
    title = Column(String, index=True)              # İçeriğin Başlığı
    url = Column(String, nullable=True)             # İnternet linki (Eğer bir not ise boş kalabilir)
    category = Column(String, index=True)           # Kategori: Proje Fikri, Staj, Eğitim, Not
    priority = Column(String, default="Orta")       # Öncelik Sırası: Yüksek, Orta, Düşük
    status = Column(String, default="Yapılacak")    # Durum: Yapılacak, Devam Ediyor, Tamamlandı
    notes = Column(Text, nullable=True)             # Senin ekleyeceğin özel notlar
    due_date = Column(DateTime, nullable=True)      # Bitiş tarihi (Staj başvuruları için)
    created_at = Column(DateTime, default=datetime.datetime.utcnow) # Sisteme eklenme zamanı

# Yukarıda tasarladığımız tabloyu veritabanında fiziksel olarak oluşturur
Base.metadata.create_all(bind=engine)