# User Ratings & Database - Uitleg

## Database Locatie

### Waar staat de database?

De SQLite database wordt aangemaakt op de volgende locatie:

**Lokaal (Development):**
```
{Project Root}/data/user_ratings.db
```

**Azure App Service:**
```
D:\home\site\wwwroot\data\user_ratings.db
```

**Configuratie:**
Je kunt de database locatie aanpassen in `appsettings.json`:
```json
{
  "DatabaseSettings": {
    "RatingsDatabasePath": "data/user_ratings.db"
  }
}
```

### Database Bestand

- **Bestandsnaam**: `user_ratings.db`
- **Type**: SQLite database (lokaal bestand)
- **Automatisch aangemaakt**: Ja, bij eerste gebruik
- **Tabel**: `UserRatings`

### Database Inhoud

```sql
CREATE TABLE UserRatings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CarId INTEGER NOT NULL,
    Rating INTEGER NOT NULL CHECK (Rating >= 1 AND Rating <= 5),
    UserId TEXT NOT NULL,
    OriginalPrompt TEXT,
    UserPreferencesJson TEXT,
    RecommendationContext TEXT,
    Timestamp DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
)
```

### Database Bekijken

Je kunt de database bekijken met:
- **DB Browser for SQLite** (gratis tool)
- **Visual Studio** (SQLite tools)
- **Command line**: `sqlite3 data/user_ratings.db`

---

## User IDs Zonder Inlog Systeem

### Hoe werkt het nu?

**GEEN ECHTE AUTHENTICATION NODIG!** Het systeem gebruikt **session-based user tracking**:

#### Optie 1: Session-based (Aanbevolen)
- Elke browser krijgt een **session ID** (automatisch via cookies)
- Session ID wordt gekoppeld aan een **user ID** (GUID)
- Zelfde browser = zelfde user ID (zolang session actief is)
- **Geen inlog nodig!**

#### Optie 2: GUID per Request
- Als er geen session is, wordt een nieuwe GUID gemaakt
- Elke rating krijgt een nieuwe user ID
- **Minder ideaal** voor collaborative filtering

### Voorbeeld Flow

```
1. GEBRUIKER OPENT WEBSITE
   → Browser krijgt automatisch session cookie
   → Session ID: "abc123"
   ↓
2. SESSION SERVICE
   → Koppelt session ID aan user ID
   → User ID: "550e8400-e29b-41d4-a716-446655440000"
   ↓
3. GEBRUIKER RATE AUTO
   → POST /api/ratings
   → UserId wordt automatisch bepaald uit session
   → Rating opgeslagen met deze user ID
   ↓
4. VOLGENDE REQUEST
   → Zelfde session = zelfde user ID
   → Alle ratings van deze gebruiker blijven gekoppeld
```

### Waarom Geen Inlog?

**Voor collaborative filtering heb je GEEN echte user accounts nodig:**

✅ **Wat je WEL nodig hebt:**
- Consistente user ID per browser session
- Preferences van gebruiker (opgeslagen bij rating)
- Ratings per auto

❌ **Wat je NIET nodig hebt:**
- Username/password
- Email verificatie
- User accounts
- Login/logout

### Session Management

**Hoe lang blijft een session actief?**
- Standaard: **24 uur** (configureerbaar)
- Na 24 uur inactiviteit: nieuwe session = nieuwe user ID
- Dit is OK voor collaborative filtering (ratings blijven in database)

### Voorbeeld: Twee Gebruikers

```
GEBRUIKER A (Browser Chrome):
- Session ID: "session-abc"
- User ID: "user-123"
- Rates BMW X5: ⭐⭐⭐⭐⭐
- Rates Audi Q7: ⭐⭐⭐⭐

GEBRUIKER B (Browser Firefox):
- Session ID: "session-xyz"  
- User ID: "user-456"
- Rates BMW X5: ⭐⭐⭐⭐⭐
- Rates Mercedes GLE: ⭐⭐⭐⭐⭐

COLLABORATIVE FILTERING:
- Beide gebruikers hebben gelijkaardige preferences
- BMW X5 heeft 2x 5 sterren van gelijkaardige gebruikers
- → BMW X5 krijgt hogere score voor nieuwe gebruiker
```

---

## Wanneer WEL Inlog Systeem?

Je hebt alleen een inlog systeem nodig als je:

1. **Persoonlijke accounts** wilt (gebruiker kan eigen ratings bekijken)
2. **Cross-device sync** wilt (ratings op telefoon + laptop)
3. **User profiles** wilt (favorieten, geschiedenis)
4. **Privacy/security** nodig hebt (beschermde data)

**Voor collaborative filtering alleen: NIET NODIG!**

---

## Database Locatie Controleren

### Via Code

De database locatie wordt gelogd bij startup:
```
[INFO] User Ratings Database locatie: C:\...\data\user_ratings.db
```

### Via API (toekomstig)

Je kunt een endpoint toevoegen:
```csharp
[HttpGet("database/info")]
public IActionResult GetDatabaseInfo()
{
    var repo = (UserRatingRepository)_ratingRepository;
    return Ok(new { 
        databasePath = repo.DatabasePath,
        exists = File.Exists(repo.DatabasePath)
    });
}
```

---

## Azure Deployment

**Belangrijk voor Azure:**

SQLite databases in Azure App Service:
- ✅ **Werken lokaal** in de App Service directory
- ⚠️ **Verdwijnen bij restart** (als je geen persistent storage gebruikt)
- ✅ **Voor productie**: Gebruik Azure SQL Database i.p.v. SQLite

**Aanbeveling:**
- **Development**: SQLite (lokaal)
- **Production**: Azure SQL Database (persistent, schaalbaar)

---

## Samenvatting

1. **Database locatie**: `data/user_ratings.db` (lokaal) of configureerbaar
2. **User IDs**: Session-based (geen inlog nodig!)
3. **Hoe het werkt**: Browser session → User ID → Ratings gekoppeld
4. **Voor collaborative filtering**: Geen authentication nodig
5. **Voor persoonlijke accounts**: Dan wel inlog systeem nodig

**Het huidige systeem werkt perfect voor collaborative filtering zonder inlog!** 🎉






