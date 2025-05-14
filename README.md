# Database for Developers – Obligatorisk Opgave 2


## Introduktion

Dette projekt er en teknisk skabelon (template) for implementeringen af centrale backend-komponenter i en e-handelsplatform. Skabelonen er udarbejdet som svar på kravene i den obligatoriske opgave "Database for Developers – Compulsory 2".

Formålet har været at konstruere en struktureret og skalerbar backend, der illustrerer, hvordan man kan opbygge en moderne e-commerce-løsning med fokus på følgende nøgleområder:

- Valg af database og lagringsstrategi
- Brug af cloud storage til håndtering af større objekter
- Integration af caching for hurtigere svartider
- CQRS-arkitektur til adskillelse af læse- og skriveoperationer
- Transaktionshåndtering på tværs af relationelle og ikke-relationelle databaser

Projektet giver et solidt udgangspunkt for videreudvikling af en fuldt funktionel e-commerce backend, og kan nemt tilpasses til mere komplekse behov.

# Databasevalg
Igennem projektet har jeg valgt at bruge en nosql db (mongodb), til Read operationer, en sql db (mariadb) til Write operationer samt en Redis for lokal chaching.
Hertil har jeg inkluderet en eventbus og handlers for at opdatere read databasen, med relevant indhold fra sql databasen. på denne måde sikre jeg en optimeret stuktur til at sikre relationernerne mellem brugere og listing, samt hurtig read fra Mongodb.

## NoSQL database
**MongoDB**  
Som NoSQL-løsning anvender jeg MongoDB, da den er dokumentbaseret og optimeret til hurtige læseoperationer. Den giver fleksibilitet i datamodellering og er særligt velegnet til håndtering af ustruktureret eller semistruktureret data.

## Relationel database
**MariaDB**  
Til det relationelle databaselag er MariaDB valgt. MariaDB er en hurtig, pålidelig og open-source database, der er fuldt kompatibel med MySQL. Den understøtter komplekse forespørgsler og relationer, hvilket gør den velegnet til strukturerede data og systemer med stærk dataintegritet.

## Cachelag
**Redis**  
Til cache anvender jeg Redis – en hukommelsesbaseret key-value store, der er ideel til hurtig adgang til midlertidige data. Redis hjælper med at forbedre svartider og aflaste databaselagene ved hyppigt anvendte forespørgsler.


## 2 Datamodel og lagringsstrategi

Platformens datamodel er bygget op omkring fire centrale entiteter: bruger, produkt (listing), ordre og anmeldelse. Brugeren afspejler individet bag en handling, og skelnen mellem køber og sælger sker logisk gennem brugerens aktiviteter. Produkter præsenteres som listings, som knyttes til en sælger og indeholder metadata som titel, pris og beskrivelse. Når brugere handler, genereres en ordre, der samler information om køber, valgte produkter og totalbeløb. Anmeldelser tilknyttes produkter og knyttes til brugeroplevelsen.

Billeder og andet ustruktureret data gemmes eksternt i en cloud-løsning(MiniO) og tilgås via URL'er, hvilket giver hurtig adgang og skalerbarhed uden at belaste den relationelle database.

### Forenklet databaseskema (relationel struktur)

```sql
-- USERS
CREATE TABLE Users (
    Id UUID PRIMARY KEY,
    Name TEXT NOT NULL,
    Email TEXT UNIQUE NOT NULL
);

-- LISTINGS
CREATE TABLE Listings (
    Id UUID PRIMARY KEY,
    Title TEXT NOT NULL,
    Description TEXT,
    Price DECIMAL NOT NULL,
    ImageUrl TEXT,
    SellerId UUID REFERENCES Users(Id)
);

-- ORDERS
CREATE TABLE Orders (
    Id UUID PRIMARY KEY,
    UserId UUID REFERENCES Users(Id),
    OrderDate TIMESTAMP NOT NULL,
    TotalAmount DECIMAL NOT NULL
);

-- ORDER-LISTINGS (mellemtingstabel for mange-til-mange relation)
CREATE TABLE OrderListings (
    OrderId UUID REFERENCES Orders(Id),
    ListingId UUID REFERENCES Listings(Id),
    PRIMARY KEY (OrderId, ListingId)
);

-- REVIEWS
CREATE TABLE Reviews (
    Id UUID PRIMARY KEY,
    ListingId UUID REFERENCES Listings(Id),
    UserId UUID REFERENCES Users(Id),
    Rating INT NOT NULL CHECK (Rating BETWEEN 1 AND 5),
    Comment TEXT
);
```

ligeledes skal lignende implemnteres i mongodb for at sikre mulighed for hurtig read. Konsistens mellem databaserne bliver bevaret via subcribers og publishers for de relevante handlers på min eventbus.

# ☁️ Integration af Cloud Storage

Systemet understøtter upload og håndtering af mediefiler (som f.eks. billeder tilknyttet brugerprofiler eller listings) via en ekstern, objektbaseret lagringsløsning. Der benyttes en **S3-kompatibel** cloud storage-platform, hvilket betyder, at systemet kan arbejde problemfrit med både lokale løsninger som **MinIO** og eksterne tjenester som **AWS S3** – uden behov for ændringer i applikationslogikken.

Denne tilgang gør systemet fleksibelt og fremtidssikret i forhold til både pris, skalerbarhed og hostingbehov.

---

## 🎯 Implementering

Cloud storage-funktionaliteten er implementeret i et repository under `Infrastructure`, hvor den nødvendige konfiguration og S3-klient logik håndteres.

Uploadprocessen sker gennem generering af **presigned URLs**, som returneres til klienten. Klienten kan derefter uploade filen direkte til den eksterne storage — uden at belaste backendserveren.

### flow:

1. Klienten anmoder backend om en uploadmulighed for et billede (f.eks. profilbillede).
2. Backend genererer en presigned S3-upload-URL og returnerer den til klienten.
3. Klienten uploader billedet direkte til cloud storage via URL’en.
4. Når upload er fuldført, sender klienten en reference (f.eks. URL eller sti) tilbage til backend.
5. Backend gemmer denne reference i databasen.

---

## 🗃️ Samspil med databasen

I MariaDB-databasen og mongoDB gemmes **kun referencen til billedet** – typisk som en sti eller URL. Dette gælder både for:

- `User.ProfileImagePath` – til profilbilleder
- `Listing.ImagePath` – til billeder for opslag/listings

Ved kun at gemme metadata i databasen og ikke selve filerne, sikrer vi at:

- Databasen holdes let og hurtig
- Store mediefiler håndteres uden at forringe performance
- Systemet skalerer bedre både i drift og ved øget belastning

---

## ⚙️ Fordele ved S3-kompatibel integration

- **Udbyderuafhængighed**: Systemet fungerer både med MinIO, AWS S3 og andre kompatible tjenester.
- **Skalerbarhed**: Systemet håndterer store filer uden at overbelaste

# Caching Strategi
I dette system er caching en central del af at sikre hurtig adgang til ofte anvendte data, reducere belastningen på databasen og forbedre systemets samlede ydeevne. Caching anvendes især til at håndtere data, der sjældent ændres, men ofte tilgås, som for eksempel brugerprofiler, listings og billeddata.

Den valgte cache-teknologi i dette system er **Redis**, som er en højtydende, in-memory key-value store. Redis bruges til at cache ofte efterspurgte data, hvilket gør systemet både hurtigere og mere skalerbart.

---

## 🚀 Caching Teknologi

**Redis** er valgt som cache-løsning, da den tilbyder:
- **Høj ydeevne**: Redis er i stand til at håndtere store mængder data hurtigt og effektivt, da det kører i hukommelsen.
- **Skalérbarhed**: Redis understøtter skalerbare løsninger og kan håndtere et stort antal samtidige anmodninger.
- **Let integration**: Redis er nem at integrere med C#-applikationer og tilbyder et enkelt API til at gemme og hente data.

Redis bruges til at cache data, der ofte efterspørges, såsom brugerprofiler og listings. Data som billeder gemmes derimod udenfor databasen, som beskrevet i **cloud storage** sektionen, og cachet via URL-links.

### Integration af Redis i repoet

Redis integreres i systemet via `Infrastructure/Services/CacheService.cs`, hvor Redis-klienten håndterer caching og cache-udløsning.

**Eksempel på Redis Cache Service**:

```csharp
using StackExchange.Redis;
using System.Threading.Tasks;

public class CacheService
{
    private readonly IDatabase _cache;
    
    public CacheService(IConnectionMultiplexer redis)
    {
        _cache = redis.GetDatabase();
    }

    // Cache et objekt
    public async Task SetCacheAsync(string key, string value)
    {
        await _cache.StringSetAsync(key, value);
    }

    // Hent cachet objekt
    public async Task<string> GetCacheAsync(string key)
    {
        return await _cache.StringGetAsync(key);
    }

    // Slet cachet objekt
    public async Task RemoveCacheAsync(string key)
    {
        await _cache.KeyDeleteAsync(key);
    }
}
```
# CQRS Implementering

CQRS (Command Query Responsibility Segregation) er et arkitekturmønster, der adskiller læseoperationer (queries) fra skriveoperationer (commands). Denne adskillelse gør det muligt at optimere både læse- og skriveoperationer uafhængigt, hvilket kan forbedre systemets ydeevne, skalerbarhed og vedligeholdelse. I vores system har vi implementeret CQRS for at håndtere operationslogik for læsning og skrivning af data i separate lag, hvilket bidrager til bedre performance og fleksibilitet.

---

## CQRS Arkitektur i Systemet

### Kommandoer (Commands)
I systemet anvendes kommandoer til at håndtere skriveoperationer. Kommandoer repræsenterer ændringer i systemets tilstand og bliver behandlet gennem et kommandohåndteringslag. Kommandoerne kan være forbundet med entiteter som **user profiles**, **listings** eller **orders**, hvor systemet skal skrive eller opdatere data.

For at implementere dette er kommandoerne håndteret i **Command Handlers**, som findes i **Application/Commands**-mappen. Kommandoerne bliver sendt til et command handler, som så udfører den nødvendige logik og ændrer dataene i databasen.

For at udløse en kommando skal man kalde den relevante handler i systemet, som f.eks. `UpdateUserProfileCommandHandler`, og bruge den til at håndtere opdateringer af brugerdetaljer.

---

## Forespørgsler (Queries)
I CQRS-mønstret håndterer forespørgsler (queries) læseoperationer, der henter data fra systemet. Læseoperationer adskilles fra skriveoperationer, hvilket giver mulighed for at optimere databasen til specifikt at håndtere forespørgsler. I denne implementering er forespørgsler implementeret i **Query Handlers**, som findes i **Application/Queries**-mappen.

Forespørgsler kan være enkle, såsom at hente alle listings eller brugerdetaljer, eller de kan være mere komplekse, som at filtrere data baseret på specifikke kriterier. Da læseoperationer ikke involverer komplekse ændringer af systemets tilstand, er de ofte optimeret til at være hurtigere.

Læsning af data sker fra en NoSQL-database, mens skriveoperationer håndteres af en SQL-database, hvilket giver mulighed for at optimere både læse- og skriveoperationer hver for sig.

For at udløse en forespørgsel skal man kalde den relevante query handler i systemet, som f.eks. `GetUserProfileQueryHandler`, for at hente brugerinformationsdata fra NoSQL-databasen.

---

# 📦 Fordele ved CQRS

- **Skalérbarhed**: Læse- og skriveoperationer kan skaleres uafhængigt af hinanden. Hvis læseoperationer dominerer systemet, kan vi skalere læse-klusteret uden at påvirke skriveoperationer.

- **Forbedret ydeevne**: Ved at adskille læsning og skrivning kan vi optimere databasens struktur og forespørgsler (queries) til at matche deres specifikke behov.

- **Fleksibilitet**: Det er muligt at ændre eller optimere læse- og skriveoperationer uden at påvirke hinanden.

- **Håndtering af kompleksitet**: Ved at bruge CQRS kan vi organisere logikken i systemet på en måde, der gør det lettere at vedligeholde og udvikle i fremtiden.


## Transaction Management i Systemet

### Relational Database
Kommunikation med den relationelle database håndteres via SQL, hvor vi bruger transaktioner for at sikre ACID-principperne (Atomicity, Consistency, Isolation, Durability). Transaktionerne sikrer, at alle opdateringer på ordrer og listings er atomare og konsistente.

### NoSQL Database
For NoSQL-databasen (MongoDB) implementeres transaktionshåndtering manuelt via **MongoDB.Driver**. Dette gør det muligt at sikre transaktionel integritet, men kræver eksplicit håndtering af transaktionerne via event bus til opdatering af data.
