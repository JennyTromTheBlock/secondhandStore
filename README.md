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

## Datamodel og lagringsstrategi

Platformens datamodel er bygget op omkring fire centrale entiteter: bruger, produkt (listing), ordre og anmeldelse. Brugeren afspejler individet bag en handling, og skelnen mellem køber og sælger sker logisk gennem brugerens aktiviteter. Produkter præsenteres som listings, som knyttes til en sælger og indeholder metadata som titel, pris og beskrivelse. Når brugere handler, genereres en ordre, der samler information om køber, valgte produkter og totalbeløb. Anmeldelser tilknyttes produkter og knyttes til brugeroplevelsen.

Billeder og andet ustruktureret data gemmes eksternt i en cloud-løsning (f.eks. MinIO eller S3-kompatibel storage) og tilgås via URL'er, hvilket giver hurtig adgang og skalerbarhed uden at belaste den relationelle database.

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
