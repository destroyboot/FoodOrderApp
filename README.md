# FoodOrderApp

FoodOrderApp to aplikacja do obslugi zamowien gastronomicznych. Projekt sklada sie z backendu ASP.NET Core, panelu webowego dla administracji/restauracji oraz aplikacji mobilnej dla klienta. System obsluguje restauracje, menu, koszyki, zamowienia stolikowe, odbior osobisty, dostawy, rezerwacje, powiadomienia, faktury, raporty oraz wielojezycznosc.

## Technologie

- Backend: ASP.NET Core, Entity Framework Core, SQL Server, ASP.NET Identity, JWT, SignalR
- Frontend web-admin: React, TypeScript, Vite, React Router, SignalR client, i18next
- Mobile: Expo, React Native, TypeScript, i18next
- Baza danych: SQL Server z migracjami EF Core

## Struktura projektu

```text
FoodOrderApp/
├── API/              # Warstwa HTTP: kontrolery, middleware, SignalR, Swagger, pliki lokalizacji
├── Core/             # Logika domenowa: encje, DTO/kontrakty, interfejsy, serwisy
├── Infrastructure/   # Dostep do danych i uslug zewnetrznych: EF Core, repozytoria, email, seedery
├── web-admin/        # Panel administracyjny w React
├── mobile-app/       # Aplikacja mobilna Expo/React Native
└── FoodOrderApp.sln  # Rozwiazanie Visual Studio
```

## Warstwy backendu

### API

`API` odpowiada za komunikacje ze swiatem zewnetrznym. Tutaj znajduja sie kontrolery REST, konfiguracja JWT, CORS, SignalR, Swagger, middleware bledow i diagnostyki czasu zapytan.

Najwazniejsze katalogi:

- `API/Controllers` - endpointy HTTP, np. restauracje, menu, koszyk, zamowienia, rezerwacje, raporty.
- `API/Hubs` - hub SignalR do odswiezania aktywnych zamowien.
- `API/Middleware` - middleware bledow i pomiaru czasu requestow.
- `API/Localization` oraz `API/App_Data/Localization` - obsluga tekstow aplikacji w roznych jezykach.
- `API/Support` - pomocnicze generatory, np. maile, PDF faktury, PDF podsumowania zamowienia.
- `API/Program.cs` - konfiguracja DI, JWT, CORS, SignalR, middleware i start aplikacji.

### Core

`Core` przechowuje najwazniejsze elementy biznesowe aplikacji. Ta warstwa nie powinna zalezec od szczegolow infrastruktury, takich jak SQL Server czy SMTP.

Najwazniejsze katalogi:

- `Core/Data/Entities` - encje domenowe, np. `Order`, `MenuItem`, `Restaurant`, `Cuisine`, `Allergen`.
- `Core/Contracts` - DTO, czyli obiekty przesylane przez API do weba/mobilki i odwrotnie.
- `Core/Interfaces` - interfejsy serwisow i repozytoriow.
- `Core/Services` - logika biznesowa, np. koszyk, menu, powiadomienia.
- `Core/Models` i `Core/Utilities` - pomocnicze modele i narzedzia.

### Infrastructure

`Infrastructure` zawiera implementacje techniczne: baze danych, repozytoria, seedery, wysylke emaili i powiadomienia push.

Najwazniejsze katalogi:

- `Infrastructure/Persistence` - `AppDbContext`, konfiguracja EF Core i fabryka kontekstu.
- `Infrastructure/Migrations` - migracje bazy danych.
- `Infrastructure/Repositories` - implementacje repozytoriow.
- `Infrastructure/Seeding` - dane startowe, konta testowe, role, restauracje, kuchnie, alergeny.
- `Infrastructure/Email` - wysylka maili przez SMTP.
- `Infrastructure/Notifications` - obsluga push notifications.
- `Infrastructure/Auth` - uzytkownik aplikacji i elementy Identity.

## Web-admin

`web-admin` to panel React dla administratora systemu i pracownikow restauracji.

Najwazniejsze miejsca:

- `web-admin/src/App.tsx` - glowny routing i uklad aplikacji.
- `web-admin/src/api.ts` - wspolna funkcja do wywolan API.
- `web-admin/src/auth.ts` - obsluga tokenu i sesji.
- `web-admin/src/i18n.tsx` - obsluga jezykow i tekstow pobieranych z API.
- `web-admin/src/realtime/ordersHub.ts` - polaczenie SignalR dla aktywnych zamowien.
- `web-admin/src/Pages` - glowne widoki panelu, np. aktywne zamowienia, menu, restauracje, rezerwacje, raporty.
- `web-admin/src/Pages/*/` - komponenty wydzielone z wiekszych ekranow.
- `web-admin/src/Components` - wspolne komponenty UI.

## Mobile-app

`mobile-app` to aplikacja Expo/React Native dla klienta.

Najwazniejsze miejsca:

- `mobile-app/src/RootShell.tsx` - glowny uklad aplikacji i dolna nawigacja.
- `mobile-app/src/context/AppSessionContext.tsx` - centralny stan sesji, restauracji, menu, koszyka, zamowien i powiadomien.
- `mobile-app/src/lib/api.ts` - klient API.
- `mobile-app/src/lib/config.ts` - adres backendu zalezy od platformy i zmiennej `EXPO_PUBLIC_API_BASE_URL`.
- `mobile-app/src/lib/i18n.ts` - konfiguracja i18n.
- `mobile-app/src/screens` - glowne ekrany.
- `mobile-app/src/screens/*/` - komponenty wydzielone z wiekszych ekranow.
- `mobile-app/src/components` - wspolne komponenty mobilne.

## Kluczowe funkcjonalnosci

- Autoryzacja i role: ASP.NET Identity, JWT, role `Admin`, `RestaurantAdmin`, `Waiter`, `Chef`, `DeliveryDriver`.
- Restauracje: zarzadzanie danymi restauracji, ustawieniami zamowien, kuchniami, stolikami i personelem.
- Menu: kategorie, pozycje menu, zdjecia, skladniki, alergeny i tlumaczenia.
- Koszyk: obsluga goscia przez `X-Guest-Token` oraz zalogowanego uzytkownika przez JWT.
- Zamowienia: zamowienia stolikowe, odbior osobisty, dostawa, statusy, przypisywanie kierowcy, faktury.
- Rezerwacje: terminy, stoliki, ustawienia rezerwacji i widok kalendarza.
- Powiadomienia: zapis w bazie, feed powiadomien, SignalR dla aktywnych zamowien i push notifications.
- Raporty: raporty admina generowane na podstawie funkcji/zapytan SQL.
- Wielojezycznosc: teksty UI, tlumaczenia menu, kuchni i alergenow; web i mobile przekazuja wybrany `culture` do API.

## Wielojezycznosc

Aplikacja uzywa i18n, czyli mechanizmu pozwalajacego wyswietlac teksty w roznych jezykach. Teksty interfejsu sa trzymane w plikach JSON w `API/App_Data/Localization`, a dane biznesowe, takie jak menu, kuchnie i alergeny, maja osobne tabele tlumaczen w bazie.

Przyklady:

- teksty UI: `API/App_Data/Localization/pl-PL.json`, `API/App_Data/Localization/en-US.json`
- menu: `MenuItemTranslation`, `MenuCategoryTranslation`
- kuchnie: `CuisineTranslation`
- alergeny: `AllergenTranslation`

Web-admin i mobile znaja aktualnie wybrany jezyk i wysylaja go do API jako parametr, np.:

```text
/api/restaurants?culture=pl-PL
/api/admin/ingredients/allergens?culture=en-US
```

API probuje znalezc tlumaczenie dla wskazanego jezyka, potem wraca do domyslnego `pl-PL`, a na koncu do wartosci technicznej, jesli brakuje tlumaczenia.

## Szybkie uruchomienie

### Backend

```powershell
cd C:\Users\destr\source\repos\FoodOrderApp
dotnet restore
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Infrastructure/Infrastructure.csproj
dotnet run --project API/API.csproj
```

Domyslne adresy API:

```text
https://localhost:7234
http://localhost:5271
```

Swagger w trybie development:

```text
https://localhost:7234/swagger
```

### Web-admin

```powershell
cd C:\Users\destr\source\repos\FoodOrderApp\web-admin
npm install
npm run dev
```

Domyslnie Vite uruchamia panel pod adresem:

```text
http://localhost:5173
```

### Mobile-app

```powershell
cd C:\Users\destr\source\repos\FoodOrderApp\mobile-app
npm install
npm run start
```

Dla Android Emulatora backend powinien byc dostepny przez:

```powershell
$env:EXPO_PUBLIC_API_BASE_URL="http://10.0.2.2:5271"
```

Dla telefonu w tej samej sieci Wi-Fi nalezy uzyc IP komputera:

```powershell
$env:EXPO_PUBLIC_API_BASE_URL="http://TWOJE-IP:5271"
```

## Przydatne komendy

Backend:

```powershell
dotnet build API/API.csproj
dotnet ef migrations add NazwaMigracji --project Infrastructure/Infrastructure.csproj --startup-project API/API.csproj
dotnet ef database update --project Infrastructure/Infrastructure.csproj --startup-project Infrastructure/Infrastructure.csproj
```

Web-admin:

```powershell
npm run build
```

Mobile:

```powershell
npm run typecheck
```

Wyszukiwanie w projekcie:

```powershell
rg "szukana fraza"
```

## Uwagi

- `API/App_Data/Uploads`, `API/wwwroot/uploads`, `.codex`, `.codex_temp`, `dist`, `node_modules` i pliki `*.tsbuildinfo` sa ignorowane przez Git.
- Przy zmianach w encjach EF Core trzeba dodac migracje i zastosowac ja na bazie.
- Przy problemach z wolnymi requestami warto patrzec na naglowek `X-Elapsed-Ms` oraz logi z `RequestTimingMiddleware`.
- Przy testowaniu SignalR lokalnie web-admin laczy sie z `/hubs/orders`.
